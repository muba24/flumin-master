using System.Drawing;

namespace WaveDisplayControl {
    public interface IYScaleProvider {

        double YMin { get; }
        double YMax { get; }

        bool ValidateAndSetYMin(double yMin);
        bool ValidateAndSetYMax(double yMax);

        void Render(Graphics g, Rectangle area);

    }
}