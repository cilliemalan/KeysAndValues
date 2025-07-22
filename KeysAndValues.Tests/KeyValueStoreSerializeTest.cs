namespace KeysAndValues.Tests;

public class KeyValueStoreSerializeTest
{
    [Fact]
    public void BasicSerializeTest()
    {
        var kvs = new KeyValueStore(1, Corpus.GenerateUnsorted(10));
        using var ms = new MemoryStream();
        KeyValueStoreSerialization.SerializeStoreVersion(kvs.Snapshot(), ms);
        Assert.True(ms.Length > 0);
    }

    [Fact]
    public void BasicDeserializeTest()
    {
        var kvs = new KeyValueStore(1, Corpus.GenerateUnsorted(10));
        using var ms = new MemoryStream();
        KeyValueStoreSerialization.SerializeStoreVersion(kvs.Snapshot(), ms);
        ms.Position = 0;
        var ver2 = KeyValueStoreSerialization.DeserializeStoreVersion(ms);
        Assert.Equal(kvs.Count, ver2.Data.Count);
        Assert.Equal(kvs.Sequence, ver2.Sequence);
        Assert.Equal(kvs.Data.AsEnumerable(), ver2.Data.AsEnumerable());
    }
}
