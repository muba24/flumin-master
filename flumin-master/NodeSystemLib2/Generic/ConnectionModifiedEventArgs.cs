using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2 {
    public class ConnectionModifiedEventArgs : EventArgs {
        public enum Modifier {
            Added, Removed, Changed
        }

        public readonly Port Connection;
        public readonly Modifier Action;

        public ConnectionModifiedEventArgs(Modifier m, Port p) {
            Action = m;
            Connection = p;
        }
    }
}
