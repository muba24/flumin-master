using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NodeEditorLib.EditorControl {

    public abstract class Port {

        public enum Direction {
            Input, Output
        }

        public Node Parent { get; }
        public string Name { get; set; }
        public PortDataType DataType { get; }
        public Direction FlowDirection { get; }
        public Size Size { get; set; }
        public Padding Margin { get; set; }
        public int Top { get; set; }

        public Port (Node parent, string Name, PortDataType DataType, Direction FlowDirection) {
            this.Parent = parent;
            this.Name = Name;
            this.DataType = DataType;
            this.FlowDirection = FlowDirection;
            this.Size = new Size(10, 10);
            this.Margin = new Padding(2, 4, 2, 4);
        }

        public void Render(Graphics g) {
            g.FillEllipse(DataType.ColorBrush, 0, Margin.Top, Size.Width, Size.Height);
            g.DrawEllipse(Pens.DarkGray, 0, Margin.Top, Size.Width, Size.Height);

            var rcText = g.MeasureString(Name, Parent.Parent.Font);

            if (FlowDirection == Direction.Input) {
                g.DrawString(Name, Parent.Parent.Font, Brushes.White, new PointF(Size.Width + Margin.Horizontal, 0));
            } else {
                g.DrawString(Name, Parent.Parent.Font, Brushes.White, new PointF(-rcText.Width, 0));
            }
        }

    }

}
