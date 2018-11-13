using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeEditorLib.EditorControl {
  public  class OutputPort : Port {

        private List<InputPort> _ports = new List<InputPort>();

        public OutputPort(Node parent, string Name, PortDataType DataType) 
            : base(parent, Name, DataType, Direction.Output) {
        }

        public int ConnectionCount => _ports.Count;

        public IReadOnlyList<InputPort> Ports => _ports;

        public void AddConnection(InputPort p) {
            if (!p.DataType.Equals(this.DataType)) {
                throw new InvalidOperationException("Port types don't match");
            }
            _ports.Add(p);
        }

        public void RemoveConnection(InputPort p) {
            _ports.Remove(p);
        }

        internal void ClearConnections() {
            // .ToList() because target port will call RemoveConnection!
            // Without .ToList() Deadlock!
            foreach (var port in _ports.ToList()) {
                port.Connection = null;
            }
        }
    }
}
