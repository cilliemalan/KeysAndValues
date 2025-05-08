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
    private readonly ReadOnlyMemory<byte> memory;

    public Mem(ReadOnlyMemory<byte> data)
    {
        memory = data;
    }

    public Mem(byte[] data, int index, int length)
    {
        memory = new(data, index, length);
    }

    public ReadOnlySpan<byte> Span => memory.Span;

    public bool IsEmpty => memory.Length == 0;

    public int Length => memory.Length;

    public bool Equals(Mem other)
    {
        if (memory.Length != other.memory.Length)
        {
            return false;
        }

        if (memory.Length == 0)
        {
            return true;
        }

        if (memory.Equals(other.memory))
        {
            return true;
        }

        return memory.Span.SequenceEqual(other.memory.Span);
    }

    public int CompareTo(Mem other)
    {
        if (memory.Length == 0)
        {
            return 0;
        }

        if (memory.Equals(other.memory))
        {
            return 0;
        }

        return memory.Span.SequenceCompareTo(other.memory.Span);
    }

    public override bool Equals(object? obj) =>
        obj is Mem other && Equals(other);

    public unsafe override int GetHashCode()
    {
        uint l = (uint)memory.Length;
        uint hash = l;

        using var mh = memory.Pin();
        byte* data = (byte*)mh.Pointer;
        uint index = 0;
        while (index + 4 <= l)
        {
            hash = BitOperations.RotateLeft(hash + ((uint*)data)[index / 4] * 3266489917U, 17) * 668265263U;
            index += 4;
        }

        while (index < l)
        {
            hash = BitOperations.RotateLeft(hash + data[index] * 2654435761U, 13) * 2246822519U;
            index++;
        }

        return unchecked((int)hash);
    }

    public override string ToString() => $"[{memory.Length} bytes]";

    public static implicit operator ReadOnlySpan<byte>(in Mem mem) => mem.memory.Span;
    public static implicit operator ReadOnlyMemory<byte>(in Mem mem) => mem.memory;
    public static implicit operator Mem(ReadOnlyMemory<byte> mem) => new(mem);
    public static bool operator ==(Mem left, Mem right) => left.Equals(right);
    public static bool operator !=(Mem left, Mem right) => !(left == right);
    public static bool operator <(Mem left, Mem right) => left.CompareTo(right) < 0;
    public static bool operator <=(Mem left, Mem right) => left.CompareTo(right) <= 0;
    public static bool operator >(Mem left, Mem right) => left.CompareTo(right) > 0;
    public static bool operator >=(Mem left, Mem right) => left.CompareTo(right) >= 0;
}
