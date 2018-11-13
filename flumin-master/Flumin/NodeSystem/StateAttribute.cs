using System;

namespace SimpleADC.NodeSystem {

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
