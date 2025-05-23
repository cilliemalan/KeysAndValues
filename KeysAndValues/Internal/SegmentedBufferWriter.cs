namespace KeysAndValues.Internal;

/// <summary>
/// A buffer writer that creates segments of memory as required
/// rather than resizeing and reallocating a single buffer.
/// </summary>
public sealed class SegmentedBufferWriter<T>(int minimumSegmentSize = 256) : IBufferWriter<T>, IDisposable
{
    private readonly int minimumSegmentSize = minimumSegmentSize;
    Segment[] segments = [];
    int segmentIndex = 0;
    int length = 0;

    public int Length => length;

    public ReadOnlySequence<T> WrittenSequence
    {
        get
        {
            ObjectDisposedException.ThrowIf(segments is null, this);

            if (segmentIndex == 0)
            {
                if (segments.Length == 0)
                {
                    return default;
                }

                return new(segments[0].array, 0, segments[0].writtenLength);
            }

            SegmentRef? lastSeg = null;
            SegmentRef? nextSeg = null;
            var totalLength = length;
            for (int i = 0; i < segments.Length; i++)
            {
                ref var segment = ref segments[segments.Length - i - 1];
                if (segment.writtenLength == 0)
                {
                    continue;
                }

                totalLength -= segment.writtenLength;
                nextSeg = new SegmentRef(
                    new ReadOnlyMemory<T>(segment.array, 0, segment.writtenLength),
                    nextSeg,
                    totalLength);
                lastSeg ??= nextSeg;
            }

            if (nextSeg is null || lastSeg is null)
            {
                return default;
            }

            return new(nextSeg, 0, lastSeg, lastSeg.Memory.Length);
        }
    }

    public void Advance(int count)
    {
        ObjectDisposedException.ThrowIf(segments is null, this);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);
        if (segmentIndex >= segments.Length)
        {
            throw new InvalidOperationException();
        }

        ref var segment = ref segments[segmentIndex];
        if (segment.array == null)
        {
            throw new InvalidOperationException();
        }

        if (segment.writtenLength + count > segment.array.Length)
        {
            throw new InvalidOperationException();
        }

        segment.writtenLength += count;
        length += count;
    }

    public void Dispose()
    {
        var s = segments;
        if (s is null)
        {
            return;
        }

        segments = null!;
        foreach (var segment in s)
        {
            segment.Dispose();
        }
    }

    public Memory<T> GetMemory(int sizeHint = 0)
    {
        ObjectDisposedException.ThrowIf(segments is null, this);

        if (sizeHint == 0)
        {
            sizeHint = 1;
        }

        if (segmentIndex >= segments.Length)
        {
            Array.Resize(ref segments, Math.Max(4, segments.Length * 2 - segments.Length / 2));
        }

        ref var segment = ref segments[segmentIndex];
        if (segment.array == null)
        {
            segment = new(Math.Max(minimumSegmentSize, sizeHint));
        }

        var spaceLeftInSegment = segment.array.Length - segment.writtenLength;
        if (spaceLeftInSegment < sizeHint)
        {
            segmentIndex++;
            length = 0;
            return GetMemory(sizeHint);
        }

        return new Memory<T>(segment.array, segment.writtenLength, spaceLeftInSegment);
    }

    public Span<T> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;

    struct Segment(int size) : IDisposable
    {
        public T[] array = ArrayPool<T>.Shared.Rent(size);
        public int writtenLength = 0;

        public void Dispose()
        {
            if (array is not null)
            {
                ArrayPool<T>.Shared.Return(array);
                array = null!;
            }
            writtenLength = 0;
        }
    }

    class SegmentRef : ReadOnlySequenceSegment<T>
    {
        public SegmentRef(ReadOnlyMemory<T> memory, SegmentRef? next, int runningIndex)
        {
            Memory = memory;
            Next = next;
            RunningIndex = runningIndex;
        }
    }
}
