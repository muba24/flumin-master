using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewOpenGLRenderer {

    class FFTFrame {
        public int FFTSize { get; }
        public int Samplerate { get; }
        public double[] Data { get; }

        public FFTFrame(int size, int samplerate) {
            FFTSize = size;
            Samplerate = samplerate;
            Data = new double[FFTSize / 2];
        }

        public FFTFrame(int size, int samplerate, double[] data) {
            FFTSize = size;
            Samplerate = samplerate;
            Data = (double[])data.Clone();
        }
    }

}
