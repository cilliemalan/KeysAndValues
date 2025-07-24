namespace KeysAndValues;

/// <summary>
/// A change operation.
/// </summary>
public readonly struct ChangeOperation<TKey, TValue> : IEquatable<ChangeOperation<TKey, TValue>>, IComparable<ChangeOperation<TKey, TValue>>
    where TValue : IComparable<TValue>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// The type of the operation.
    /// </summary>
    public ChangeOperationType Type { get; init; }

    /// <summary>
    /// The key
    /// </summary>
    public TKey Key { get; init; }

    /// <summary>
    /// The value
    /// </summary>
    public TValue Value { get; init; }

    /// <inheritdoc />
    public int CompareTo(ChangeOperation<TKey, TValue> other)
    {
        int c = Type.CompareTo(other.Type);
        if (c != 0) return c;
        c = Key.CompareTo(other.Key);
        if (c != 0) return c;
        if (Type == ChangeOperationType.Delete)
        {
            return 0;
        }
        return Value.CompareTo(other.Value);
    }

    /// <inheritdoc />
    public bool Equals(ChangeOperation<TKey, TValue> other)
    {
        return Type == other.Type &&
            Key.Equals(other.Key) &&
            (Type == ChangeOperationType.Delete ||
                Value.Equals(other.Value));
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is ChangeOperation<TKey, TValue> cop && Equals(cop);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        int a = Type.GetHashCode();
        int b = Key.GetHashCode();
        int c = Type == ChangeOperationType.Delete ? Value.GetHashCode() : 0;
        b = (int)((uint)b << 13 | (uint)b >> 19);
        c = (int)((uint)c << 15 | (uint)c >> 17);
        return a | (b * 1246132673) | ((~c) * -17);
    }
}

/// <summary>
/// A few methods for working with change operations.
/// </summary>
public static class ChangeOperation
{
    /// <summary>
    /// Create a new set operation
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static ChangeOperation<TKey, TValue> Set<TKey, TValue>(TKey key, TValue value)
        where TValue : IComparable<TValue>
        where TKey : IComparable<TKey>
        => new() { Type = ChangeOperationType.Set, Key = key, Value = value };

    /// <summary>
    /// Create a new add operation.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static ChangeOperation<TKey, TValue> Add<TKey, TValue>(TKey key, TValue value)
        where TValue : IComparable<TValue>
        where TKey : IComparable<TKey>
        => new() { Type = ChangeOperationType.Add, Key = key, Value = value };

    /// <summary>
    /// Create a new delete operation.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public static ChangeOperation<TKey, TValue> Delete<TKey, TValue>(TKey key)
        where TValue : IComparable<TValue>
        where TKey : IComparable<TKey>
        => new() { Type = ChangeOperationType.Delete, Key = key };
}