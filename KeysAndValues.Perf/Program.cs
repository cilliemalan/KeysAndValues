using KeysAndValues;
using System.Diagnostics;
using System.Diagnostics.Tracing;

var sw = new Stopwatch();
Console.Write("Generating corpus...");
sw.Restart();
var corpus = new List<KeyValuePair<Mem, Mem>>(1000000);
corpus.AddRange(KeysAndValues.Bench.Corpus.GenerateUnsorted(corpus.Capacity, 16, 32, 16, 256, 123123));
var corpus2 = new List<KeyValuePair<Mem, Mem>>(1000000);
corpus2.AddRange(KeysAndValues.Bench.Corpus.GenerateUnsorted(corpus.Capacity, 16, 32, 16, 256, 123123));
Console.WriteLine($" {sw.Elapsed.TotalMilliseconds:F0}ms ({sw.Elapsed.TotalSeconds / (corpus.Count * 2) * 1.0e9:F0}ns per item)");



Console.Write("Creating Tree");
sw.Restart();
ImmutableAvlTree<Mem, Mem> tree = ImmutableAvlTree.CreateRange(corpus);
Console.WriteLine($" {sw.Elapsed.TotalMilliseconds:F0}ms ({sw.Elapsed.TotalSeconds / (corpus.Count * 2) * 1.0e9:F0}ns per item)");



Console.Write("Enumerating Tree");
long dummy = 0;
sw.Restart();
for (int i = 0; i < 100; i++)
{
    using var enumerator = tree.GetEnumerator();
    while (enumerator.MoveNext())
    {
        dummy += enumerator.Current.Key.Length;
    }
}
Console.WriteLine($" {sw.Elapsed.TotalMilliseconds:F0}ms ({sw.Elapsed.TotalSeconds / (corpus.Count * 100.0) * 1.0e9:F0}ns per item)");



Console.Write("Doing lookups");
var r = new Random();
dummy = 0;
sw.Restart();
for (int i = 0; i < corpus.Count; i++)
{
    var key = corpus[r.Next(corpus.Count)].Key;
    dummy += tree[key].Length;
}
Console.WriteLine($" {sw.Elapsed.TotalMilliseconds:F0}ms ({sw.Elapsed.TotalSeconds / corpus.Count * 1.0e9:F0}ns per lookup)");



Console.WriteLine("Done");
