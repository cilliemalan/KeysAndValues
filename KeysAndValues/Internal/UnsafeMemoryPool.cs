using Microsoft.VisualBasic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace KeysAndValues.Internal;

/// <summary>
/// Represents memory that can be allocated in pieces but is freed as a whole.
/// Disposing the memory pool renders all previously allocated memories undefined.
/// </summary>
public unsafe sealed class UnsafeMemoryPool : IDisposable
{
    private int disposed;
    private readonly ConcurrentBag<nint> allocations = [];
    private long total;

    public long TotalAllocated => total;

    public Mem Allocate(int length, Action<Span<byte>> initializer)
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 0);

        if (length == 0)
        {
            return default;
        }

        void* tr = NativeMemory.AlignedAlloc((nuint)length, 8);
        allocations.Add((nint)tr);
        Interlocked.Add(ref total, length);
        initializer(new(tr, (int)length));
        return new((byte*)tr, length);
    }

    public Mem Allocate(ReadOnlySpan<byte> contents)
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);

        var length = contents.Length;
        void* tr = NativeMemory.AlignedAlloc((nuint)length, 8);
        allocations.Add((nint)tr);
        Interlocked.Add(ref total, length);
        contents.CopyTo(new(tr, length));
        return new((byte*)tr, length);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0)
        {
            var allAllocations = allocations.ToArray();
            foreach (var allocation in allAllocations)
            {
                NativeMemory.AlignedFree((void*)allocation);
            }
        }
    }
}
