namespace KeysAndValues;

using KeysAndValues.Internal;
using System.Security.Cryptography;

public partial class KeyValueStore
{
    public void Serialize(Stream stream)
    {
        var snap = Snapshot(out var sequence);

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
}
