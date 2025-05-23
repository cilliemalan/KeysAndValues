using System.Diagnostics.CodeAnalysis;

namespace KeysAndValues;

public readonly struct WriteAheadLogEntry: IComparable<WriteAheadLogEntry>, IEquatable<WriteAheadLogEntry>
{
    public WriteAheadLogEntryType Type { get; init; }
    public long Sequence { get; init; }
    public ChangeOperation[]? ChangeOperations { get; init; }
    public ImmutableAvlTree<Mem, Mem>? Snapshot { get; init; }

    public override int GetHashCode()
    {
        return Sequence.GetHashCode() ^ Type.GetHashCode();
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is WriteAheadLogEntry logEntry &&
            logEntry.Sequence == Sequence &&
            logEntry.Type == Type &&
            (ChangeOperations is not null) == (logEntry.ChangeOperations is not null) &&
            (Snapshot is not null) == (logEntry.Snapshot is not null);
    }

    public int CompareTo(WriteAheadLogEntry other)
    {
        if (Sequence < other.Sequence)
        {
            return -1;
        }
        else if (Sequence > other.Sequence)
        {
            return 1;
        }
        else
        {
            return Type.CompareTo(other.Type);
        }
    }

    public bool Equals(WriteAheadLogEntry logEntry)
    {
        return logEntry.Sequence == Sequence &&
            logEntry.Type == Type &&
            (ChangeOperations is not null) == (logEntry.ChangeOperations is not null) &&
            (Snapshot is not null) == (logEntry.Snapshot is not null);
    }

    public static bool operator ==(WriteAheadLogEntry left, WriteAheadLogEntry right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WriteAheadLogEntry left, WriteAheadLogEntry right)
    {
        return !(left == right);
    }

    public static bool operator <(WriteAheadLogEntry left, WriteAheadLogEntry right)
    {
        return Comparer.Instance.Compare(left, right) < 0;
    }

    public static bool operator >(WriteAheadLogEntry left, WriteAheadLogEntry right)
    {
        return Comparer.Instance.Compare(left, right) > 0;
    }

    public static bool operator <=(WriteAheadLogEntry left, WriteAheadLogEntry right)
    {
        return Comparer.Instance.Compare(left, right) <= 0;
    }

    public static bool operator >=(WriteAheadLogEntry left, WriteAheadLogEntry right)
    {
        return Comparer.Instance.Compare(left, right) >= 0;
    }

    public class Comparer : IComparer<WriteAheadLogEntry>, IEqualityComparer<WriteAheadLogEntry>
    {
        public static readonly Comparer Instance = new();

        public int Compare(WriteAheadLogEntry x, WriteAheadLogEntry y)
        {
            if (x.Sequence < y.Sequence)
            {
                return -1;
            }
            else if (x.Sequence > y.Sequence)
            {
                return 1;
            }
            else
            {
                return x.Type.CompareTo(y.Type);
            }
        }

        public bool Equals(WriteAheadLogEntry x, WriteAheadLogEntry y)
        {
            return x.Sequence == y.Sequence &&
                x.Type == y.Type &&
                (x.ChangeOperations is not null) == (y.ChangeOperations is not null) &&
                (x.Snapshot is not null) == (y.Snapshot is not null);
        }

        public int GetHashCode([DisallowNull] WriteAheadLogEntry obj)
        {
            return obj.Sequence.GetHashCode() ^ obj.Type.GetHashCode();
        }
    }
}
