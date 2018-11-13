using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic {

    public interface INodeUi {

        void OnLoad(NodeEditorLib.EditorControl.Node node);

        void OnDoubleClick();

        void OnDraw(Rectangle node, Graphics e);

    }

}
