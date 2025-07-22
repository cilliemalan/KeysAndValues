namespace KeysAndValues;

public sealed class StoreVersion : IEquatable<StoreVersion>, IComparable<StoreVersion>
{
    public long Sequence { get; }
    public ImmutableAvlTree<Mem, Mem> Data { get; }

    public StoreVersion(long sequence, ImmutableAvlTree<Mem, Mem> data)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sequence, 0);
        Sequence = sequence;
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public int CompareTo(StoreVersion? other) => Sequence.CompareTo(other?.Sequence ?? -1);
    public bool Equals(StoreVersion? other) => other is not null && other.Sequence == Sequence;
    public override bool Equals(object? obj) => obj is StoreVersion other && other.Sequence == Sequence;
    public override int GetHashCode() => ~Sequence.GetHashCode();
    override public string ToString() => $"StoreVersion(Sequence={Sequence}, Count={Data.Count})";

    public static bool operator ==(StoreVersion? left, StoreVersion? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(StoreVersion? left, StoreVersion? right) => !(left == right);
    public static bool operator <(StoreVersion? left, StoreVersion? right) => left?.CompareTo(right) < 0;
    public static bool operator >(StoreVersion? left, StoreVersion? right) => left?.CompareTo(right) > 0;
    public static bool operator <=(StoreVersion? left, StoreVersion? right) => left?.CompareTo(right) <= 0;
    public static bool operator >=(StoreVersion? left, StoreVersion? right) => left?.CompareTo(right) >= 0;
}
