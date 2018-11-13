using System.Drawing;

namespace WaveDisplayControl {

    public interface IDataRenderer {

        double Samplerate { get; }

        long DataLength { get; }

        IYScaleProvider YScale { get; }

        void Resize(int width, int height);

        void Render(Graphics g, Rectangle area, long start, long count, long step);

    }

}
