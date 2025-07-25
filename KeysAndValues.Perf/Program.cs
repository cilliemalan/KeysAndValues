using KeysAndValues;
using System.Diagnostics;

var sw = new Stopwatch();
var r = new Random();



Console.Write("Generating corpus...");
sw.Restart();
var corpus = new List<KeyValuePair<Mem, Mem>>(100000);
corpus.AddRange(Corpus.GenerateUnsorted(corpus.Capacity, 16, 32, 16, 256, 123123));
var corpus2 = new List<KeyValuePair<Mem, Mem>>(100000);
corpus2.AddRange(Corpus.GenerateUnsorted(corpus.Capacity, 16, 32, 16, 256, 123124));
Console.WriteLine($" {sw.Elapsed.TotalMilliseconds:F0}ms ({sw.Elapsed.TotalSeconds / (corpus.Count * 2) * 1.0e9:F0}ns per item)");



Console.Write("Creating Tree");
sw.Restart();
ImmutableAvlTree<Mem, Mem> tree = ImmutableAvlTree.CreateRange(corpus);
Console.WriteLine($" {sw.Elapsed.TotalMilliseconds:F0}ms ({sw.Elapsed.TotalSeconds / (corpus.Count * 2) * 1.0e9:F0}ns per item)");



Console.Write("Enumerating Tree");
long dummy = 0;
sw.Restart();
for (int i = 0; i < 10; i++)
{
    using var enumerator = tree.GetEnumerator();
    while (enumerator.MoveNext())
    {
        dummy += enumerator.Current.Key.Length;
    }
}
Console.WriteLine($" {sw.Elapsed.TotalMilliseconds:F0}ms ({sw.Elapsed.TotalSeconds / (corpus.Count * 10.0) * 1.0e9:F0}ns per item)");



Console.Write("Doing lookups");
dummy = 0;
sw.Restart();
for (int i = 0; i < corpus.Count; i++)
{
    var item = corpus[r.Next(corpus.Count)];
    tree = tree.SetItem(item.Key, item.Value);
}
Console.WriteLine($" {sw.Elapsed.TotalMilliseconds:F0}ms ({sw.Elapsed.TotalSeconds / corpus.Count * 1.0e9:F0}ns per lookup)");



Console.Write("Generating changes");
int nr2 = 0;
var keys = tree.Keys.ToList();
var changes = new List<ChangeOperation<Mem, Mem>>(100000);
sw.Restart();
for (int i = 0; i < 100000; i++)
{
    bool isAdd = r.Next(10) < 2;
    if (isAdd)
    {
        var kvp = corpus2[nr2++];
        changes.Add(ChangeOperation.Add(kvp.Key, kvp.Value));
    }

    bool isDelete = r.Next(10) < 3;
    if (isDelete)
    {
        var ixToDelete = r.Next(keys.Count);
        var keyToDelete = keys[ixToDelete];
        changes.Add(ChangeOperation.Delete<Mem, Mem>(keyToDelete));
        keys.RemoveAt(ixToDelete);
        continue;
    }

    var ixToChange = r.Next(keys.Count);
    var keyToChange = keys[ixToChange];
    changes.Add(ChangeOperation.Set(keyToChange, corpus2[nr2++].Value));
}
Console.WriteLine($" {sw.Elapsed.TotalMilliseconds:F0}ms ({sw.Elapsed.TotalSeconds / changes.Count * 1.0e9:F0}ns per generation)");



Console.Write("Applying changes");
var trees = new List<ImmutableAvlTree<Mem, Mem>>(100000);
sw.Restart();
for (int i = 0; i < changes.Count; i++)
{
    trees.Add(tree.Apply([changes[i]]));
}
Console.WriteLine($" {sw.Elapsed.TotalMilliseconds:F0}ms ({sw.Elapsed.TotalSeconds / changes.Count * 1.0e9:F0}ns per change)");



Console.Write("Calculating diffs");
var diffs = new List<ChangeOperation<Mem, Mem>>(100000);
sw.Restart();
for (int i = 1; i < trees.Count; i++)
{
    var ops = DifferenceCalculation.CalculateDifference(trees[i - 1], trees[i]);
    diffs.AddRange(ops);
}
Console.WriteLine($" {sw.Elapsed.TotalMilliseconds:F0}ms ({sw.Elapsed.TotalSeconds / changes.Count * 1.0e9:F0}ns per diff)");


Console.WriteLine("Done");
