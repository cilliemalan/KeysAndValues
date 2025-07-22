namespace KeysAndValues;

using KeysAndValues.Internal;
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
            BitConverter.TryWriteBytes(tmp[0..4], node.Key.Length);
            BitConverter.TryWriteBytes(tmp[4..8], node.Value.Length);
            stream.Write(tmp[0..4]);
            sha.AppendData(tmp[0..4]);
            stream.Write(node.Key.Span);
            sha.AppendData(node.Key.Span);
            stream.Write(tmp[4..8]);
            sha.AppendData(tmp[4..8]);
            stream.Write(node.Value.Span);
            sha.AppendData(node.Value.Span);
        }

        sha.GetCurrentHash(tmp);
        stream.Write(tmp);
    }

    /// <summary>
    /// Deserialize a store version from a stream.
    /// </summary>
    /// <param name="stream">The stream to deserialize from</param>
    /// <returns>The new version object.</returns>
    /// <exception cref="InvalidDataException">Deserialization failed.</exception>
    public static StoreVersion DeserializeStoreVersion(Stream stream)
    {
        if (!TryDeserializeStoreVersion(stream, out var kvs))
        {
            throw new InvalidDataException("Invalid KeyValueStore serialization data.");
        }
        return kvs;
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
        if (!stream.TryReadExactly(tmp[..12]))
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
            if (!stream.TryReadExactly(tmp[..4]))
            {
                return false;
            }
            sha.AppendData(tmp[..4]);
            int keyLength = BitConverter.ToInt32(tmp);
            var keyMem = new byte[keyLength];
            if (!stream.TryReadExactly(keyMem))
            {
                return false;
            }
            sha.AppendData(keyMem);
            if (!stream.TryReadExactly(tmp[..4]))
            {
                return false;
            }
            sha.AppendData(tmp[..4]);
            int valueLength = BitConverter.ToInt32(tmp);
            var valueMem = new byte[valueLength];
            if (!stream.TryReadExactly(valueMem))
            {
                return false;
            }
            sha.AppendData(valueMem);
            builder.Add(new(keyMem), new(valueMem));
        }

        sha.GetCurrentHash(tmp);
        Span<byte> tmp2 = stackalloc byte[32];
        if (!stream.TryReadExactly(tmp2))
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
}
