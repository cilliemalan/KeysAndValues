using System.Runtime.InteropServices;

namespace KeysAndValues;

/// <summary>
/// Mem represents a pinned region of memory. Underneath it has a pointer and length.
/// The data is completely immutable from the perspective of the Mem structure.
/// </summary>
public readonly struct Mem : IEquatable<Mem>, IComparable<Mem>
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

    public override int GetHashCode()
    {
        var u8data = memory.Span;
        var u32data = MemoryMarshal.Cast<byte, uint>(u8data);
        
        uint l = (uint)memory.Length;
        uint hash = l;

        int index = 0;
        while (index + 4 <= l)
        {
            hash = System.Numerics.BitOperations.RotateLeft(hash + u32data[index / 4] * 3266489917U, 17) * 668265263U;
            index += 4;
        }

        while (index < l)
        {
            hash = System.Numerics.BitOperations.RotateLeft(hash + u8data[index] * 2654435761U, 13) * 2246822519U;
            index++;
        }

        return unchecked((int)hash);
    }

    public override string ToString() => $"[{memory.Length} bytes]";

    internal bool TryGetArray(out ArraySegment<byte> segment)
        => MemoryMarshal.TryGetArray(memory, out segment);

    public static implicit operator ReadOnlySpan<byte>(in Mem mem) => mem.memory.Span;
    public static implicit operator ReadOnlyMemory<byte>(in Mem mem) => mem.memory;
    public static implicit operator Mem(in ReadOnlyMemory<byte> mem) => new(mem);
    public static implicit operator Mem(string mem) => new(Internal.StringToBytesConversion.GetBytes(mem));
    public static implicit operator string(Mem mem) => Internal.StringToBytesConversion.GetString(mem);

    public static bool operator ==(Mem left, Mem right) => left.Equals(right);
    public static bool operator !=(Mem left, Mem right) => !(left == right);
    public static bool operator <(Mem left, Mem right) => left.CompareTo(right) < 0;
    public static bool operator <=(Mem left, Mem right) => left.CompareTo(right) <= 0;
    public static bool operator >(Mem left, Mem right) => left.CompareTo(right) > 0;
    public static bool operator >=(Mem left, Mem right) => left.CompareTo(right) >= 0;
}
