using System.Runtime.InteropServices;

namespace KeysAndValues;

/// <summary>
/// Mem represents a region of memory.
/// </summary>
/// <remarks>
/// <para>The data is completely immutable from the perspective of the Mem structure.</para>
/// <para>Mem objects have value equality with one another and can be compared to one another.</para>
/// </remarks>
public readonly struct Mem : IEquatable<Mem>, IComparable<Mem>
{
    private readonly ReadOnlyMemory<byte> memory;

    /// <summary>
    /// Create a Mem from a byte array.
    /// </summary>
    /// <param name="data">The data</param>
    public Mem(ReadOnlyMemory<byte> data)
    {
        memory = data;
    }

    /// <summary>
    /// Create a Mem from a byte array.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="index">The index.</param>
    /// <param name="length">The length.</param>
    public Mem(byte[] data, int index, int length)
    {
        memory = new(data, index, length);
    }

    /// <summary>
    /// Get a span representing the data in this Mem.
    /// </summary>
    public ReadOnlySpan<byte> Span => memory.Span;

    /// <summary>
    /// Gets whether or not this mem is empty.
    /// </summary>
    public bool IsEmpty => memory.Length == 0;

    /// <summary>
    /// Gets the length of this mem.
    /// </summary>
    public int Length => memory.Length;

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is Mem other && Equals(other);

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override string ToString() => $"[{memory.Length} bytes]";

    internal bool TryGetArray(out ArraySegment<byte> segment)
        => MemoryMarshal.TryGetArray(memory, out segment);

    /// <inheritdoc />
    public static implicit operator ReadOnlySpan<byte>(in Mem mem) => mem.memory.Span;
    /// <inheritdoc />
    public static implicit operator ReadOnlyMemory<byte>(in Mem mem) => mem.memory;
    /// <inheritdoc />
    public static implicit operator Mem(in ReadOnlyMemory<byte> mem) => new(mem);
    /// <inheritdoc />
    public static implicit operator Mem(string mem) => new(Encoding.UTF8.GetBytes(mem));
    /// <inheritdoc />
    public static implicit operator string(Mem mem) => Encoding.UTF8.GetString(mem.memory.Span);

    /// <inheritdoc />
    public static bool operator ==(Mem left, Mem right) => left.Equals(right);
    /// <inheritdoc />
    public static bool operator !=(Mem left, Mem right) => !(left == right);
    /// <inheritdoc />
    public static bool operator <(Mem left, Mem right) => left.CompareTo(right) < 0;
    /// <inheritdoc />
    public static bool operator <=(Mem left, Mem right) => left.CompareTo(right) <= 0;
    /// <inheritdoc />
    public static bool operator >(Mem left, Mem right) => left.CompareTo(right) > 0;
    /// <inheritdoc />
    public static bool operator >=(Mem left, Mem right) => left.CompareTo(right) >= 0;
}
