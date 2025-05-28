using KeysAndValues.Internal;
using System.Security.Cryptography;

namespace KeysAndValues;

public static class KeyValueStoreSerializationExtensions
{
    public static void Serialize(this KeyValueStore store, Stream stream)
    {
        var snap = store.Snapshot(out var sequence);

        using var sha = SHA256.Create();
        byte[] tmpa = ArrayPool<byte>.Shared.Rent(32);
        Span<byte> tmp = tmpa.AsSpan(0..32);
        BitConverter.TryWriteBytes(tmp, sequence);
        BitConverter.TryWriteBytes(tmp[8..], snap.Count);
        stream.Write(tmp[..16]);
        sha.TransformBlock(tmpa, 0, 16, null, 0);

        foreach (var node in snap)
        {
            BitConverter.TryWriteBytes(tmp[0..4], node.Key.Length);
            BitConverter.TryWriteBytes(tmp[4..8], node.Value.Length);
            stream.Write(tmp[0..4]);
            sha.TransformBlock(tmpa, 0, 4, null, 0);
            stream.Write(node.Key.Span);
            IngestInternal(sha, node.Key);
            stream.Write(tmp[4..8]);
            sha.TransformBlock(tmpa, 4, 4, null, 0);
            stream.Write(node.Value.Span);
            IngestInternal(sha, node.Value);
        }

        sha.TransformBlock(tmpa, 0, 0, tmpa, 0);
        stream.Write(tmp);
    }

    private static void IngestInternal(SHA256 sha, Mem key)
    {
        throw new NotImplementedException();
    }
}
