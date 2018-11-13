using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeEditorLib.EditorControl {

   public class PortDataType {
        public string Name { get; }
        public Guid Id { get; }
        public Color Color { get; }
        public Brush ColorBrush { get; }
        public Pen ColorPen { get; }

        public PortDataType(Guid id , string name , Color color) {
            this.Id = id;
            this.Name = name;
            this.Color = color;
            this.ColorBrush = new SolidBrush(color);
            this.ColorPen = new Pen(color, 2);
        }

        public override bool Equals(object obj) {
            if (obj is PortDataType) {
                return ((PortDataType)obj).Id.Equals(Id);
            }
            return base.Equals(obj);
        }
    }

}
