namespace KeysAndValues.Tests;

public class BasicOperationTests
{
    [Fact]
    public void BasicSetTest()
    {
        var kvs = new KeyValueStore();
        kvs.Set("a", "1");
        kvs.Set("b", "2");
        kvs.Set("c", "3");
        kvs.Set("d", "4");
        var v = kvs.Snapshot();
        var s = v.Data;
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
        var kvs = new KeyValueStore(1, new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        Assert.Equal(4, kvs.Count);
        Assert.Equal("1", kvs.GetSring("a"));
        Assert.Equal("2", kvs.GetSring("b"));
        Assert.Equal("3", kvs.GetSring("c"));
        Assert.Equal("4", kvs.GetSring("d"));
        Assert.Equal(1, kvs.Sequence);
    }

    [Fact]
    public void BasicSetOverwriteTest()
    {
        var kvs = new KeyValueStore();
        kvs.Set("a", "1");
        kvs.Set("b", "2");
        kvs.Set("c", "3");
        kvs.Set("d", "4");
        kvs.Set("c", "5");
        kvs.Set("d", "6");
        var s = kvs.Data;
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
        var kvs = new KeyValueStore(1, new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        kvs.Delete("b");
        kvs.Delete("d");
        Assert.Equal(2, kvs.Count);
        Assert.Equal("1", kvs.GetSring("a"));
        Assert.Equal("3", kvs.GetSring("c"));
        Assert.Equal(3, kvs.Sequence);
    }

    [Fact]
    public void MultiDeleteTest()
    {
        var kvs = new KeyValueStore(1, new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        kvs.Delete("b");
        kvs.Delete("b");
        Assert.Equal(3, kvs.Count);
        Assert.Equal("1", kvs.GetSring("a"));
        Assert.Equal("3", kvs.GetSring("c"));
        Assert.Equal("4", kvs.GetSring("d"));
        Assert.Equal(3, kvs.Sequence);
    }

    [Fact]
    public void BasicEnumerationTest()
    {
        var kvs = new KeyValueStore(1, new Dictionary<Mem, Mem>
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
        var kvs = new KeyValueStore(1, new Dictionary<Mem, Mem>
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
        var kvs = new KeyValueStore(1, new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        var s = kvs.Data;
        kvs.Set("e", "5");
        kvs.Set("f", "6");
        Assert.Equal(4, s.Count);
        Assert.Equal(6, kvs.Count);
    }

    [Fact]
    public void DeleteSnapshotTest()
    {
        var kvs = new KeyValueStore(1, new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        var s = kvs.Data;
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
        var kvs = new KeyValueStore(1, new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        kvs.Delete(["a", "b"]);
        Assert.Equal(2, kvs.Count);
        Assert.Equal("3", kvs.GetSring("c"));
        Assert.Equal("4", kvs.GetSring("d"));
    }

    [Fact]
    public void GetNonexistantTest()
    {
        var kvs = new KeyValueStore(1, new Dictionary<Mem, Mem>
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
        var kvs = new KeyValueStore(1, new Dictionary<Mem, Mem>
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

    [Fact]
    public void SetMultipleWithOverlapTest()
    {
        var kvs = new KeyValueStore(1, new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        kvs.Set(new Dictionary<Mem, Mem>
        {
            ["c"] = "5",
            ["d"] = "6",
            ["e"] = "7",
            ["f"] = "8",
        });
        Assert.Equal(6, kvs.Count);
        Assert.Equal(2, kvs.Sequence);
        Assert.Equal(["a", "b", "c", "d", "e", "f"], [.. kvs.Keys().Select(a => (string)a)]);
        Assert.Equal(["1", "2", "5", "6", "7", "8"], [.. kvs.Values().Select(a => (string)a)]);
    }

    [Fact]
    public void SetMultipleWithOverlapAndRepeatsTest()
    {
        var kvs = new KeyValueStore(1, new Dictionary<Mem, Mem>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3",
            ["d"] = "4",
        });
        kvs.Set(
        [
            new("c", "5"),
            new("d", "6"),
            new("e", "7"),
            new("f", "8"),
            new("c", "9"),
            new("d", "0"),
        ]);
        Assert.Equal(6, kvs.Count);
        Assert.Equal(2, kvs.Sequence);
        Assert.Equal(["a", "b", "c", "d", "e", "f"], [.. kvs.Keys().Select(a => (string)a)]);
        Assert.Equal(["1", "2", "9", "0", "7", "8"], [.. kvs.Values().Select(a => (string)a)]);
    }
}
