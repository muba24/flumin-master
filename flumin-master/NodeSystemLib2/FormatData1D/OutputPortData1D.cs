using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FormatData1D {

    public class OutputPortData1D : OutputPort {

        private RingBuffer1D<double> _queue;

        private TimeLocatedBuffer1D<double> _transferBuffer;

        private int _samplerate;

        public event EventHandler<SamplerateChangedEventArgs> SamplerateChanged;

        public RingBuffer1D<double> Buffer => _queue;

        public OutputPortData1D(Node parent, string name) : base(parent, name, PortDataTypes.TypeIdSignal1D) {}

        public int Samplerate {
            get { return _samplerate; }
            set {
                if (value < 0) throw new ArgumentOutOfRangeException();

                _samplerate = value;

                foreach (var connection in Connections) {
                    ((InputPortData1D)connection).Samplerate = _samplerate;
                }

                SamplerateChanged?.Invoke(this, new SamplerateChangedEventArgs(value));
            }
        }

        public override void AddConnection(InputPort port) {
            base.AddConnection(port);
            ((InputPortData1D)port).Samplerate = Samplerate;
        }

        public void PrepareProcessing() {
            PrepareProcessing(
                DefaultParameters.DefaultQueueMilliseconds.ToSamples(Samplerate),
                DefaultParameters.DefaultBufferMilliseconds.ToSamples(Samplerate)
            );
        }

        public void PrepareProcessing(int queueSize, int transferBufferSize) {
            if (Samplerate < 0) throw new ArgumentOutOfRangeException();
            if (queueSize < 0) throw new ArgumentOutOfRangeException();

            _queue = new RingBuffer1D<double>(queueSize, Samplerate);
            _transferBuffer = new TimeLocatedBuffer1D<double>(transferBufferSize, Samplerate);
        }

        public void Transfer() {
            var minFree = Connections.Count > 0 ? Connections.Min(c => ((InputPortData1D)c).Free) : int.MaxValue;
            var count = Math.Min(_transferBuffer.Capacity, minFree);

            var read = _queue.Read(_transferBuffer, count);

            foreach (var input in Connections) {
                var written = ((InputPortData1D)input).Write(_transferBuffer);
                if (written != read) {
                    throw new InvalidOperationException();
                }
            }
        }

    }

}
