namespace KeysAndValues;

/// <summary>
/// Extension methods for <see cref="KeyValueStore"/>.
/// </summary>
public static class KeyValueStoreExtensions
{
    /// <summary>
    /// Get an item from the store.
    /// </summary>
    /// <param name="store">The store</param>
    /// <param name="mem">The key</param>
    /// <returns>The item.</returns>
    /// <exception cref="KeyNotFoundException">The item was not found.</exception>
    public static Mem Get(this KeyValueStore store, Mem mem)
    {
        if (store.TryGet(mem, out var value))
        {
            return value;
        }
        throw new KeyNotFoundException($"Key not found in the store.");
    }

    /// <summary>
    /// Set an item in the store.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>The new store version</returns>
    public static StoreVersion Set(this KeyValueStore store, Mem key, Mem value)
    {
        return store.Apply([new()
        {
            Type = ChangeOperationType.Set,
            Key = key,
            Value = value
        }]);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="store"></param>
    /// <param name="keys"></param>
    /// <returns>The new store version</returns>
    public static StoreVersion Delete(this KeyValueStore store, IEnumerable<Mem> keys)
    {
        int nkcap = keys is ICollection<Mem> ckeys ? ckeys.Count : 1;
        ChangeOperation<Mem, Mem>[] ops = ArrayPool<ChangeOperation<Mem, Mem>>.Shared.Rent(nkcap);
        try
        {
            int nkeys = 0;
            foreach (var k in keys)
            {
                if (nkeys >= ops.Length)
                {
                    var nops = ArrayPool<ChangeOperation<Mem, Mem>>.Shared.Rent(nkeys + nkeys / 2 + 1);
                    Array.Copy(ops, 0, nops, 0, nkeys);
                    ArrayPool<ChangeOperation<Mem, Mem>>.Shared.Return(ops);
                    ops = nops;
                }

                ops[nkeys++] = new()
                {
                    Type = ChangeOperationType.Delete,
                    Key = k
                };
            }

            return store.Apply(ops.AsSpan(0, nkeys));
        }
        finally
        {
            ArrayPool<ChangeOperation<Mem, Mem>>.Shared.Return(ops);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="store"></param>
    /// <param name="key"></param>
    /// <returns>The new store version</returns>
    public static StoreVersion Delete(this KeyValueStore store, Mem key)
    {
        return store.Apply([new()
        {
            Type = ChangeOperationType.Delete,
            Key = key
        }]);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="store"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>The new store version</returns>
    /// <exception cref="ArgumentException"></exception>
    public static StoreVersion Set(this KeyValueStore store, ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
    {
        if (key.Length == 0)
        {
            throw new ArgumentException("Key can not be empty", nameof(key));
        }

        var mem = new byte[key.Length + value.Length];
        key.CopyTo(mem);
        value.CopyTo(mem.AsSpan(key.Length));

        return store.Apply([new()
        {
            Type = ChangeOperationType.Set,
            Key = new(mem, 0, key.Length),
            Value = value.Length > 0 ? new(mem, key.Length, value.Length) : default
        }]);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="store"></param>
    /// <param name="entries"></param>
    /// <returns>The new store version</returns>
    public static StoreVersion Set(this KeyValueStore store, IEnumerable<KeyValuePair<Mem, Mem>> entries)
    {
        int nkeys = 0;
        int nkcap = entries is ICollection<KeyValuePair<Mem, Mem>> kc ? kc.Count : 1;
        var ops = ArrayPool<ChangeOperation<Mem, Mem>>.Shared.Rent(nkcap);
        try
        {
            foreach (var entry in entries)
            {
                if (nkeys >= ops.Length)
                {
                    var nops = ArrayPool<ChangeOperation<Mem, Mem>>.Shared.Rent(nkeys + nkeys / 2 + 1);
                    Array.Copy(ops, 0, nops, 0, nkeys);
                    ArrayPool<ChangeOperation<Mem, Mem>>.Shared.Return(ops);
                    ops = nops;
                }

                ops[nkeys++] = new()
                {
                    Type = ChangeOperationType.Set,
                    Key = entry.Key,
                    Value = entry.Value
                };
            }

            return store.Apply(ops.AsSpan(0, nkeys));
        }
        finally
        {
            ArrayPool<ChangeOperation<Mem, Mem>>.Shared.Return(ops);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="store"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>The new store version</returns>
    public static StoreVersion Set(this KeyValueStore store, ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value)
    {
        return store.Apply([new() { Type = ChangeOperationType.Set, Key = key, Value = value }]);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="store"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static bool ContainsKey(this KeyValueStore store, Mem key)
    {
        return store.Snapshot().Data.ContainsKey(key);
    }

    internal static void Apply(this ImmutableAvlTree<Mem, Mem>.Builder builder, ReadOnlySpan<ChangeOperation<Mem, Mem>> operations)
    {
        for (int i = 0; i < operations.Length; i++)
        {
            var operation = operations[i];

            switch (operation.Type)
            {
                case ChangeOperationType.Add:
                    //builder.Add(operation.Key, operation.Value);
                    builder[operation.Key] = operation.Value;
                    break;
                case ChangeOperationType.Set:
                    builder[operation.Key] = operation.Value;
                    break;
                case ChangeOperationType.Delete:
                    builder.Remove(operation.Key);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operations));
            }
        }
    }

    /// <summary>
    /// Apply a set of changes to an <see cref="ImmutableAvlTree{TKey, TValue}"/>.
    /// </summary>
    /// <param name="store">The data structure to apply the changes to.</param>
    /// <param name="operations">The operations to apply</param>
    /// <returns>A new <see cref="ImmutableAvlTree{TKey, TValue}"/> representing the tree after the changes.</returns>
    public static ImmutableAvlTree<Mem, Mem> Apply(this ImmutableAvlTree<Mem, Mem> store, ReadOnlySpan<ChangeOperation<Mem, Mem>> operations)
    {
        var builder = store.ToBuilder();
        builder.Apply(operations);
        return builder.ToImmutable();
    }

    /// <summary>
    /// Enumerate the keys and values in a store.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <returns>An enumerable for the items in the store.</returns>
    public static IEnumerable<KeyValuePair<Mem, Mem>> Enumerate(this KeyValueStore store) => store.Data;

    /// <summary>
    /// Enumerate the keys  in a store.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <returns>An enumerable for the keys in the store.</returns>
    public static IEnumerable<Mem> Keys(this KeyValueStore store) => store.Data.Keys;

    /// <summary>
    /// Enumerate the values in a store.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <returns>An enumerable for the values in the store.</returns>
    public static IEnumerable<Mem> Values(this KeyValueStore store) => store.Data.Values;

    /// <summary>
    /// Return an enumerable that allows range enumeration over the store.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="fromKeyInclusive">The largest key to include.</param>
    /// <param name="toKeyExclusive">The smallest key to exclude.</param>
    /// <returns>An enumerable for the items in the store.</returns>
    public static IEnumerable<KeyValuePair<Mem, Mem>> Enumerate(this KeyValueStore store, Mem fromKeyInclusive, Mem toKeyExclusive)
    {
        var k = store.Data;
        if (toKeyExclusive.IsEmpty)
        {
            // the "default" Mem will always sort before the first entry in the database
            return k.Range(fromKeyInclusive);
        }

        return k.Range(fromKeyInclusive, toKeyExclusive);
    }

    /// <summary>
    /// Returns an enumerable that enumerates all items with a key with a given prefix.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="prefix">The prefix.</param>
    /// <returns>An enumerable for the items in the store.</returns>
    public static IEnumerable<KeyValuePair<Mem, Mem>> EnumeratePrefix(this KeyValueStore store, Mem prefix)
    {
        return store.Data.EnumeratePrefix(prefix);
    }

    /// <summary>
    /// Returns an enumerable that enumerates all items with a key with a given prefix.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="prefix">The prefix.</param>
    /// <returns>An enumerable for the items in the store.</returns>
    private static IEnumerable<KeyValuePair<Mem, Mem>> EnumeratePrefix(this ImmutableAvlTree<Mem, Mem> store, Mem prefix)
    {
        if (prefix.IsEmpty)
        {
            return store;
        }

        var mem = new byte[prefix.Length];
        prefix.Span.CopyTo(mem);
        if (Increment(mem))
        {
            var prefixEnd = new Mem(mem);
            return store.Range(prefix, prefixEnd);
        }
        else
        {
            // it's not possible to add an item to the dictionary
            // that has the same prefix but sorts past it.
            return store.Range(prefix);
        }
    }

    private static bool Increment(Span<byte> mem)
    {
        for (int i = 0; i < mem.Length; i++)
        {
            var newbyte = ++mem[mem.Length - 1 - i];

            if (newbyte != 0)
            {
                return true;
            }
        }

        return false;
    }
}