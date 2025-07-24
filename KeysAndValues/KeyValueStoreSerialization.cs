namespace KeysAndValues;

using System.IO;

// TODO: net standard 2.0
#if !NETSTANDARD2_0
using System.Security.Cryptography;

/// <summary>
/// Provides methods for serializing key/value stores.
/// </summary>
public static class KeyValueStoreSerialization
{
    /// <summary>
    /// Serialize a store version.
    /// </summary>
    /// <param name="version">The version to serialize.</param>
    /// <param name="stream">The stream to serialize to.</param>
    public static void SerializeStoreVersion(StoreVersion version, Stream stream)
    {
        var snap = version.Data;
        var sequence = version.Sequence;

        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> tmp = stackalloc byte[32];
        BitConverter.TryWriteBytes(tmp, sequence);
        BitConverter.TryWriteBytes(tmp[8..], snap.Count);
        stream.Write(tmp[..12]);
        sha.AppendData(tmp[..12]);

        foreach (var node in snap)
        {
            SerializeKvp(node, stream, sha);
        }

        sha.TryGetHashAndReset(tmp, out _);
        stream.Write(tmp);
    }

    /// <summary>
    /// Try to deserialize a store version from a stream.
    /// </summary>
    /// <param name="stream">The stream to deserialize from.</param>
    /// <param name="kvs">The version to deserialize.</param>
    /// <returns><c>true</c> if a store version could be deserialized.</returns>
    public static bool TryDeserializeStoreVersion(Stream stream, [MaybeNullWhen(false)] out StoreVersion kvs)
    {
        kvs = null;
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> tmp = stackalloc byte[32];
        if (!TryReadExactly(stream, tmp[..12]))
        {
            return false;
        }
        sha.AppendData(tmp[..12]);
        long sequence = BitConverter.ToInt64(tmp);
        int count = BitConverter.ToInt32(tmp[8..]);
        if (sequence < 0 || count < 0 ||
            !(count > 0 || sequence == 0))
        {
            return false;
        }

        var builder = ImmutableAvlTree<Mem, Mem>.Empty.ToBuilder();
        for (int i = 0; i < count; i++)
        {
            if (!TryDeserializeKvp(stream, sha, out var node))
            {
                return false;
            }
            builder.Add(node);
        }

        sha.TryGetHashAndReset(tmp, out _);
        Span<byte> tmp2 = stackalloc byte[32];
        if (!TryReadExactly(stream, tmp2))
        {
            return false;
        }
        if (!tmp.SequenceEqual(tmp2))
        {
            return false;
        }

        kvs = new(sequence, builder.ToImmutable());
        return true;
    }

    /// <summary>
    /// Serialize a set of change operations.
    /// </summary>
    /// <param name="changes">Changes to serialize.</param>
    /// <param name="stream">The stream to serialize to.</param>
    public static void SerializeChangeBatch(
        in ChangeBatch changes,
        Stream stream)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> tmp = stackalloc byte[32];

        BitConverter.TryWriteBytes(tmp[0..8], changes.Sequence);
        sha.AppendData(tmp[0..8]);
        stream.Write(tmp[0..8]);

        BitConverter.TryWriteBytes(tmp[0..4], changes.Operations.Length);
        sha.AppendData(tmp[0..4]);
        stream.Write(tmp[0..4]);

        foreach (var op in changes.Operations)
        {
            SerializeChangeOperation(op, stream, sha);
        }

        sha.TryGetHashAndReset(tmp, out _);
        stream.Write(tmp);
    }

    /// <summary>
    /// Try to read a set of change operations.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="changes">The deserialized change batch.</param>
    /// <returns><c>true</c> if the change batch could be deserialized.</returns>
    public static bool TryDeserializeChangeBatch(
        Stream stream,
        out ChangeBatch changes)
    {
        changes = default;

        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> tmp = stackalloc byte[32];

        // sequence
        if (!TryReadExactly(stream, tmp[0..8]))
        {
            return false;
        }
        var sequence = BitConverter.ToInt64(tmp);
        sha.AppendData(tmp[0..8]);

        // num ops
        if (!TryReadExactly(stream, tmp[0..4]))
        {
            return false;
        }
        var numOps = BitConverter.ToInt32(tmp);
        sha.AppendData(tmp[0..4]);

        if (numOps < 0)
        {
            return false;
        }

        var ops = new ChangeOperation[numOps];
        for (int i = 0; i < numOps; i++)
        {
            if (!TryDeserializeChangeOperation(stream, sha, out ops[i]))
            {
                return false;
            }
        }

        if (!TryReadExactly(stream, tmp))
        {
            return false;
        }
        Span<byte> cmp = stackalloc byte[32];
        sha.TryGetHashAndReset(cmp, out _);
        if (!cmp.SequenceEqual(tmp))
        {
            return false;
        }

        changes = new(sequence, ops);
        return true;
    }

    private static bool TryDeserializeChangeOperation(
        Stream stream,
        IncrementalHash? sha,
        out ChangeOperation op)
    {
        Span<byte> tmp = stackalloc byte[4];
        if (!TryReadExactly(stream, tmp))
        {
            op = default;
            return false;
        }
        sha?.AppendData(tmp);
        var type = (ChangeOperationType)BitConverter.ToInt32(tmp);

        if (!TryDeserializeKvp(stream, sha, out var kvp))
        {
            op = default;
            return false;
        }

        op = new()
        {
            Type = type,
            Key = kvp.Key,
            Value = kvp.Value,
        };

        return true;
    }

    private static void SerializeChangeOperation(
        ChangeOperation op,
        Stream stream,
        IncrementalHash? sha)
    {
        Span<byte> tmp = stackalloc byte[4];
        BitConverter.TryWriteBytes(tmp, (int)op.Type);
        stream.Write(tmp);
        sha?.AppendData(tmp);

        SerializeKvp(new(op.Key, op.Value), stream, sha);
    }

    private static void SerializeKvp(
        KeyValuePair<Mem, Mem> node,
        Stream stream,
        IncrementalHash? sha)
    {
        Span<byte> tmp = stackalloc byte[4];
        BitConverter.TryWriteBytes(tmp, node.Key.Length);
        stream.Write(tmp);
        sha?.AppendData(tmp);
        stream.Write(node.Key.Span);
        sha?.AppendData(node.Key.Span);
        BitConverter.TryWriteBytes(tmp, node.Value.Length);
        stream.Write(tmp);
        sha?.AppendData(tmp);
        stream.Write(node.Value.Span);
        sha?.AppendData(node.Value.Span);
    }

    private static bool TryDeserializeKvp(
        Stream stream,
        IncrementalHash? sha,
        out KeyValuePair<Mem, Mem> node)
    {
        Span<byte> tmp = stackalloc byte[4];
        node = default;
        if (!TryReadExactly(stream, tmp))
        {
            return false;
        }
        sha?.AppendData(tmp);
        int keyLength = BitConverter.ToInt32(tmp);
        var keyMem = new byte[keyLength];
        if (!TryReadExactly(stream, keyMem))
        {
            return false;
        }
        sha?.AppendData(keyMem);
        if (!TryReadExactly(stream, tmp))
        {
            return false;
        }
        sha?.AppendData(tmp);
        int valueLength = BitConverter.ToInt32(tmp);
        var valueMem = new byte[valueLength];
        if (!TryReadExactly(stream, valueMem))
        {
            return false;
        }
        sha?.AppendData(valueMem);
        node = new(new(keyMem), new(valueMem));
        return true;
    }

    private static bool TryReadExactly(Stream stream, Span<byte> buffer)
    {
        while (buffer.Length > 0)
        {
            int amt = stream.Read(buffer);
            if (amt == 0)
            {
                return false;
            }
            buffer = buffer[amt..];
        }

        return true;
    }
}

#endif