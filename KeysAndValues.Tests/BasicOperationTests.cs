namespace KeysAndValues.Tests;

public class BasicOperationTests
{
    [Fact]
    public void BasicSetTest()
    {
        var kvs = KeyValueStore.CreateEmpty();
        kvs.Set("a", "1");
        kvs.Set("b", "2");
        kvs.Set("c", "3");
        kvs.Set("d", "4");
        var s = kvs.Snapshot();
        Assert.Equal(4, s.Count);
        Assert.Equal("1", s["a"]);
        Assert.Equal("2", s["b"]);
        Assert.Equal("3", s["c"]);
        Assert.Equal("4", s["d"]);
        Assert.Equal(4, kvs.Sequence);
    }

    [Fact]
    public void BasicGetTest()
    {
        var kvs = KeyValueStore.CreateNewFrom(new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        Assert.Equal(4, kvs.Count);
        Assert.Equal("1", kvs.Get("a"));
        Assert.Equal("2", kvs.Get("b"));
        Assert.Equal("3", kvs.Get("c"));
        Assert.Equal("4", kvs.Get("d"));
        Assert.Equal(1, kvs.Sequence);
    }

    [Fact]
    public void BasicSetOverwriteTest()
    {
        var kvs = KeyValueStore.CreateEmpty();
        kvs.Set("a", "1");
        kvs.Set("b", "2");
        kvs.Set("c", "3");
        kvs.Set("d", "4");
        kvs.Set("c", "5");
        kvs.Set("d", "6");
        var s = kvs.Snapshot();
        Assert.Equal(4, s.Count);
        Assert.Equal("1", s["a"]);
        Assert.Equal("2", s["b"]);
        Assert.Equal("5", s["c"]);
        Assert.Equal("6", s["d"]);
        Assert.Equal(6, kvs.Sequence);
    }

    [Fact]
    public void BasicDeleteTest()
    {
        var kvs = KeyValueStore.CreateNewFrom(new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        kvs.Delete("b");
        kvs.Delete("d");
        Assert.Equal(2, kvs.Count);
        Assert.Equal("1", kvs.Get("a"));
        Assert.Equal("3", kvs.Get("c"));
        Assert.Equal(3, kvs.Sequence);
    }

    [Fact]
    public void MultiDeleteTest()
    {
        var kvs = KeyValueStore.CreateNewFrom(new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        kvs.Delete("b");
        kvs.Delete("b");
        Assert.Equal(3, kvs.Count);
        Assert.Equal("1", kvs.Get("a"));
        Assert.Equal("3", kvs.Get("c"));
        Assert.Equal("4", kvs.Get("d"));
        Assert.Equal(3, kvs.Sequence);
    }

    [Fact]
    public void BasicEnumerationTest()
    {
        var kvs = KeyValueStore.CreateNewFrom(new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        var s = kvs.Enumerate().ToDictionary();
        Assert.Equal("1", s["a"]);
        Assert.Equal("2", s["b"]);
        Assert.Equal("3", s["c"]);
        Assert.Equal("4", s["d"]);
    }

    [Fact]
    public void BasicRangeEnumerationTest()
    {
        var kvs = KeyValueStore.CreateNewFrom(new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
            ["e"] = "4",
            ["f"] = "4",
        });
        var s = kvs.Enumerate("b", "e").ToDictionary();
        Assert.Equal(3, s.Count);
        Assert.Equal("2", s["b"]);
        Assert.Equal("3", s["c"]);
        Assert.Equal("4", s["d"]);
    }

    [Fact]
    public void BasicSnapshotTest()
    {
        var kvs = KeyValueStore.CreateNewFrom(new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        var s = kvs.Snapshot();
        kvs.Set("e", "5");
        kvs.Set("f", "6");
        Assert.Equal(4, s.Count);
        Assert.Equal(6, kvs.Count);
    }

    [Fact]
    public void DeleteSnapshotTest()
    {
        var kvs = KeyValueStore.CreateNewFrom(new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        var s = kvs.Snapshot();
        kvs.Delete("b");
        kvs.Delete("c");
        Assert.Equal(4, s.Count);
        Assert.Equal(2, kvs.Count);
        Assert.Equal(["a", "d"], [.. kvs.Keys().Select(a => (string)a)]);
        Assert.Equal(["a", "b", "c", "d"], [.. s.Keys.Select(a => (string)a)]);
    }

    [Fact]
    public void DeleteRangeTest()
    {
        var kvs = KeyValueStore.CreateNewFrom(new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        kvs.Delete(["a", "b"]);
        Assert.Equal(2, kvs.Count);
        Assert.Equal("3", kvs.Get("c"));
        Assert.Equal("4", kvs.Get("d"));
    }

    [Fact]
    public void GetNonexistantTest()
    {
        var kvs = KeyValueStore.CreateNewFrom(new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        Assert.False(kvs.TryGet("e", out var _));
    }

    [Fact]
    public void SetMultipleTest()
    {
        var kvs = KeyValueStore.CreateNewFrom(new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        kvs.Set(new Dictionary<Mem, Mem>
        {
            ["e"] = "5",
            ["f"] = "6",
            ["g"] = "7",
            ["h"] = "8",
        });
        Assert.Equal(8, kvs.Count);
        Assert.Equal(2, kvs.Sequence);
        Assert.Equal(["a", "b", "c", "d", "e", "f", "g", "h"], [.. kvs.Keys().Select(a => (string)a)]);
        Assert.Equal(["1", "2", "3", "4", "5", "6", "7", "8"], [.. kvs.Values().Select(a => (string)a)]);
    }
}
