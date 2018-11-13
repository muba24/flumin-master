using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic.NodeAttributes {

    public class AttributeChangedEventArgs : EventArgs {
        public NodeAttribute Attribute { get; }
        public AttributeChangedEventArgs(NodeAttribute attr) {
            Attribute = attr;
        }
    }

}
