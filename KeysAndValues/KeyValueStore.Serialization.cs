using System.Collections.Immutable;
using System.IO.Compression;
using System.Xml.Serialization;

namespace KeysAndValues;

public partial class KeyValueStore
{
    private const int MAGIC = 0x21241232;

    public static KeyValueStore Deserialize(Stream stream)
    {
        using var gz = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new BinaryReader(gz);
        if (reader.ReadInt32() != MAGIC)
        {
            throw new InvalidDataException("Invalid magic number");
        }

        var m = new KeyValueStore();
        var store = m.store.ToBuilder();
        m.sequence = reader.ReadInt64();
        var count = reader.ReadInt32();
        for (var i = 0; i < count; i++)
        {
            var keyLength = reader.ReadInt32();
            var key = m.pool.Allocate(keyLength, r => reader.Read(r));
            var valueLength = reader.ReadInt32();
            var value = m.pool.Allocate(valueLength, r => reader.Read(r));
            store.Add(key, value);
        }
        m.store = store.ToImmutable();
        return m;
    }

    public void Serialize(Stream stream)
    {
        using var gz = new GZipStream(stream, CompressionLevel.Optimal);
        using var writer = new BinaryWriter(gz);

        var (sequence, store) = Snapshot();

        writer.Write(MAGIC);
        writer.Write(sequence);
        writer.Write(store.Count);
        foreach (var kvp in store)
        {
            writer.Write(kvp.Key.Length);
            writer.Write(kvp.Key);
            writer.Write(kvp.Value.Length);
            writer.Write(kvp.Value);
        }
    }
}
