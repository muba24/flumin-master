using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FormatValue {

    public class OuputPortValue<T> : OutputPort {

        private readonly List<TimeLocatedValue<T>> _values;

        public OuputPortValue(Node parent, string name, PortDataType type) : base(parent, name, type) {
            _values = new List<TimeLocatedValue<T>>();
        }

        public void PrepareProcessing() {
            _values.Clear();
        }

        public int BufferedValueCount => _values.Count;

        public void BufferForTransfer(TimeLocatedValue<T> value) {
            lock (_values) {
                _values.Add(value);
            }
        }

        public void TransferBuffer() {
            lock (_values) {
                foreach (var value in _values) {
                    foreach (var con in Connections.OfType<InputPortValue<T>>()) {
                        con.Write(value);
                    }
                }
                _values.Clear();
            }
        }

    }

}
