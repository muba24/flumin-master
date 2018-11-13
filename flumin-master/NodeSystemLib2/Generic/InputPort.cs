using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2 {

    public class InputPort : Port {

        private OutputPort _connection;

        public event EventHandler<ConnectionModifiedEventArgs> ConnectionChanged;

        public OutputPort Connection {
            get { return _connection; }
            set {
                if (value != null && !value.DataType.Equals(DataType)) {
                    throw new PortTypeMismatchException(DataType, value.DataType);
                }

                if (value != _connection) {
                    _connection = value;
                    if (_connection != null && !_connection.Connections.Contains(this)) {
                        _connection.AddConnection(this);
                    }
                    ConnectionChanged?.Invoke(this, new ConnectionModifiedEventArgs(ConnectionModifiedEventArgs.Modifier.Changed, value));
                }
            }
        }

        public InputPort(Node parent, string name, PortDataType type) : base(parent, Direction.Input, type, name) {
            parent.AddPort(this);
        }

    }

}
