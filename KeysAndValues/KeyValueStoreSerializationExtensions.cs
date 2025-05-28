using KeysAndValues.Internal;
using System.Security.Cryptography;

namespace KeysAndValues;

public static class KeyValueStoreSerializationExtensions
{
    public static void Serialize(this KeyValueStore store, Stream stream)
    {
        var snap = store.Snapshot(out var sequence);

        using var sha = new Sha256Hasher();
        Span<byte> tmp = stackalloc byte[32];
        BitConverter.TryWriteBytes(tmp, sequence);
        BitConverter.TryWriteBytes(tmp[8..], snap.Count);
        stream.Write(tmp[..16]);
        sha.Ingest(tmp[..16]);

        foreach (var node in snap)
        {
            BitConverter.TryWriteBytes(tmp[0..4], node.Key.Length);
            BitConverter.TryWriteBytes(tmp[4..8], node.Value.Length);
            stream.Write(tmp[0..4]);
            sha.Ingest(tmp[0..4]);
            stream.Write(node.Key.Span);
            sha.Ingest(node.Key.Span);
            stream.Write(tmp[4..8]);
            sha.Ingest(tmp[4..8]);
            stream.Write(node.Value.Span);
            sha.Ingest(node.Value.Span);
        }

        sha.Compute(tmp);
        stream.Write(tmp);
    }

    private static void IngestInternal(SHA256 sha, Mem key)
    {
        if (key.TryGetArray(out var seg))
        {
            sha.TransformBlock(seg.Array!, seg.Offset, seg.Count, seg.Array, seg.Offset);
            return;
        }

        byte[] tmp = ArrayPool<byte>.Shared.Rent(64);
        ReadOnlySpan<byte> src = key.Span;
        while (src.Length > 0)
        {
            int toCopy = Math.Min(src.Length, tmp.Length);
            src[..toCopy].CopyTo(tmp);
            sha.TransformBlock(tmp, 0, toCopy, tmp, 0);
            src = src[toCopy..];
        }
        ArrayPool<byte>.Shared.Return(tmp);
    }
}
