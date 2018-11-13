using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2 {
    class PortTypeMismatchException : Exception {
        public PortDataType Expected { get; }
        public PortDataType Given { get; }

        public PortTypeMismatchException(PortDataType expected, PortDataType given) {
            Expected = expected;
            Given    = given;
        }
    }
}
