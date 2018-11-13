using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2 {

    public class Port {

        public enum Direction {
            Input, Output
        }

        public Direction FlowDirection { get; }

        public PortDataType DataType { get; }

        public string Name { get; private set; }

        public Node Parent {get;}

        public Port(Node parent, Direction dir, PortDataType dataType, string name) {
            FlowDirection = dir;
            DataType      = dataType;
            Name          = name;
            Parent        = parent;
        }

    }

}
