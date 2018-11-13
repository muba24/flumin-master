using System.Collections.Generic;

namespace NodeSystemLib2.Generic.NodeAttributes {
    public interface IAttributable {
        void AddAttribute(NodeAttribute attr);
        IEnumerable<NodeAttribute> Attributes { get; }
        bool IsRunning { get; }
    }
}