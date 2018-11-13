using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluPy {
    public class PyException : Exception {

        public int Line { get; }
        public int Offset { get; }

        public PyException(string msg, int line, int offset) : base(msg) {
            Line = line;
            Offset = offset;
        }
    }
}
