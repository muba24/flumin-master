using NodeSystemLib2.Generic;
using NodeSystemLib2;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewOpenGLRenderer {

    public interface ITimeData : IData {

        int SamplesPerSecond { get; }
        TimeInterval Duration { get; }

    }

    public interface IData {

        string Name { get; set; }
        Color LineColor { get; set; }
        bool Selected { get; set; }
        bool Visible { get; set; }

        void Clear();

        void Render(ShaderColorXY shader, RectangleF view, Size areaInPixels, TimeStamp offset);

    }

}
