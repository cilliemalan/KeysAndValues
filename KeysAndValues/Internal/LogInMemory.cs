using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace KeysAndValues.Internal
{
    public class LogInMemory
    {
        private ImmutableList<WriteAheadLogEntry> log;

        private LogInMemory(ImmutableList<WriteAheadLogEntry> log)
        {
            this.log = log;
        }

        public static LogInMemory Read(Stream stream)
        {
            long pos = 0;

            var b = ImmutableList<WriteAheadLogEntry>.Empty.ToBuilder();
            while (TryDeserializeLogEntry(
                stream,
                out var amtRead,
                out var logEntry))
            {
                pos += amtRead;
                b.Add(logEntry);
            }
            var log = b.ToImmutable();

            return new(log);
        }

        public void Append(ChangeOperation[] operations, long sequence)
        {
            log = log.Add(new() { Sequence = sequence, ChangeOperations = operations });
        }

        public void AppendSnapshot(ImmutableAvlTree<Mem, Mem> snapshot, long sequence)
        {
            log = log.Add(new() { Sequence = sequence, Snapshot = snapshot });
        }

        public bool TryGetSnapshot(long sequence, [MaybeNullWhen(false)] out ImmutableAvlTree<Mem, Mem>? store)
        {
            if (log.Count == 0 || sequence <= 0)
            {
                store = null;
                return false;
            }

            var entries = log;
            int opIndex = entries.BinarySearch(new() { Sequence = sequence, Type = WriteAheadLogEntryType.ChangeOperation });
            if (opIndex < 0)
            {
                var firstEntry = log[0];
                if (firstEntry.Sequence > sequence &&
                    firstEntry.Type == WriteAheadLogEntryType.Snapshot)
                {
                    // the log is truncated
                    store = firstEntry.Snapshot;
                    return false;
                }

                store = null;
                return false;
            }

            var entry = entries[opIndex];
            Debug.Assert(entry.Type == WriteAheadLogEntryType.ChangeOperation);
            Debug.Assert(entry.Sequence == sequence);

            // if this entry is adjacent to a snapshot, return that instead
            if (opIndex + 1 < entries.Count &&
                entries[opIndex + 1].Type == WriteAheadLogEntryType.Snapshot)
            {
                store = entries[opIndex + 1].Snapshot;
                return true;
            }

            // scan backwards until we find a snapshot
            var closestSnapshot = ImmutableAvlTree<Mem, Mem>.Empty;
            int snapshotIndex = -1;
            for (int i = opIndex; i >= 0; i--)
            {
                var e = entries[i];
                if (e.Type == WriteAheadLogEntryType.Snapshot)
                {
                    Debug.Assert(e.Snapshot is not null);
                    closestSnapshot = e.Snapshot;
                    snapshotIndex = i;
                    break;
                }
            }

            // apply all changes up until the one we are
            // looking for.
            var b = closestSnapshot.ToBuilder();
            for (int i = snapshotIndex + 1; i <= opIndex; i++)
            {
                var ops = log[i].ChangeOperations;
                Debug.Assert(ops is not null);
                b.Apply(ops);
            }
            store = b.ToImmutable();
            return true;
        }

        public static bool TryDeserializeLogEntry(Stream stream, out int amtRead, out WriteAheadLogEntry entry)
        {
            amtRead = 0;
            entry = default;

            // read the length and type
            Span<byte> header = stackalloc byte[4 + 1 + 8];
            int r = stream.ReadAtLeast(header, header.Length, false);
            amtRead += r;
            if (r != header.Length)
            {
                return false;
            }

            // parse the length, type, and sequence
            int length = BitConverter.ToInt32(header);
            int type = header[4];
            long sequence = BitConverter.ToInt64(header[5..]);
            if (length < 32 + header.Length || sequence < 0)
            {
                return false;
            }

            // read the whole buffer
            var bdata = new byte[length];
            var buffer = new byte[length].AsSpan();
            header.CopyTo(buffer);
            r = stream.ReadAtLeast(buffer[header.Length..], length - header.Length, false);
            amtRead += r;
            if (r != length - header.Length)
            {
                return false;
            }

            // check the hash
            Span<byte> hashcmp = stackalloc byte[32];
            SHA256.HashData(buffer[..^32], hashcmp);
            if (!hashcmp.SequenceEqual(buffer[^32..]))
            {
                return false;
            }

            var letype = (WriteAheadLogEntryType)type;
            switch (letype)
            {
                case WriteAheadLogEntryType.ChangeOperation:
                    {
                        var index = 13;
                        int count = BitConverter.ToInt32(buffer[index..]);
                        index += 4;
                        var operations = new ChangeOperation[count];
                        for (int i = 0; i < count; i++)
                        {
                            var optype = (ChangeOperationType)buffer[index++];
                            ReadOnlyMemory<byte> key;
                            ReadOnlyMemory<byte> value;
                            switch (operations[i].Type)
                            {
                                case ChangeOperationType.Set:
                                    int kl = BitConverter.ToInt32(buffer[index..]);
                                    index += 4;
                                    key = new(bdata, index, kl);
                                    index += kl;
                                    int vl = BitConverter.ToInt32(buffer[index..]);
                                    index += 4;
                                    value = new(bdata, index, vl);
                                    index += vl;
                                    break;
                                case ChangeOperationType.Delete:
                                    int dkl = BitConverter.ToInt32(buffer[index..]);
                                    index += 4;
                                    key = new ReadOnlyMemory<byte>(bdata, index, dkl);
                                    index += dkl;
                                    value = default;
                                    break;
                                default:
                                case ChangeOperationType.None:
                                    key = default;
                                    value = default;
                                    break;
                            }
                            operations[i] = new ChangeOperation
                            {
                                Type = optype,
                                Key = key,
                                Value = value
                            };
                        }

                        entry = new()
                        {
                            Type = letype,
                            Sequence = sequence,
                            ChangeOperations = operations
                        };
                        return true;
                    }
                case WriteAheadLogEntryType.Snapshot:
                    {
                        // note: reading it like this prevents the 
                        // entire block from ever being GC'd
                        // which is fine as long as no dictionaries
                        // are built based on it.
                        var index = 13;
                        var builder = ImmutableAvlTree<Mem, Mem>.Empty.ToBuilder();
                        while (index < length - 32)
                        {
                            int kl = BitConverter.ToInt32(buffer[index..]);
                            index += 4;
                            var key = new ReadOnlyMemory<byte>(bdata, index, kl);
                            index += kl;
                            int vl = BitConverter.ToInt32(buffer[index..]);
                            index += 4;
                            var value = new ReadOnlyMemory<byte>(bdata, index, vl);
                            index += vl;
                            builder.Add(key, value);
                        }
                        entry = new() { Type = letype, Sequence = sequence, Snapshot = builder.ToImmutable() };
                    }
                    return true;
                default:
                    entry = new() { Type = letype, Sequence = sequence };
                    return true;
            }
        }

    }
}
