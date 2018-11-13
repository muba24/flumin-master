using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic {
    /// <summary>
    /// Used to mark a private field in a class a state variable.
    /// When saving the state of a node, private fields with a State attribute
    /// will be saved. Otherwise they will be ignored.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class StateAttribute : Attribute {
        //
    }
}
