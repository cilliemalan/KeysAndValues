using KeysAndValues.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeysAndValues;

public sealed partial class KeyValueStore
{
    internal class Log
    {
        private readonly BufferedWriteStream wal;
        private readonly List<LogEntry> log = [];

        public Log(Stream writeAheadLogStream)
        {
            wal = new(writeAheadLogStream, true);
        }

        public static Log Open(Stream stream)
        {
            Log log = new(stream);
            long pos = 0;

            while (TryDeserializeLogEntry(
                stream,
                out var amtRead,
                out var logEntry))
            {
                pos += amtRead;
                log.log.Add(logEntry);
            }

            stream.Position = pos;
            return log;
        }

        public void Append(ChangeOperation[] operations, long sequence)
        {
            Debug.Assert(log.Count == 0 || log[^1].Sequence == sequence - 1);
            log.Add(new() { Sequence = sequence, ChangeOperations = operations });
            Serialize(operations, sequence, wal);
        }

        public void AppendSnapshot(ImmutableAvlTree<Mem, Mem> snapshot, long sequence)
        {
            Debug.Assert(log.Count == 0 || log[^1].Sequence == sequence);
            log.Add(new() { Sequence = sequence, Snapshot = snapshot });
            Serialize(snapshot, sequence, wal);
        }

        public void Flush() => wal.Flush();
        public Task FlushAsync(CancellationToken cancellationToken) => wal.FlushAsync(cancellationToken);

        public readonly struct LogEntry
        {
            public LogEntryType Type { get; init; }
            public long Sequence { get; init; }
            public ChangeOperation[]? ChangeOperations { get; init; }
            public ImmutableAvlTree<Mem, Mem> Snapshot { get; init; }
        }

        public enum LogEntryType
        {
            ChangeOperation = 1,
            Snapshot = 2,
        }
    }
}
