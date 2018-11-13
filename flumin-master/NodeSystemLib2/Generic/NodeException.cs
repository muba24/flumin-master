using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic {
    public class NodeException : Exception {
        public Node Node { get; }
        public Exception Exception { get; }

        public NodeException(Node n, Exception e) {
            Node = n;
            Exception = e;
        }
    }
}
