using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib2.FormatValue;
using NodeSystemLib2.FormatData1D;
using NodeSystemLib2.FormatDataFFT;

namespace NodeSystemLib2.Generic {

    public class GenericNodeState<T> : NodeState where T : Node {

        private readonly T _node;

        public GenericNodeState(T node, TimeStamp stamp) : base(node, stamp) {
            _node = node;
        }

        protected override void LoadStateFields() {
            LoadStateFields(_node, this);
        }
    }

    public class NodeState {
        protected readonly Dictionary<string, object> PortValues = new Dictionary<string, object>();
        protected readonly Dictionary<string, object> FieldValues = new Dictionary<string, object>();
        protected readonly Dictionary<string, object> CustomValues = new Dictionary<string, object>();

        public Node Parent { get; }

        protected NodeState(Node parent, TimeStamp stamp) {
            Stamp = stamp;
            Parent = parent;
        }

        public TimeStamp Stamp { get; set; }

        public object this[string index] {
            get { return CustomValues[index]; }
            set { WriteValue(CustomValues, index, value); }
        }

        protected void WriteValue(Dictionary<string, object> dict, string index, object value) {
            if (!dict.ContainsKey(index)) {
                dict.Add(index, value);
            } else {
                dict[index] = value;
            }
        }

        public void Load() {
            foreach (var port in Parent.InputPorts) {
                if (Equals(port.DataType, PortDataTypes.TypeIdSignal1D)) {
                    LoadDataInputBuffer(this, to: (InputPortData1D) port);
                } else if (port.DataType.Equals(PortDataTypes.TypeIdValueDouble)) {
                    LoadValueInputBuffer(this, to: (InputPortValueDouble) port);
                } else if (port.DataType.Equals(PortDataTypes.TypeIdFFT)) {
                    LoadFftInputBuffer(this, to: (InputPortDataFFT) port);
                } else throw new NotImplementedException();
            }

            LoadStateFields();
        }

        private static void LoadDataInputBuffer(NodeState from, InputPortData1D to) {
            if (to.Queue != null) {
                to.LoadBuffer((SignalRingBuffer)from.PortValues[to.Name]);
            }
        }

        private static void LoadValueInputBuffer(NodeState from, InputPortValueDouble to) {
            if (to.Values != null) {
                var values = (ConcurrentTreeBag<TimeLocatedValue<>>)from.PortValues[to.Name];
                to.Values = values.Clone();
            }
        }

        private static void LoadFftInputBuffer(NodeState from, InputPortDataFFT to) {
            if (to.Queue != null) {
                to.LoadBuffer((SignalRingBuffer)from.PortValues[to.Name]);
            }
        }

        public static GenericNodeState<T> Save<T>(T n, TimeStamp time) where T : Node {
            var to = new GenericNodeState<T>(n, time);

            foreach (var port in n.InputPorts) {
                if (Equals(port.DataType, PortDataTypes.TypeIdSignal1D)) {
                    SaveDataInputBuffer((InputPortData1D)port, to);
                } else if (port.DataType.Equals(PortDataTypes.TypeIdValueDouble)) {
                    SaveDataInputBuffer((InputPortValueDouble)port, to);
                } else if (port.DataType.Equals(PortDataTypes.TypeIdFFT)) {
                    SaveDataInputBuffer((InputPortDataFFT)port, to);
                } else throw new NotImplementedException();
            }
            SaveStateFields(n, to);

            return to;
        }

        private static void SaveDataInputBuffer(InputPortData1D p, NodeState to) {
            
            if (p.Queue != null) {
                to.WriteValue(to.PortValues, p.Name, p.Queue.GetState());
            }
        }

        private static void SaveDataInputBuffer(InputPortValueDouble p, NodeState to) {
            if (p.Values != null) {
                to.WriteValue(to.PortValues, p.Name, p.Values.Clone());
            }
        }

        private static void SaveDataInputBuffer(InputPortDataFFT p, NodeState to) {
            if (p.Queue != null) {
                to.WriteValue(to.PortValues, p.Name, p.Queue.GetState());
            }
        }

        protected virtual void LoadStateFields() {
            LoadStateFields(Parent, this);
        }

        protected static void LoadStateFields<T>(T obj, NodeState from) {
            FieldInfo[] fields = (typeof(T)).GetFields(BindingFlags.NonPublic |
                                                    BindingFlags.Public |
                                                    BindingFlags.Instance);

            PropertyInfo[] props  = (typeof(T)).GetProperties(BindingFlags.NonPublic |
                                                            BindingFlags.Public |
                                                            BindingFlags.Instance);

            foreach (var field in from.FieldValues) {
                var fv = fields.FirstOrDefault(f => f.Name == field.Key);
                if (fv != null) {
                    fv.SetValue(obj, field.Value);
                } else {
                    var p = props.FirstOrDefault(f => f.Name == field.Key);
                    if (p != null) {
                        p.SetValue(obj, field.Value);
                    } else {
                        throw new InvalidOperationException("Field/property not found: " + field.Key);
                    }
                }
            }
        }

        protected static void SaveStateFields<T>(T obj, NodeState to) {
            PropertyInfo[] props  = (typeof(T)).GetProperties(BindingFlags.NonPublic |
                                                            BindingFlags.Public |
                                                            BindingFlags.Instance);

            FieldInfo[] fields    = (typeof(T)).GetFields(BindingFlags.NonPublic |
                                                        BindingFlags.Public |
                                                        BindingFlags.Instance);

            Action<string, object> saveValue = (name, value) => {
                var clonable = value as ICloneable;
                if (clonable != null) {
                    to.WriteValue(to.FieldValues, name, clonable.Clone());
                } else {
                    to.WriteValue(to.FieldValues, name, value);
                }
            };

            foreach (var prop in props) {
                var state = prop.GetCustomAttributes(typeof(StateAttribute), true);
                if (state.Length > 0) {
                    var value = prop.GetValue(obj);
                    saveValue(prop.Name, value);
                }
            }

            foreach (var fi in fields) {
                var state = fi.GetCustomAttributes(typeof(StateAttribute), true);
                if (state.Length > 0) {
                    var value = fi.GetValue(obj);
                    saveValue(fi.Name, value);
                }
            }
        }
    }

    public class NodeStateComparer : IComparer<NodeState> {

        public int Compare(NodeState x, NodeState y) {
            return x.Stamp.CompareTo(y.Stamp);
        }

    }

}
