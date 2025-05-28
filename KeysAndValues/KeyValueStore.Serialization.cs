namespace KeysAndValues;

using KeysAndValues.Internal;
using System.Security.Cryptography;

public partial class KeyValueStore
{
    public void Serialize(Stream stream)
    {
        var snap = Snapshot(out var sequence);

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
}
