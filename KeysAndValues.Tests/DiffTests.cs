using System.Security.Claims;

namespace KeysAndValues.Tests;

public class DiffTests
{
    [Fact]
    public void BasicDiffTest()
    {
        var d1 = ImmutableAvlTree<Mem, Mem>.Empty.AddRange([
            new("key1", "value1"),
            new("key2", "value2"),
            new("key3", "value3"),
            new("key4", "value4"),
            new("key5", "value5"),
        ]);

        var d2 = ImmutableAvlTree<Mem, Mem>.Empty.AddRange([
            new("key1", "VALUE1"),
            new("key2", "value2"),
            new("key3", "value3"),
            new("key5", "value5"),
            new("key6", "value6"),
        ]);

        ChangeOperation<Mem, Mem>[] diff = DifferenceCalculation.CalculateDifference(d1, d2);
        Assert.Equal(3, diff.Length);
        Assert.Contains(ChangeOperation.Set<Mem, Mem>("key1", "VALUE1"), diff);
        Assert.Contains(ChangeOperation.Add<Mem, Mem>("key6", "value6"), diff);
        Assert.Contains(ChangeOperation.Delete<Mem, Mem>("key4"), diff);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(12)]
    [InlineData(15)]
    [InlineData(20)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10, 10)]
    [InlineData(12, 10)]
    [InlineData(15, 20)]
    [InlineData(20, 20)]
    [InlineData(100, 100)]
    [InlineData(1000, 1000)]
    public void ShuffleDiffTest(int count, int numChanges = 0)
    {
        var r = new Random(count * 17);

        var baseTree = ImmutableAvlTree<Mem, Mem>.Empty.AddRange(
            Enumerable.Range(0, count)
                .Select(i => new KeyValuePair<Mem, Mem>($"key{i}", Guid.NewGuid().ToString("N"))));

        var da = baseTree;
        while (numChanges == 0)
        {
            numChanges = r.Next(count + 1);
        }

        var db = ChangeAround(baseTree, r, numChanges);

        var diffa2b = DifferenceCalculation.CalculateDifference(da, db);
        CheckDiff(da, db, diffa2b);
        CheckApplication(da, db, diffa2b);

        var diffb2a = DifferenceCalculation.CalculateDifference(db, da);
        CheckDiff(db, da, diffb2a);
        CheckApplication(db, da, diffb2a);

        Assert.Equal(diffa2b.Length, diffb2a.Length);
    }

    private static void CheckDiff(ImmutableAvlTree<Mem, Mem> d1, ImmutableAvlTree<Mem, Mem> d2, ReadOnlySpan<ChangeOperation<Mem, Mem>> diff)
    {
        foreach (var change in diff)
        {
            switch (change.Type)
            {
                case ChangeOperationType.Add:
                    Assert.False(d1.ContainsKey(change.Key));
                    Assert.True(d2.TryGetValue(change.Key, out var addedVal));
                    Assert.Equal(change.Value, addedVal);
                    break;
                case ChangeOperationType.Set:
                    Assert.True(d1.TryGetValue(change.Key, out var oldVal));
                    Assert.True(d2.TryGetValue(change.Key, out var newVal));
                    Assert.NotEqual(change.Value, oldVal);
                    Assert.Equal(change.Value, newVal);
                    break;
                case ChangeOperationType.Delete:
                    Assert.True(d1.ContainsKey(change.Key));
                    Assert.False(d2.ContainsKey(change.Key));
                    break;
            }
        }
    }

    private static void CheckApplication(ImmutableAvlTree<Mem, Mem> d1, ImmutableAvlTree<Mem, Mem> d2, ReadOnlySpan<ChangeOperation<Mem, Mem>> diff)
    {
        var cd2 = d1.Apply(diff);
        Assert.Equal(d2.Count, cd2.Count);
        var kvps = d2.ToList();
        var kvpc = cd2.ToList();
        for (int i = 0; i < kvps.Count; i++)
        {
            Assert.Equal(kvps[i], kvpc[i]);
        }
    }

    private static ImmutableAvlTree<Mem, Mem> ChangeAround(
        ImmutableAvlTree<Mem, Mem> d1,
        Random r,
        int numChanges)
    {
        var keys = d1.Keys.ToList();
        int nr = d1.Count + 1;

        var b = d1.ToBuilder();
        for (int i = 0; i < numChanges; i++)
        {
            bool isAdd = r.Next(10) < 2;
            if (isAdd)
            {
                var nkey = $"key{++nr}";
                var nval = Guid.NewGuid().ToString("N");
                b.Add(nkey, nval);
            }

            bool isDelete = r.Next(10) < 3;
            if (isDelete)
            {
                var ixToDelete = r.Next(keys.Count);
                var keyToDelete = keys[ixToDelete];
                b.Remove(keyToDelete);
                keys.RemoveAt(ixToDelete);
                continue;
            }

            var ixToChange = r.Next(keys.Count);
            var keyToChange = keys[ixToChange];
            b[keyToChange] = Guid.NewGuid().ToString("N");
        }

        return b.ToImmutable();
    }
}
