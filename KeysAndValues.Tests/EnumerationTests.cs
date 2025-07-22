namespace KeysAndValues.Tests;

public class EnumerationTests
{
    [Fact]
    public void BasicEnumerationTest()
    {
        var store = new KeyValueStore(1, new Dictionary<Mem,Mem>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
            ["key3"] = "value3",
            ["key4"] = "value4",
            ["key5"] = "value5"
        });
        var list = store.Enumerate().ToList();
        Assert.Equal(5, list.Count);
        Assert.Contains(new KeyValuePair<Mem, Mem>("key1", "value1"), list);
        Assert.Contains(new KeyValuePair<Mem, Mem>("key2", "value2"), list);
        Assert.Contains(new KeyValuePair<Mem, Mem>("key3", "value3"), list);
        Assert.Contains(new KeyValuePair<Mem, Mem>("key4", "value4"), list);
        Assert.Contains(new KeyValuePair<Mem, Mem>("key5", "value5"), list);
    }

    [Fact]
    public void RangeEnumerationTest()
    {
        var store = new KeyValueStore(1, new Dictionary<Mem, Mem>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
            ["key3"] = "value3",
            ["key4"] = "value4",
            ["key5"] = "value5"
        });
        var list = store.Enumerate("key2", "key5").ToList();
        Assert.Equal(3, list.Count);
        Assert.Contains(new KeyValuePair<Mem, Mem>("key2", "value2"), list);
        Assert.Contains(new KeyValuePair<Mem, Mem>("key3", "value3"), list);
        Assert.Contains(new KeyValuePair<Mem, Mem>("key4", "value4"), list);
    }

    [Fact]
    public void PrefixEnumerationTest()
    {
        var store = new KeyValueStore(1, new Dictionary<Mem, Mem>
        {
            ["alpha:1"] = "value1",
            ["beta:2"] = "value2",
            ["beta:3"] = "value3",
            ["beta:4"] = "value4",
            ["gamma:5"] = "value5"
        });
        var list = store.EnumeratePrefix("beta").ToList();
        Assert.Equal(3, list.Count);
        Assert.Contains(new KeyValuePair<Mem, Mem>("beta:2", "value2"), list);
        Assert.Contains(new KeyValuePair<Mem, Mem>("beta:3", "value3"), list);
        Assert.Contains(new KeyValuePair<Mem, Mem>("beta:4", "value4"), list);
    }

    [Fact]
    public void BasePrefixEnumerationTest()
    {
        var store = new KeyValueStore(1, new Dictionary<Mem, Mem>
        {
            ["alpha:1"] = "value1",
            ["bet`"] = "value",
            ["beta"] = "value",
            ["beta:2"] = "value2",
            ["beta:3"] = "value3",
            ["beta:4"] = "value4",
            ["betb"] = "valueb",
            ["gamma:5"] = "value5"
        });
        var list = store.EnumeratePrefix("beta").ToList();
        Assert.Equal(4, list.Count);
        Assert.Contains(new KeyValuePair<Mem, Mem>("beta", "value"), list);
        Assert.Contains(new KeyValuePair<Mem, Mem>("beta:2", "value2"), list);
        Assert.Contains(new KeyValuePair<Mem, Mem>("beta:3", "value3"), list);
        Assert.Contains(new KeyValuePair<Mem, Mem>("beta:4", "value4"), list);
    }

    [Fact]
    public void AdjacentPrefixEnumerationTest()
    {
        var store = new KeyValueStore(1, new Dictionary<Mem, Mem>
        {
            ["alpha:1"] = "value1",
            ["beta:2"] = "value2",
            ["beta:3"] = "value3",
            ["beta:4"] = "value4",
            ["beta0"] = "value0",
            ["gamma:5"] = "value5"
        });
        var list = store.EnumeratePrefix("beta").ToList();
        Assert.Equal(4, list.Count);
        Assert.Contains(new KeyValuePair<Mem, Mem>("beta:2", "value2"), list);
        Assert.Contains(new KeyValuePair<Mem, Mem>("beta:3", "value3"), list);
        Assert.Contains(new KeyValuePair<Mem, Mem>("beta:4", "value4"), list);
        Assert.Contains(new KeyValuePair<Mem, Mem>("beta0", "value0"), list);
    }
}
