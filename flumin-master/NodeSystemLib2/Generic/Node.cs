using NodeSystemLib2.Generic;
using NodeSystemLib2.Generic.NodeAttributes;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NodeSystemLib2 {

    public abstract class Node : IDisposable, IAttributable {

        public class PortChangedEventArgs : EventArgs {
            public enum Modifier {
                Added, Removed
            }

            public int Index { get; }
            public Port Port { get; }
            public Modifier Action { get; }

            public PortChangedEventArgs(Port p, int index, Modifier action) {
                Port = p;
                Action = action;
                Index = index;
            }
        }

        public class PortConnectionChangedEventArgs : EventArgs {
            public enum Modifier {
                Added, Removed, Changed
            }

            public Port Port { get; }
            public Modifier Action { get; }
            public Port Connection { get; }

            public PortConnectionChangedEventArgs(Port p, Port to, Modifier action) {
                Port = p;
                Action = action;
                Connection = to;
            }
        }

        private readonly List<OutputPort> _outputs = new List<OutputPort>();
        private readonly List<InputPort> _inputs = new List<InputPort>();
        private readonly Dictionary<string, NodeAttribute> _attributes 
            = new Dictionary<string, NodeAttribute>(StringComparer.InvariantCultureIgnoreCase);

        private readonly AttributeValueString _description;
        private readonly AttributeValueString _name;

        public IReadOnlyList<OutputPort> OutputPorts => _outputs;
        public IReadOnlyList<InputPort> InputPorts => _inputs;
        public IEnumerable<NodeAttribute> Attributes => _attributes.Values;

        public Graph Parent { get; }
        public Graph.State State => Parent.GraphState;

        public string Description {
            get { return _description.TypedGet(); }
            set { _description.Set(value); }
        }

        public string Name {
            get { return _name.TypedGet(); }
            protected set { _name.Set(value); }
        }

        public abstract void PrepareProcessing();
        public abstract void StartProcessing();
        public abstract void StopProcessing();
        public abstract void SuspendProcessing();
        public abstract void Process();
        public abstract void Transfer();
        public abstract bool CanProcess { get; }
        public abstract bool CanTransfer { get; }

        bool IAttributable.IsRunning => State != Graph.State.Stopped;

        public virtual FlushState FlushData() { return FlushState.Empty; }

        public event EventHandler<PortChangedEventArgs> PortAdded;
        public event EventHandler<PortChangedEventArgs> PortRemoved;
        public event EventHandler<ConnectionModifiedEventArgs> PortConnectionChanged;

        public enum FlushState {
            Empty, Some
        }

        protected Node(Graph parent) {
            Parent = parent;
            Parent.AddNode(this);

            _description = new AttributeValueString(this, "Description");
            _name = new AttributeValueString(this, "Name");
            _name.SetReadOnly();
        }

        public NodeAttribute GetAttribute(string name) {
            if (name == null) throw new ArgumentNullException();
            return _attributes[name];
        }

        public void AddAttribute(NodeAttribute attr) {
            if (attr == null) throw new ArgumentNullException();
            _attributes.Add(attr.Name, attr);
        }

        public void RemovePort(InputPort port) {
            var index = _inputs.IndexOf(port);
            _inputs.Remove(port);
            PortRemoved?.Invoke(this, new PortChangedEventArgs(port, index, PortChangedEventArgs.Modifier.Removed));
        }

        public void RemovePort(OutputPort port) {
            var index = _outputs.IndexOf(port);
            _outputs.Remove(port);
            PortRemoved?.Invoke(this, new PortChangedEventArgs(port, index, PortChangedEventArgs.Modifier.Removed));
        }

        /// <summary>
        /// Adds a port to the collection of input ports
        /// </summary>
        /// <param name="port"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddPort(InputPort port) {
            AddPort(port, _inputs.Count);
        }

        /// <summary>
        /// Adds a port to the collection of output ports
        /// </summary>
        /// <param name="port"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddPort(OutputPort port) {
            AddPort(port, _outputs.Count);
        }

        /// <summary>
        /// Adds a port to the collection of input ports
        /// </summary>
        /// <param name="port"></param>
        /// <param name="index">Index at which to insert port</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddPort(InputPort port, int index) {
            if (port == null) throw new ArgumentNullException();
            if (index < 0) throw new ArgumentOutOfRangeException();
            if (!_inputs.Contains(port)) {
                _inputs.Insert(index, port);
                port.ConnectionChanged += Port_ConnectionChanged;
                PortAdded?.Invoke(this, new PortChangedEventArgs(port, index, PortChangedEventArgs.Modifier.Added));
            } else {
                throw new ArgumentException("Port already exists in collection");
            }
        }

        /// <summary>
        /// Adds a port to the collection of input ports
        /// </summary>
        /// <param name="port"></param>
        /// <param name="index">Index at which to insert port</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void AddPort(OutputPort port, int index) {
            if (port == null) throw new ArgumentNullException();
            if (index < 0) throw new ArgumentOutOfRangeException();
            if (!_outputs.Contains(port)) {
                _outputs.Insert(index, port);
                port.ConnectionAdded += Port_ConnectionChanged;
                port.ConnectionRemoved += Port_ConnectionChanged;
                PortAdded?.Invoke(this, new PortChangedEventArgs(port, index, PortChangedEventArgs.Modifier.Added));
            } else {
                throw new ArgumentException("Port already exists in collection");
            }
        }

        private void Port_ConnectionChanged(object sender, ConnectionModifiedEventArgs e) {
            PortConnectionChanged?.Invoke(sender, e);
        }

        public void Serialize(XmlWriter writer) {
            writer.WriteStartElement("node");
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("description", Description);
            Serializing(writer);
            writer.WriteEndElement();
        }

        protected virtual void Deserializing(XmlNode reader) {
            foreach (var child in reader.ChildNodes.OfType<XmlNode>()) {
                if (child.Name == "attribute") {
                    var attrName = child.Attributes.GetNamedItem("name").Value;
                    var attrValue = child.Attributes.GetNamedItem("value").Value;
                    var attr = Attributes.FirstOrDefault(a => a.Name == attrName);
                    if (attr != null) {
                        attr.Deserialize(attrValue);
                    } else {
                        System.Diagnostics.Debug.WriteLine("Deserialization: attribute not found: " + attrName);
                    }
                }
            }
        }

        protected virtual void Serializing(XmlWriter writer) {
            foreach (var attr in Attributes) {
                writer.WriteStartElement("attribute");
                writer.WriteAttributeString("name", attr.Name);
                writer.WriteAttributeString("value", attr.Serialize());
                writer.WriteEndElement();
            }
        }

        public virtual void Dispose() {
            //
        }
    }

}
