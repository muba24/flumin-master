using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2 {

    public class OutputPort : Port {

        private readonly List<InputPort> _connections;

        public IReadOnlyList<InputPort> Connections => _connections;

        public event EventHandler<ConnectionModifiedEventArgs> ConnectionAdded;
        public event EventHandler<ConnectionModifiedEventArgs> ConnectionRemoved;

        public OutputPort(Node parent, string name, PortDataType type) : base(parent, Direction.Output, type, name) {
            _connections = new List<InputPort>();
            parent.AddPort(this);
        }

        /// <summary>
        /// Adds a remote port to the connection list.
        /// Port must be an input.
        /// </summary>
        /// <param name="port"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="TypeAccessException"></exception>
        public virtual void AddConnection(InputPort port) {
            if (port == null) throw new ArgumentNullException();

            if (!port.DataType.Equals(DataType)) throw new PortTypeMismatchException(DataType, port.DataType);

            if (!_connections.Contains(port)) {
                _connections.Add(port);
                port.Connection = this;
                ConnectionAdded?.Invoke(this, new ConnectionModifiedEventArgs(ConnectionModifiedEventArgs.Modifier.Added, port));
            } else {
                throw new ArgumentException("element already exists", nameof(port));
            }
        }

        public virtual void RemoveConnection(InputPort port) {
            if (port == null) throw new ArgumentNullException();

            if (_connections.Contains(port)) {
                _connections.Remove(port);
                ConnectionRemoved?.Invoke(this, new ConnectionModifiedEventArgs(ConnectionModifiedEventArgs.Modifier.Removed, port));
            } else {
                throw new ArgumentException("element not found", nameof(port));
            }
        }

    }

}
