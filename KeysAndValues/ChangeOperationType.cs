using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeysAndValues
{
    public enum ChangeOperationType
    {
        /// <summary>
        /// There is no change. This is an invalid operation type.
        /// </summary>
        None,

        /// <summary>
        /// A set operation.
        /// </summary>
        Set,

        /// <summary>
        /// A delete operation.
        /// </summary>
        Delete
    }
}
