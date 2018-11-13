using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeEditorLib.EditorControl {
    public class NodeCollectionModifiedEventArgs : EventArgs {
        public enum ActionType {
            Added, Removed
        }

        public Node Item;
        public ActionType Action;
    }
}
