using System.Buffers;
using System.Text;

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

    public static long Set(this KeyValueStore store, IEnumerable<KeyValuePair<string, string>> items)
    {
        if (items is not ICollection<KeyValuePair<string, string>> collection)
        {
            return Set(store, items.ToList());
        }

        int dataLen = 0;
        foreach (var item in collection)
        {
            var kl = Encoding.UTF8.GetByteCount(item.Key);
            var vl = Encoding.UTF8.GetByteCount(item.Value);
            dataLen += kl + vl;
        }

        var mem = ArrayPool<byte>.Shared.Rent(dataLen);
        var ops = ArrayPool<ChangeOperation>.Shared.Rent(collection.Count);
        try
        {
            var mspan = mem.AsSpan();

            var index = 0;
            var dindex = 0;
            foreach (var item in collection)
            {
                var kl = Encoding.UTF8.GetByteCount(item.Key);
                var vl = Encoding.UTF8.GetByteCount(item.Value);
                var ks = dindex;
                dindex += kl;
                var vs = dindex;
                dindex += vl;
                var mkey = mspan.Slice(ks, kl);
                var mval = mspan.Slice(vs, vl);
                Encoding.UTF8.GetBytes(item.Key, mkey);
                Encoding.UTF8.GetBytes(item.Value, mval);
                ops[index++] = new()
                {
                    Type = ChangeOperationType.Set,
                    Key = new(mem, ks, kl),
                    Value = new(mem, vs, vl)
                };
            }

            return store.Apply(ops);
        }
        finally
        {
            ArrayPool<ChangeOperation>.Shared.Return(ops);
            ArrayPool<byte>.Shared.Return(mem);
        }
    }

    public static long Delete(this KeyValueStore store, IEnumerable<string> keys)
    {
        if (keys is not ICollection<string> ckeys)
        {
            return Delete(store, keys.ToList());
        }

        int dataLen = 0;
        foreach (var key in ckeys)
        {
            dataLen += Encoding.UTF8.GetByteCount(key);
        }

        var mem = ArrayPool<byte>.Shared.Rent(dataLen);
        var ops = ArrayPool<ChangeOperation>.Shared.Rent(ckeys.Count);
        try
        {
            var mspan = mem.AsSpan();

            var index = 0;
            var dindex = 0;
            foreach (var key in ckeys)
            {
                var kl = Encoding.UTF8.GetByteCount(key);
                var ks = dindex;
                dindex += kl;
                var mkey = mspan.Slice(ks, kl);
                Encoding.UTF8.GetBytes(key, mkey);
                ops[index++] = new()
                {
                    Type = ChangeOperationType.Delete,
                    Key = new(mem, ks, kl),
                };
            }

            return store.Apply(ops);
        }
        finally
        {
            ArrayPool<ChangeOperation>.Shared.Return(ops);
            ArrayPool<byte>.Shared.Return(mem);
        }
    }

    public static long Delete(this KeyValueStore store, string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length == 0)
        {
            throw new ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));
        }

        Span<byte> mkey;
        var kl = Encoding.UTF8.GetByteCount(key);
        using var m = MemoryPool<byte>.Shared.Rent(kl);
        mkey = m.Memory.Span[..kl];
        Encoding.UTF8.GetBytes(key, mkey);
        return store.Delete(mkey);
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

    public static long Set(this KeyValueStore store, IEnumerable<KeyValuePair<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>> entries)
    {
        var len = (entries as System.Collections.ICollection)?.Count ?? -1;
        if (len == 0)
        {
            return store.Apply([]);
        }

        if (len < 0)
        {
            return Set(store, entries.ToList());
        }

        var ops = ArrayPool<ChangeOperation>.Shared.Rent(len);
        try
        {
            var index = 0;
            foreach (var entry in entries)
            {
                ops[index++] = new()
                {
                    Type = ChangeOperationType.Set,
                    Key = entry.Key,
                    Value = entry.Value
                };
            }

            return store.Apply(ops);
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

    public static IEnumerable<KeyValuePair<Mem, Mem>> Enumerate(this KeyValueStore store) => store.Snapshot();
    public static IEnumerable<Mem> Keys(this KeyValueStore store) => store.Snapshot().Keys;
    public static IEnumerable<Mem> Values(this KeyValueStore store) => store.Snapshot().Values;

    public static IEnumerable<KeyValuePair<Mem, Mem>> Enumerate(this KeyValueStore store, Mem fromKeyInclusive, Mem toKeyExclusive)
    {
        var k = store.Snapshot();
        if (toKeyExclusive.IsEmpty)
        {
            // the "default" Mem will always sort before the first entry in the database
            return k.Range(fromKeyInclusive);
        }

        return k.Range(fromKeyInclusive, toKeyExclusive);
    }
}