namespace KeysAndValues;

using KeysAndValues.Internal;
using System.Collections;
using System.Collections.Concurrent;
using System.Security.Cryptography;

public sealed class WriteAheadLog : IDisposable
{
    private long disposed;
    private readonly Thread storeThread;
    private readonly BlockingCollection<object> queue = [];
    private readonly IWalStreamProvider walStreamProvider;
    private ImmutableAvlTree<long, long> offsetLog = [];

    public WriteAheadLog(KeyValueStore store, string walFilePath)
        : this(store, new FileWalStreamProvider(walFilePath))
    {
    }

    public WriteAheadLog(KeyValueStore store, IWalStreamProvider walStreamProvider)
    {
        ArgumentNullException.ThrowIfNull(store);

        var writeAheadLogWriteStream = walStreamProvider.OpenWrite();
        try
        {
            OpenWalFile(writeAheadLogWriteStream, out offsetLog);

            storeThread = new(StoreThread)
            {
                IsBackground = true,
                Name = $"{nameof(WriteAheadLog)} Write Thread"
            };

            storeThread.Start(writeAheadLogWriteStream);
            store.Changed += Store_Changed;
            this.walStreamProvider = walStreamProvider;
        }
        catch
        {
            writeAheadLogWriteStream.Dispose();
            throw;
        }
    }

    internal static void OpenWalFile(Stream fs, out ImmutableAvlTree<long, long> sequences)
    {
        // opening the WAL file is very tolerant to data corruption.
        // It is not tolerant to read or write failures.
        var sb = ImmutableAvlTree<long, long>.Empty.ToBuilder();
        try
        {
            sequences = [];
            long previousSequence = 0;
            for (; ; )
            {
                var offset = fs.Position;
                if (!ReadButOnlyVerifyEntry(fs, out var sequence) ||
                    sequence <= previousSequence)
                {
                    if (fs.Position != offset)
                    {
                        // rewind back to just after the
                        // last valid entry.
                        fs.Position = offset;
                    }
                    break;
                }

                Debug.Assert(sequence > 0);
                sb.Add(sequence, offset);
                previousSequence = sequence;
            }

            sequences = sb.ToImmutable();
        }
        catch
        {
            fs.Dispose();
            throw;
        }

        if (fs.Position != fs.Length)
        {
            // chop off the invalid part at the end.
            fs.SetLength(fs.Position);
        }
    }

    private void StoreThread(object? obj)
    {
        Debug.Assert(obj is not null);
        using var fs = (FileStream)obj;
        long lastSequenceNumber = 0;
        foreach (var item in queue.GetConsumingEnumerable())
        {
            if (item is WriteAheadLogEntry entry)
            {
                WriteEntry(fs, entry);
                offsetLog = offsetLog.Add(entry.Sequence, fs.Position);
                lastSequenceNumber = entry.Sequence;
            }
            else if (item is TaskCompletionSource<(long sequenceNumber, long offset)> tcs)
            {
                tcs.TrySetResult((lastSequenceNumber, fs.Position));
            }
        }
    }

    private void Store_Changed(KeyValueStore keyValueStore, in ReadOnlySpan<ChangeOperation> operations, long newSequence, ImmutableAvlTree<Mem, Mem> store)
    {
        queue.Add(new WriteAheadLogEntry
        {
            Sequence = newSequence,
            ChangeOperations = [.. operations]
        });
    }

    internal static bool ReadEntry(Stream stream, out WriteAheadLogEntry entry)
    {
        entry = default;
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> tmp = stackalloc byte[32];
        if (!stream.TryReadExactly(tmp[..12]))
        {
            return false;
        }
        hasher.AppendData(tmp[..12]);
        long sequence = BitConverter.ToInt64(tmp[..8]);
        if (sequence <= 0)
        {
            return false;
        }

        int numOps = BitConverter.ToInt32(tmp[8..12]);
        if (numOps < 0)
        {
            return false;
        }

        var changes = new ChangeOperation[numOps];
        for (int i = 0; i < numOps; i++)
        {
            // type
            if (!stream.TryReadExactly(tmp[..1]))
            {
                return false;
            }
            hasher.AppendData(tmp[..1]);

            var type = (ChangeOperationType)tmp[0];
            if (type == ChangeOperationType.None)
            {
                continue;
            }

            // key
            if (!stream.TryReadExactly(tmp[..4]))
            {
                return false;
            }
            hasher.AppendData(tmp[..4]);
            int keyLength = BitConverter.ToInt32(tmp[..4]);
            if (keyLength < 0)
            {
                return false;
            }
            var keyMem = new byte[keyLength];
            if (!stream.TryReadExactly(keyMem))
            {
                return false;
            }
            hasher.AppendData(keyMem);

            if (type == ChangeOperationType.Delete)
            {
                changes[i] = new() { Type = type, Key = keyMem };
                continue;
            }

            if (type != ChangeOperationType.Set)
            {
                return false;
            }

            // value
            if (!stream.TryReadExactly(tmp[..4]))
            {
                return false;
            }
            hasher.AppendData(tmp[..4]);
            int valueLength = BitConverter.ToInt32(tmp[..4]);
            if (valueLength < 0)
            {
                return false;
            }
            var valueMem = new byte[valueLength];
            if (!stream.TryReadExactly(valueMem))
            {
                return false;
            }
            hasher.AppendData(valueMem);


            changes[i] = new()
            {
                Type = type,
                Key = keyMem,
                Value = valueMem
            };
        }

        hasher.GetHashAndReset(tmp);
        Span<byte> storedHash = stackalloc byte[32];
        if (!stream.TryReadExactly(storedHash))
        {
            return false;
        }

        if (!storedHash.SequenceEqual(tmp))
        {
            return false;
        }

        entry = new WriteAheadLogEntry
        {
            Sequence = sequence,
            ChangeOperations = changes
        };
        return true;
    }

    internal static bool ReadButOnlyVerifyEntry(Stream stream, out long sequence)
    {
        sequence = 0;
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> tmp = stackalloc byte[32];
        if (!stream.TryReadExactly(tmp[..12]))
        {
            return false;
        }
        hasher.AppendData(tmp[..12]);
        sequence = BitConverter.ToInt64(tmp[..8]);
        int numOps = BitConverter.ToInt32(tmp[8..12]);
        if (numOps < 0)
        {
            return false;
        }

        var pool = ArrayPool<byte>.Shared;
        for (int i = 0; i < numOps; i++)
        {
            // type
            if (!stream.TryReadExactly(tmp[..1]))
            {
                return false;
            }
            hasher.AppendData(tmp[..1]);

            var type = (ChangeOperationType)tmp[0];
            if (type == ChangeOperationType.None)
            {
                continue;
            }

            // key
            if (!stream.TryReadExactly(tmp[..4]))
            {
                return false;
            }
            hasher.AppendData(tmp[..4]);
            int keyLength = BitConverter.ToInt32(tmp[..4]);
            if (keyLength < 0)
            {
                return false;
            }
            var keyMem = pool.Rent(keyLength);
            var key = keyMem.AsSpan(0, keyLength);
            if (!stream.TryReadExactly(key))
            {
                pool.Return(keyMem);
                return false;
            }
            hasher.AppendData(key);
            pool.Return(keyMem);

            if (type == ChangeOperationType.Delete)
            {
                continue;
            }

            if (type != ChangeOperationType.Set)
            {
                return false;
            }

            // value
            if (!stream.TryReadExactly(tmp[..4]))
            {
                return false;
            }
            hasher.AppendData(tmp[..4]);
            int valueLength = BitConverter.ToInt32(tmp[..4]);
            if (valueLength < 0)
            {
                return false;
            }
            var valueMem = pool.Rent(valueLength);
            var value = valueMem.AsSpan(0, valueLength);
            if (!stream.TryReadExactly(value))
            {
                pool.Return(valueMem);
                return false;
            }
            hasher.AppendData(value);
            pool.Return(valueMem);
        }

        hasher.GetHashAndReset(tmp);
        Span<byte> storedHash = stackalloc byte[32];
        if (!stream.TryReadExactly(storedHash))
        {
            return false;
        }

        if (!storedHash.SequenceEqual(tmp))
        {
            return false;
        }

        return true;
    }

    internal static long WriteEntry(Stream stream, in WriteAheadLogEntry entry)
    {
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> tmp = stackalloc byte[32];
        BitConverter.TryWriteBytes(tmp, entry.Sequence);
        BitConverter.TryWriteBytes(tmp[8..], entry.ChangeOperations.Length);
        stream.Write(tmp[..12]);
        hasher.AppendData(tmp[..12]);

        for (int i = 0; i < entry.ChangeOperations.Length; i++)
        {
            var change = entry.ChangeOperations[i];
            tmp[0] = (byte)change.Type;
            stream.Write(tmp[0..1]);
            hasher.AppendData(tmp[0..1]);
            if (change.Type == ChangeOperationType.None)
            {
                // shrug
                continue;
            }

            BitConverter.TryWriteBytes(tmp[0..4], change.Key.Length);
            stream.Write(tmp[0..4]);
            hasher.AppendData(tmp[0..4]);
            stream.Write(change.Key.Span);
            hasher.AppendData(change.Key.Span);

            if (change.Type != ChangeOperationType.Set)
            {
                continue;
            }

            BitConverter.TryWriteBytes(tmp[0..4], change.Value.Length);
            stream.Write(tmp[0..4]);
            hasher.AppendData(tmp[0..4]);
            stream.Write(change.Value.Span);
            hasher.AppendData(change.Value.Span);
        }

        hasher.GetHashAndReset(tmp);
        stream.Write(tmp);

        return stream.Position;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1)
        {
            return;
        }

        queue.CompleteAdding();
        storeThread.Join();
        queue.Dispose();
    }

    public interface IWalStreamProvider
    {
        public Stream OpenWrite();
        public Stream OpenRead();
    }


    public class FileWalStreamProvider(string walFilePath) : IWalStreamProvider
    {
        private readonly string walFilePath = walFilePath ?? throw new ArgumentNullException(nameof(walFilePath));

        public Stream OpenWrite()
        {
            return File.Open(walFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        }

        public Stream OpenRead()
        {
            return File.Open(walFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
    }

    public readonly struct Enumerable(IWalStreamProvider wsp,
        long startSequence, long endSequenceExclusive,
        ImmutableAvlTree<long, long> offsetLog) : IEnumerable<WriteAheadLogEntry>
    {
        public IEnumerator<WriteAheadLogEntry> GetEnumerator()
        {
            var fs = wsp.OpenRead();
            try
            {
                long fileOpenOffset = 0;
                // Todo: is there a lighter way?
                var enumerator = offsetLog.Range(startSequence, endSequenceExclusive).GetEnumerator();
                if (enumerator.MoveNext())
                {
                    fileOpenOffset = enumerator.Current.Value;
                }
                enumerator.Dispose();
                fs.Position = fileOpenOffset;

                return new Enumerator(fs, startSequence, endSequenceExclusive);
            }
            catch
            {
                fs.Dispose();
                throw;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct Enumerator(Stream stream,
        long startSequence, long endSequenceExclusive)
        : IEnumerator<WriteAheadLogEntry>
    {
        private bool valid;
        private WriteAheadLogEntry current;

        private readonly Stream stream = stream ?? throw new ArgumentNullException(nameof(stream));
        private readonly long startSequence = startSequence;
        private readonly long endSequenceExclusive = endSequenceExclusive;
        private readonly long resetPosition = stream.Position;

        public readonly WriteAheadLogEntry Current
        {
            get
            {
                if (!valid)
                {
                    throw new InvalidOperationException("Enumerator is not valid. Call MoveNext first.");
                }

                return current;
            }
        }

        readonly object IEnumerator.Current => Current;

        public readonly void Dispose() => stream.Dispose();

        public bool MoveNext()
        {
            valid = ReadEntryInternal();
            return valid;
        }

        private bool ReadEntryInternal()
        {
            for (; ; )
            {
                bool readok = ReadEntry(stream, out current);
                if (!readok)
                {
                    return false;
                }

                if (current.Sequence >= endSequenceExclusive)
                {
                    return false;
                }

                if (current.Sequence >= startSequence)
                {
                    return true;
                }
            }
        }

        public void Reset()
        {
            stream.Position = resetPosition;
            valid = false;
        }
    }
}
