using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeEditorLib.EditorControl {
    static class RectangleHelper {

        public static Rectangle ToRectangle(this RectangleF rcf) {
            return new Rectangle(
                (int)rcf.X, (int)rcf.Y, (int)rcf.Width, (int)rcf.Height
            );
        }

        public static Point Center(this Rectangle rc) {
            return new Point(rc.Left + rc.Width / 2, rc.Top + rc.Height / 2);
        }

        public static RectangleF ToRectangleF(this Rectangle rc) {
            return new Rectangle(
                rc.X, rc.Y, rc.Width, rc.Height
            );
        }

    }
}
