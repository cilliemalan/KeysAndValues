using System.Collections;

namespace KeysAndValues;

public partial class KeyValueStore
{
    public IEnumerator<KeyValuePair<Mem, Mem>> GetEnumerator() => store.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => store.GetEnumerator();
}
