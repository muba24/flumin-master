using System.Drawing;

namespace NodeSystemLib {
    public interface INodeUi {

        void OnLoad(NodeGraphControl.NodeGraphNode node);

        void OnDoubleClick();

        void OnDraw(Rectangle node, Graphics e);

    }
}