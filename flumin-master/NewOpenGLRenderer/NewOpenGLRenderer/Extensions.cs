using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewOpenGLRenderer {

    static class Extensions {

        public static Color GetContrastingColor(this Color value) {
            var d = 0;

            // Counting the perceptive luminance - human eye favors green color... 
            double a = 1 - (0.299 * value.R + 0.587 * value.G + 0.114 * value.B) / 255;

            if (a < 0.5)
                d = 0; // bright colors - black font
            else
                d = 255; // dark colors - white font

            return Color.FromArgb(d, d, d);
        }

    }

}
