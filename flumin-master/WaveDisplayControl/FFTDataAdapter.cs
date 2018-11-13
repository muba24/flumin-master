using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaveDisplayControl {
    public class FftDataAdapter : IDataCallback<double> {

        public long Start { get; set; }
        public long Count { get; set; }
        public IWaveData From { get; set; }

        public FftDataAdapter(IWaveData from) {
            From = from;
        }

        public IEnumerable<double> GetData(long index, long count, long step) {
            if (index > Count) {
                return new double[0];
            }

            return From.Iterate(Start + index, count, step);
        }

        public long GetLength() {
            return Count;
        }

        public long FillBuffer(long index, long count, long step, double[] buffer) {
            return From.GetData(index, count, step, buffer);
        }
    }

}
