using System.Drawing;

namespace SimpleADC.NodeSystem {
    public interface INodeUi {

        void OnLoad(NodeGraphControl.NodeGraphNode node);

        void OnDoubleClick();

        void OnDraw(Rectangle node, Graphics e);

    }
}