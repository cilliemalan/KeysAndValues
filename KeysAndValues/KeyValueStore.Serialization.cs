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

        int numStores = reader.ReadInt32();
        var m = new KeyValueStore();
        for (int i = 0; i < numStores; i++)
        {
            var storeb = ImmutableSortedDictionary.CreateBuilder<Mem, Mem>();
            long seq = reader.ReadInt64();
            int numEntries = reader.ReadInt32();
            for (int j = 0; j < numEntries; j++)
            {
                var klen = reader.ReadInt32();
                var key = m.pool.Allocate(klen, b => reader.Read(b));
                var vlen = reader.ReadInt32();
                var value = m.pool.Allocate(vlen, b => reader.Read(b));
                storeb.Add(key, value);
            }
            m.pastStores.Add((seq, storeb.ToImmutable()));
        }
        
        (m.sequence, m.store) = m.pastStores[^1];
        return m;
    }

    public void Serialize(Stream stream, SerializationMode mode = SerializationMode.Shallow)
    {
        int startnr = mode == SerializationMode.Shallow ? 0 : pastStores.Count - 1;
        int endnr = pastStores.Count;

        using var gz = new GZipStream(stream, CompressionLevel.Optimal);
        using var writer = new BinaryWriter(gz);

        writer.Write(MAGIC);
        writer.Write(endnr - startnr);
        for (int i = startnr; i < endnr; i++)
        {
            writer.Write(pastStores[i].sequence);
            writer.Write(pastStores[i].store.Count);
            foreach (var kvp in pastStores[i].store)
            {
                writer.Write(kvp.Key.Length);
                writer.Write(kvp.Key);
                writer.Write(kvp.Value.Length);
                writer.Write(kvp.Value);
            }
        }
    }
}
