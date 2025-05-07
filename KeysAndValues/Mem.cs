using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace KeysAndValues;

/// <summary>
/// Mem represents a pinned region of memory. Underneath it has a pointer and length.
/// The data is completely immutable from the perspective of the Mem structure.
/// </summary>
public unsafe readonly struct Mem : IEquatable<Mem>, IComparable<Mem>
{
    private readonly byte* data;
    private readonly long length;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Mem(byte* data, long length)
    {
        Debug.Assert((data == null) == (length == 0));
        this.data = data;
        this.length = length;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Mem(nint data, long length)
    {
        Debug.Assert((data == 0) == (length == 0));
        this.data = (byte*)data;
        this.length = length;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Mem(System.Buffers.MemoryHandle mh, long length)
    {
        Debug.Assert((mh.Pointer == null) == (length == 0));
        data = (byte*)mh.Pointer;
        this.length = length;
    }

    public ReadOnlySpan<byte> Span
    {
        get
        {
            if (length == 0)
            {
                return default;
            }

            return new(data, (int)length);
        }
    }

    public bool IsNull => length == 0;

    public int Length => checked((int)length);
    public long LongLength => length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> As<T>() where T : unmanaged
    {
        Debug.Assert(length % sizeof(T) == 0);
        return new((T*)data, (int)length / sizeof(T));
    }

    public bool Equals(Mem other)
    {
        if (length != other.length)
        {
            return false;
        }

        if (length == 0)
        {
            return true;
        }

        if (data == other.data)
        {
            return true;
        }

        return Span.SequenceEqual(other.Span);
    }

    public int CompareTo(Mem other)
    {
        if (length != other.length)
        {
            return length < other.length ? -1 : 1;
        }

        if (length == 0)
        {
            return 0;
        }

        if (data == other.data)
        {
            return 0;
        }

        return Span.SequenceCompareTo(other.Span);
    }

    public override bool Equals(object? obj) =>
        obj is Mem other && Equals(other);

    public unsafe override int GetHashCode()
    {
        uint hash = unchecked((uint)length ^ (uint)(length >> 32));

        int index = 0;
        while (index + 4 <= length)
        {
            hash = BitOperations.RotateLeft(hash + ((uint*)data)[index / 4] * 3266489917U, 17) * 668265263U;
            index += 4;
        }

        while (index < length)
        {
            hash = BitOperations.RotateLeft(hash + data[index] * 2654435761U, 13) * 2246822519U;
            index++;
        }

        return unchecked((int)hash);
    }

    public override string ToString() => $"[{length} bytes]";

    public static unsafe implicit operator ReadOnlySpan<byte>(in Mem mem) => new(mem.data, checked((int)mem.length));
    public static bool operator ==(Mem left, Mem right) => left.Equals(right);
    public static bool operator !=(Mem left, Mem right) => !(left == right);
    public static bool operator <(Mem left, Mem right) => left.CompareTo(right) < 0;
    public static bool operator <=(Mem left, Mem right) => left.CompareTo(right) <= 0;
    public static bool operator >(Mem left, Mem right) => left.CompareTo(right) > 0;
    public static bool operator >=(Mem left, Mem right) => left.CompareTo(right) >= 0;
}
