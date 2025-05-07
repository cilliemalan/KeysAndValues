using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeysAndValues
{
    public readonly struct ChangeOperation
    {
        public ChangeOperationType Type { get; init; }
        public ReadOnlyMemory<byte> Key { get; init; }
        public ReadOnlyMemory<byte> Value { get; init; }
    }
}
