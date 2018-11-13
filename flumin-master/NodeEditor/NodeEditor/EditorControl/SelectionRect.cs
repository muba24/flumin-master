using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NodeEditorLib.EditorControl {
    class SelectionRect {

        private Point _startAt;
        private Point _currentAt;

        public void Start(Point at) {
            _startAt = at;
            _currentAt = at;
        }

        public void Update(Point at) {
            _currentAt = at;
        }

        public Rectangle Area {
            get {
                var pointTopLeft = new Point(
                Math.Min(_startAt.X, _currentAt.X),
                Math.Min(_startAt.Y, _currentAt.Y)
            );

                var pointBottomRight = new Point(
                Math.Max(_startAt.X, _currentAt.X),
                Math.Max(_startAt.Y, _currentAt.Y)
            );

                var size = new Size(pointBottomRight.X - pointTopLeft.X,
                                pointBottomRight.Y - pointTopLeft.Y);

                return new Rectangle(pointTopLeft, size);
            }
        }

        public void Paint(Graphics g) {
            var rc = Area;
            var rcInner = Rectangle.Inflate(rc, -2, -2);

            ControlPaint.DrawSelectionFrame(g, true, rc, rcInner, Color.Green);
        }

    }
}
