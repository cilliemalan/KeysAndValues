namespace KeysAndValues;

public readonly struct WriteAheadLogEntry: IComparable<WriteAheadLogEntry>, IEquatable<WriteAheadLogEntry>
{
    public long Sequence { get; init; }
    public ChangeOperation[] ChangeOperations { get; init; }

    public override int GetHashCode() => Sequence.GetHashCode();

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is WriteAheadLogEntry logEntry && logEntry.Sequence == Sequence;
    }

    public int CompareTo(WriteAheadLogEntry other) => Sequence.CompareTo(other.Sequence);

    public bool Equals(WriteAheadLogEntry logEntry) => logEntry.Sequence == Sequence;

    public static bool operator ==(in WriteAheadLogEntry left, in WriteAheadLogEntry right) 
        => left.Sequence == right.Sequence;

    public static bool operator !=(in WriteAheadLogEntry left, in WriteAheadLogEntry right) 
        => left.Sequence != right.Sequence;

    public static bool operator <(in WriteAheadLogEntry left, in WriteAheadLogEntry right) 
        => left.Sequence < right.Sequence;

    public static bool operator >(in WriteAheadLogEntry left, in WriteAheadLogEntry right) 
        => left.Sequence > right.Sequence;

    public static bool operator <=(in WriteAheadLogEntry left, in WriteAheadLogEntry right)
        => left.Sequence <= right.Sequence;

    public static bool operator >=(in WriteAheadLogEntry left, in WriteAheadLogEntry right)
        => left.Sequence >= right.Sequence;

    public class Comparer : IComparer<WriteAheadLogEntry>, IEqualityComparer<WriteAheadLogEntry>
    {
        public static readonly Comparer Instance = new();

        public int Compare(WriteAheadLogEntry x, WriteAheadLogEntry y)
            => x.Sequence.CompareTo(y.Sequence);

        public bool Equals(WriteAheadLogEntry x, WriteAheadLogEntry y)
            => x.Sequence == y.Sequence;

        public int GetHashCode(WriteAheadLogEntry obj)
            => obj.Sequence.GetHashCode();
    }
}
