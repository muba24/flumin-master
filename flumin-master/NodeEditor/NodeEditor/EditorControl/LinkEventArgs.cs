using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeEditorLib.EditorControl {
    public class LinkEventArgs : EventArgs {
        public InputPort PortIn { get; }
        public OutputPort PortOut { get; }

        public LinkEventArgs(InputPort pIn, OutputPort pOut) {
            PortIn = pIn;
            PortOut = pOut;
        }
    }
}
