using KeysAndValues.Internal;

namespace KeysAndValues;

public static class KeyValueStoreSerializationExtensions
{
    public static void Serialize(this KeyValueStore store, Stream stream)
    {
        var snap = store.Snapshot(out var sequence);

        Sha256 hash = new(stackalloc uint[24]);
        Span<byte> tmp = stackalloc byte[32];
        BitConverter.TryWriteBytes(tmp, sequence);
        BitConverter.TryWriteBytes(tmp[8..], snap.Count);
        stream.Write(tmp[..16]);
        hash.Ingest(tmp[..16]);

        foreach (var node in snap)
        {
            BitConverter.TryWriteBytes(tmp[0..4], node.Key.Length);
            BitConverter.TryWriteBytes(tmp[4..8], node.Value.Length);
            stream.Write(tmp[0..4]);
            hash.Ingest(tmp[0..4]);
            stream.Write(node.Key.Span);
            hash.Ingest(node.Key.Span);
            stream.Write(tmp[4..8]);
            hash.Ingest(tmp[4..8]);
            stream.Write(node.Value.Span);
            hash.Ingest(node.Value.Span);
        }

        hash.ComputeHash(tmp);
        stream.Write(tmp);
    }
}
