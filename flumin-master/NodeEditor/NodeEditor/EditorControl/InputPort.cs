using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeEditorLib.EditorControl  {
   public class InputPort : Port {

        private OutputPort _connection;

        public InputPort(Node parent, string Name, PortDataType DataType) 
            : base(parent, Name, DataType, Direction.Input) {
        }

        public OutputPort Connection {
            get { return _connection; }
            set {
                if (value != null) {
                    if (!value.DataType.Equals(this.DataType)) {
                        throw new InvalidOperationException("Port types don't match");
                    }
                }

                _connection = value;
            }
        }

    }
}
