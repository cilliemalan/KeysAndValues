namespace KeysAndValues;

#pragma warning disable IDE0301 // Simplify collection initialization

/// <summary>
/// A snapshot in time of the data in a <see cref="KeyValueStore"/>.
/// </summary>
public sealed class StoreVersion : IEquatable<StoreVersion>, IComparable<StoreVersion>
{
    /// <summary>
    /// An empty version.
    /// </summary>
    public static readonly StoreVersion Empty = new();

    /// <summary>
    /// The sequence number of this version.
    /// </summary>
    public long Sequence { get; }

    /// <summary>
    /// The data in this version.
    /// </summary>
    public ImmutableAvlTree<Mem, Mem> Data { get; }

    /// <summary>
    /// Create an empty store version.
    /// </summary>
    public StoreVersion()
    {
        Sequence = 0;
        Data = ImmutableAvlTree<Mem, Mem>.Empty;
    }

    /// <summary>
    /// Create a store version based on some data.
    /// </summary>
    /// <param name="sequence">The sequence number of the version.</param>
    /// <param name="data">The data of the version.</param>
    /// <exception cref="ArgumentNullException">Something was null.</exception>
    public StoreVersion(long sequence, ImmutableAvlTree<Mem, Mem> data)
    {
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        Sequence = sequence;
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <inheritdoc />
    public int CompareTo(StoreVersion? other) => Sequence.CompareTo(other?.Sequence ?? -1);
    /// <inheritdoc />
    public bool Equals(StoreVersion? other) => other is not null && other.Sequence == Sequence && ReferenceEquals(Data, other.Data);
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is StoreVersion other && other.Sequence == Sequence && ReferenceEquals(Data, other.Data);
    /// <inheritdoc />
    public override int GetHashCode() => ~Sequence.GetHashCode();
    /// <inheritdoc />
    override public string ToString() => $"StoreVersion(Sequence={Sequence}, Count={Data.Count})";

    /// <inheritdoc />
    public static bool operator ==(StoreVersion? left, StoreVersion? right) => left?.Equals(right) ?? right is null;
    /// <inheritdoc />
    public static bool operator !=(StoreVersion? left, StoreVersion? right) => !(left == right);
    /// <inheritdoc />
    public static bool operator <(StoreVersion? left, StoreVersion? right) => left?.CompareTo(right) < 0;
    /// <inheritdoc />
    public static bool operator >(StoreVersion? left, StoreVersion? right) => left?.CompareTo(right) > 0;
    /// <inheritdoc />
    public static bool operator <=(StoreVersion? left, StoreVersion? right) => left?.CompareTo(right) <= 0;
    /// <inheritdoc />
    public static bool operator >=(StoreVersion? left, StoreVersion? right) => left?.CompareTo(right) >= 0;
}
