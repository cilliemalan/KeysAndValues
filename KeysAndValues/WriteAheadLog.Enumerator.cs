namespace KeysAndValues;

using System.Collections;

public sealed partial class WriteAheadLog
{
    public struct Enumerator(Stream stream,
        long startSequence, long endSequenceExclusive)
        : IEnumerator<WriteAheadLogEntry>
    {
        private WriteAheadLogEntry? current;

        private readonly Stream stream = stream ?? throw new ArgumentNullException(nameof(stream));
        private readonly long startSequence = startSequence;
        private readonly long endSequenceExclusive = endSequenceExclusive;
        private readonly long resetPosition = stream.Position;

        public readonly WriteAheadLogEntry Current => current ?? throw new InvalidOperationException();

        readonly object IEnumerator.Current => Current;

        public readonly void Dispose() => stream.Dispose();

        public bool MoveNext()
        {
            // if the next entry cannot possibly be in the range,
            // don't even try to read it.
            if (current.HasValue && current.Value.Sequence == endSequenceExclusive - 1)
            {
                current = default;
                return false;
            }

            for (; ; )
            {
                var spos = stream.Position;
                bool readok = LogSerialization.TryReadEntry(stream, out var entry);

                if (readok && entry.Sequence < startSequence)
                {
                    continue;
                }

                if (!readok || entry.Sequence >= endSequenceExclusive)
                {
                    current = default;

                    // rewind until before the last read.
                    if (stream.CanSeek)
                    {
                        stream.Position = spos;
                    }

                    return false;
                }

                current = entry;
                return true;
            }
        }

        public void Reset()
        {
            stream.Position = resetPosition;
            current = default;
        }
    }
}
