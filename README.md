# KeysAndValues
A simple in-memory sorted key-value store.

# Example usage:

```csharp
using KeysAndValues;
using System.Diagnostics;

// the store
KeyValueStore? kvs = null;

// try to load
var dbpath = Path.Combine(Path.GetTempPath(), "keys-and-values.dat");
if (File.Exists(dbpath))
{
    using var fs = File.OpenRead(dbpath);
    if (KeyValueStoreSerialization.TryDeserializeStore(fs, out kvs))
    {
        Console.WriteLine($"Opened existing store " +
            $"with {kvs.Count} items " +
            $"at sequence {kvs.Sequence}");
    }
    else
    {
        Console.WriteLine("The store could not be read!");
    }
}

// if we could not load, create new, empty
kvs ??= new();

// set a key
kvs.Set($"key-{Guid.NewGuid()}", $"Now is: {DateTime.Now}");

// keys and values are of type Mem, which
// wraps an immutable block of memory. It has
// many implicit casts. Strings are encoded
// via UTF-8.
Mem memKey = $"key-{Guid.NewGuid()}";
Mem memValue = new byte[] { 1, 2, 3 };
kvs.Set(memKey, memValue);

// Many keys may be set at once.
// This is an atomic change to the store.
var changes = Enumerable.Range(0, 10)
    .Select(i => new KeyValuePair<Mem, Mem>($"numbers-{Guid.NewGuid()}", $"Item {i}"));
kvs.Set(changes);

// items can be empty
kvs.Set("emptyitem", default);
Debug.Assert(kvs.ContainsKey("emptyitem"));

// items may be removed
Console.WriteLine($"Count before delete: {kvs.Count} at seq {kvs.Sequence}");
kvs.Delete(["somekey", "emptyitem"]);
Console.WriteLine($"Count after delete: {kvs.Count} at seq {kvs.Sequence}");

// multiple items may be removed
Console.WriteLine($"Count before second delete: {kvs.Count} at seq {kvs.Sequence}");
kvs.Delete(kvs.EnumeratePrefix("numbers-").Select(x => x.Key));
Console.WriteLine($"Count after second delete: {kvs.Count} at seq {kvs.Sequence}");

// As seen above, a key feature is range enumeration.
// Enumeration is thread safe (as all operations are).
// The store may be changed while enumeration is in progress
// and the enumeration will not "see" any changes after the
// call to Enumerate.
kvs.Set("item-1", "1");
kvs.Set("item-2", "2");
kvs.Set("item-3", "3");
kvs.Set("item-4", "4");
var itemsInRange = kvs.Enumerate(
    fromKeyInclusive: "item-1",
    toKeyExclusive: "item-3");
Debug.Assert(itemsInRange.Count() == 2);

// Each time the store changes, the sequence number is updated.
// Setting or deleting multiple items is one sequence number increment.
Console.WriteLine($"The store sequence number is now {kvs.Sequence}");
Console.WriteLine($"The store contains {kvs.Count} items");

// and then a store may be serialized

var tmpdbpath = Path.GetTempFileName();
using (var wfs = File.Create(tmpdbpath))
{
    KeyValueStoreSerialization.SerializeStore(kvs, wfs);
}
// copy over the old db file
File.Copy(tmpdbpath, dbpath, overwrite: true);
File.Delete(tmpdbpath);

// check some stats
var (keyBytes, valueBytes) = kvs.Enumerate().Aggregate(
    (keyBytes: 0, valueBytes: 0),
    (b, x) => (b.keyBytes + x.Key.Length, b.valueBytes + x.Value.Length));
Console.WriteLine($"The store contains {keyBytes} bytes in keys");
Console.WriteLine($"The store contains {valueBytes} bytes in values");
Console.WriteLine($"Serialized the store to a {new FileInfo(dbpath).Length} byte file");
```