using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NodeSystemLib2.Generic {

    public abstract class StateNode<T> : Node where T : StateNode<T> {

        // if a comparer is given and the parent node is marked as unique through its
        // Metric attribute, check if the node is actually unique.
        public StateNode(string name, Graph g, Func<T, bool> comparer) : base(g) {
            Name = name;
            var typeInfo = typeof(T);
            var attrs = typeInfo.GetCustomAttributes(typeof(MetricAttribute), false);
            var attr = (MetricAttribute)attrs?.FirstOrDefault();
            if (attr != null && attr.OnlyOnePerGraph) {
                foreach (var node in g.Nodes.OfType<T>()) {
                    if (node != this && comparer(node)) {
                        //Dispose();
                        if (Parent.Nodes.Contains(this)) {
                            Parent.RemoveNode(this);
                        }
                        throw new PortAlreadyExistsException();
                    }
                }
            }
        }

        public StateNode(string name, Graph g) : base(g) {
            Name = name;
        }

        //public override NodeState SaveState() { return NodeState.Save((T)this, Parent.GetCurrentClockTime()); }
        //public override void LoadState(NodeState state) { state.Load(); }

    }

}
