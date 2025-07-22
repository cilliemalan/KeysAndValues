namespace KeysAndValues;

#pragma warning disable IDE0301 // Simplify collection initialization

public sealed class StoreVersion : IEquatable<StoreVersion>, IComparable<StoreVersion>
{
    public static readonly StoreVersion Empty = new();

    public long Sequence { get; }
    public ImmutableAvlTree<Mem, Mem> Data { get; }

    public StoreVersion()
    {
        Sequence = 0;
        Data = ImmutableAvlTree<Mem, Mem>.Empty;
    }

    public StoreVersion(long sequence, ImmutableAvlTree<Mem, Mem> data)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sequence, 0);
        Sequence = sequence;
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public int CompareTo(StoreVersion? other) => Sequence.CompareTo(other?.Sequence ?? -1);
    public bool Equals(StoreVersion? other) => other is not null && other.Sequence == Sequence && ReferenceEquals(Data, other.Data);
    public override bool Equals(object? obj) => obj is StoreVersion other && other.Sequence == Sequence && ReferenceEquals(Data, other.Data);
    public override int GetHashCode() => ~Sequence.GetHashCode();
    override public string ToString() => $"StoreVersion(Sequence={Sequence}, Count={Data.Count})";

    public static bool operator ==(StoreVersion? left, StoreVersion? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(StoreVersion? left, StoreVersion? right) => !(left == right);
    public static bool operator <(StoreVersion? left, StoreVersion? right) => left?.CompareTo(right) < 0;
    public static bool operator >(StoreVersion? left, StoreVersion? right) => left?.CompareTo(right) > 0;
    public static bool operator <=(StoreVersion? left, StoreVersion? right) => left?.CompareTo(right) <= 0;
    public static bool operator >=(StoreVersion? left, StoreVersion? right) => left?.CompareTo(right) >= 0;
}
