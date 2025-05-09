using KeysAndValues.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeysAndValues;

public sealed partial class KeyValueStore
{
    internal class Log
    {
        private readonly BufferedWriteStream wal;
        private ImmutableList<LogEntry> log = [];

        public Log(Stream writeAheadLogStream)
        {
            wal = new(writeAheadLogStream, true);
        }

        public static Log Open(Stream stream)
        {
            Log log = new(stream);
            long pos = 0;

            var b = log.log.ToBuilder();
            while (TryDeserializeLogEntry(
                stream,
                out var amtRead,
                out var logEntry))
            {
                pos += amtRead;
                b.Add(logEntry);
            }
            log.log = b.ToImmutable();

            stream.Position = pos;
            return log;
        }

        public void Append(ChangeOperation[] operations, long sequence)
        {
            log = log.Add(new() { Sequence = sequence, ChangeOperations = operations });
            Serialize(operations, sequence, wal);
        }

        public void AppendSnapshot(ImmutableAvlTree<Mem, Mem> snapshot, long sequence)
        {
            log = log.Add(new() { Sequence = sequence, Snapshot = snapshot });
            Serialize(snapshot, sequence, wal);
        }

        public void Flush() => wal.Flush();
        public Task FlushAsync(CancellationToken cancellationToken) => wal.FlushAsync(cancellationToken);

        public bool TryGetSnapshot(long sequence, [MaybeNullWhen(false)] out ImmutableAvlTree<Mem, Mem>? store)
        {
            if (log.Count == 0 || sequence <= 0)
            {
                store = null;
                return false;
            }

            var entries = log;
            int opIndex = entries.BinarySearch(new() { Sequence = sequence, Type = LogEntryType.ChangeOperation });
            if (opIndex < 0)
            {
                var firstEntry = log[0];
                if (firstEntry.Sequence > sequence &&
                    firstEntry.Type == LogEntryType.Snapshot)
                {
                    // the log is truncated
                    store = firstEntry.Snapshot;
                    return false;
                }

                store = null;
                return false;
            }

            var entry = entries[opIndex];
            Debug.Assert(entry.Type == LogEntryType.ChangeOperation);
            Debug.Assert(entry.Sequence == sequence);

            // if this entry is adjacent to a snapshot, return that instead
            if (opIndex + 1 < entries.Count &&
                entries[opIndex + 1].Type == LogEntryType.Snapshot)
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
                if (e.Type == LogEntryType.Snapshot)
                {
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
                Debug.Assert(log[i].Type == LogEntryType.ChangeOperation &&
                    log[i].ChangeOperations is not null);
                ApplyToBuilder(log[i].ChangeOperations!, b);
            }
            store = b.ToImmutable();
            return true;
        }

        public readonly struct LogEntry
        {
            public LogEntryType Type { get; init; }
            public long Sequence { get; init; }
            public ChangeOperation[]? ChangeOperations { get; init; }
            public ImmutableAvlTree<Mem, Mem> Snapshot { get; init; }
        }

        private class LogEntryComparer : IComparer<LogEntry>, IEqualityComparer<LogEntry>
        {
            public static readonly LogEntryComparer Instance = new();

            public int Compare(LogEntry x, LogEntry y)
            {
                if (x.Sequence < y.Sequence)
                {
                    return -1;
                }
                else if (x.Sequence > y.Sequence)
                {
                    return 1;
                }
                else
                {
                    return x.Type.CompareTo(y.Type);
                }
            }

            public bool Equals(LogEntry x, LogEntry y)
            {
                return x.Sequence == y.Sequence && x.Type == y.Type;
            }

            public int GetHashCode([DisallowNull] LogEntry obj)
            {
                return obj.Sequence.GetHashCode() ^ obj.Type.GetHashCode();
            }
        }

        public enum LogEntryType
        {
            ChangeOperation = 1,
            Snapshot = 2,
            // note: snapshot must be greater than change operaion
        }
    }
}
