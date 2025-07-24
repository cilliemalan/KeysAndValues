namespace KeysAndValues.Tests;

public class KeyValueStoreSerializeTest
{
    [Fact]
    public void VersionSerializeDeserializeTest()
    {
        var kvs = new KeyValueStore(1, Corpus.GenerateUnsorted(10));
        using var ms = new MemoryStream();
        KeyValueStoreSerialization.SerializeStoreVersion(kvs.Snapshot(), ms);
        ms.Position = 0;
        Assert.True(KeyValueStoreSerialization.TryDeserializeStoreVersion(ms, out var ver2));
        Assert.Equal(kvs.Count, ver2.Data.Count);
        Assert.Equal(kvs.Sequence, ver2.Sequence);
        Assert.Equal(kvs.Data.AsEnumerable(), ver2.Data.AsEnumerable());
    }

    [Fact]
    public void ChangeBatchSerializeDeserializeTest()
    {
        using var ms = new MemoryStream();
        var kvps = Corpus.GenerateUnsorted(10);
        ChangeBatch cb = new(123412341234,
            kvps.Select(kvp =>
            {
                var type = kvp.Key.Span[0] % 2 == 0 ? ChangeOperationType.Set : ChangeOperationType.Delete;
                return new ChangeOperation
                {
                    Type = type,
                    Key = kvp.Key,
                    Value = type == ChangeOperationType.Delete ? default : kvp.Value
                };
            }).ToArray());

        KeyValueStoreSerialization.SerializeChangeBatch(cb, ms);
        ms.Position = 0;
        Assert.True(KeyValueStoreSerialization.TryDeserializeChangeBatch(ms, out var cb2));
        Assert.Equal(cb.Sequence, cb2.Sequence);
        Assert.Equal(cb.Operations.Length, cb2.Operations.Length);
        for (int i = 0; i < cb.Operations.Length; i++)
        {
            var op = cb.Operations[i];
            var op2 = cb2.Operations[i];
            Assert.Equal(op.Type, op2.Type);
            Assert.Equal(op.Key, op2.Key);
            Assert.Equal(op.Value, op2.Value);
        }
    }
}
