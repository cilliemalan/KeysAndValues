namespace KeysAndValues;

using KeysAndValues.Internal;
using System.Collections;
using System.Collections.Concurrent;
using System.Security.Cryptography;

public sealed partial class WriteAheadLog : IDisposable
{
    private long disposed;
    private readonly Thread storeThread;
    private readonly BlockingCollection<WriteAheadLogEntry> queue = [];
    private readonly IWalStreamProvider walStreamProvider;
    private ImmutableAvlTree<long, long> offsetLog = [];
    private Action<long>? onWriteAction;

    /// <summary>
    /// Gets or sets a value indicating whether changes are flushed
    /// to the WAL file after each write.
    /// </summary>
    public bool Synchronized { get; set; }

    public long MinSequence => offsetLog.FirstOrDefault().Key;

    public long MaxSequence
    {
        get
        {
            using var e = offsetLog.GetReverseEnumerator();
            if (!e.MoveNext())
            {
                return default;
            }

            return e.Current.Key;
        }
    }

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
                if (!LogSerialization.VerifyNextEntry(fs, out var sequence) ||
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
        while (!queue.IsAddingCompleted)
        {
            var entry = queue.Take();
            do
            {
                try
                {
                    LogSerialization.WriteEntry(fs, entry);
                    offsetLog = offsetLog.Add(entry.Sequence, fs.Position);
                    lastSequenceNumber = entry.Sequence;

                    if (Synchronized)
                    {
                        fs.Flush();
                    }
                    onWriteAction?.Invoke(entry.Sequence);
                }
                catch
                {
                    // TODO
                }
            } while (queue.TryTake(out entry));
        }
    }

    private void Store_Changed(KeyValueStore keyValueStore, in ReadOnlySpan<ChangeOperation> operations, long newSequence, ImmutableAvlTree<Mem, Mem> store)
    {
        var entry = new WriteAheadLogEntry
        {
            Sequence = newSequence,
            ChangeOperations = [.. operations]
        };

        if (!Synchronized)
        {
            queue.Add(entry);
            return;
        }

        EnqueueSynchronized(newSequence, entry);
    }

    private void EnqueueSynchronized(long newSequence, WriteAheadLogEntry entry)
    {
        var s = new SemaphoreSlim(0);
        IncludeAtomic(ref onWriteAction, Releaser);
        Thread.MemoryBarrier();
        queue.Add(entry);
        s.Wait();
        ExcludeAtomic(ref onWriteAction, Releaser);

        void Releaser(long seq)
        {
            if (seq == newSequence)
            {
                s.Release();
            }
        }
    }

    private static void IncludeAtomic<T>(ref T? del, T newdel)
        where T : Delegate
    {
        for (; ; )
        {
            var oval = del;
            var combined = (T)Delegate.Combine(oval, newdel);

            if (combined == oval)
            {
                return;
            }

            var original = Interlocked.CompareExchange(ref del, combined, del);
            if (original == oval)
            {
                return;
            }
        }
    }

    private static void ExcludeAtomic<T>(ref T? del, T olddel)
        where T : Delegate
    {
        for (; ; )
        {
            var oval = del;
            var uncombined = (T?)Delegate.Remove(oval, olddel);

            if (uncombined == oval)
            {
                return;
            }

            var original = Interlocked.CompareExchange(ref del, uncombined, del);
            if (original == oval)
            {
                return;
            }
        }
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
}
