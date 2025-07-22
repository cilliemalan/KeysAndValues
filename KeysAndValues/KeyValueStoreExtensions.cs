namespace KeysAndValues;

public static class KeyValueStoreExtensions
{
    public static string Get(this KeyValueStore store, string key)
    {
        Mem kmem = key;
        if (store.TryGet(kmem, out var vmem))
        {
            return vmem;
        }
        throw new KeyNotFoundException($"Key '{key}' not found in the store.");
    }

    public static Mem Get(this KeyValueStore store, Mem mem)
    {
        if (store.TryGet(mem, out var value))
        {
            return value;
        }
        throw new KeyNotFoundException($"Key not found in the store.");
    }

    public static long Set(this KeyValueStore store, Mem key, Mem value)
    {
        return store.Apply([new()
        {
            Type = ChangeOperationType.Set,
            Key = key,
            Value = value
        }]);
    }

    public static long Delete(this KeyValueStore store, IEnumerable<Mem> keys)
    {
        int nkeys = 0;
        int nkcap = keys is ICollection<Mem> ckeys ? ckeys.Count : 1;
        ChangeOperation[] ops = ArrayPool<ChangeOperation>.Shared.Rent(nkcap);
        try
        {
            foreach (var k in keys)
            {
                if (nkeys >= ops.Length)
                {
                    ArrayPool<ChangeOperation>.Shared.Return(ops);
                    ops = ArrayPool<ChangeOperation>.Shared.Rent(nkeys + nkeys / 2 + 1);
                }

                ops[nkeys++] = new()
                {
                    Type = ChangeOperationType.Delete,
                    Key = k
                };
            }

            return store.Apply(ops.AsSpan(..nkeys));
        }
        finally
        {
            ArrayPool<ChangeOperation>.Shared.Return(ops);
        }
    }

    public static long Delete(this KeyValueStore store, Mem key)
    {
        return store.Apply([new()
        {
            Type = ChangeOperationType.Delete,
            Key = key
        }]);
    }

    public static long Set(this KeyValueStore store, ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
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

    public static long Set(this KeyValueStore store, IEnumerable<KeyValuePair<Mem, Mem>> entries)
    {
        int nkeys = 0;
        int nkcap = entries is ICollection<KeyValuePair<Mem, Mem>> kc ? kc.Count: 1;
        var ops = ArrayPool<ChangeOperation>.Shared.Rent(nkcap);
        try
        {
            foreach (var entry in entries)
            {
                if (nkeys >= ops.Length)
                {
                    ArrayPool<ChangeOperation>.Shared.Return(ops);
                    ops = ArrayPool<ChangeOperation>.Shared.Rent(nkeys + nkeys / 2 + 1);
                }

                ops[nkeys++] = new()
                {
                    Type = ChangeOperationType.Set,
                    Key = entry.Key,
                    Value = entry.Value
                };
            }

            return store.Apply(ops.AsSpan(..nkeys));
        }
        finally
        {
            ArrayPool<ChangeOperation>.Shared.Return(ops);
        }
    }

    public static long Set(this KeyValueStore store, ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value)
    {
        return store.Apply([new() { Type = ChangeOperationType.Set, Key = key, Value = value }]);
    }

    public static long Delete(this KeyValueStore store, ReadOnlySpan<byte> key)
    {
        return store.Apply([new() { Type = ChangeOperationType.Set, Key = key.ToArray(), Value = default }]);
    }

    public static void Apply(this ImmutableAvlTree<Mem, Mem>.Builder builder, ReadOnlySpan<ChangeOperation> operations)
    {
        for (int i = 0; i < operations.Length; i++)
        {
            var operation = operations[i];

            switch (operation.Type)
            {
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

    public static ImmutableAvlTree<Mem, Mem> Apply(this ImmutableAvlTree<Mem, Mem> store, ReadOnlySpan<ChangeOperation> operations)
    {
        var builder = store.ToBuilder();
        builder.Apply(operations);
        return builder.ToImmutable();
    }

    public static IEnumerable<KeyValuePair<Mem, Mem>> Enumerate(this KeyValueStore store) => store.Snapshot().Data;
    public static IEnumerable<Mem> Keys(this KeyValueStore store) => store.Snapshot().Data.Keys;
    public static IEnumerable<Mem> Values(this KeyValueStore store) => store.Snapshot().Data.Values;

    public static IEnumerable<KeyValuePair<Mem, Mem>> Enumerate(this KeyValueStore store, Mem fromKeyInclusive, Mem toKeyExclusive)
    {
        var k = store.Snapshot().Data;
        if (toKeyExclusive.IsEmpty)
        {
            // the "default" Mem will always sort before the first entry in the database
            return k.Range(fromKeyInclusive);
        }

        return k.Range(fromKeyInclusive, toKeyExclusive);
    }
}