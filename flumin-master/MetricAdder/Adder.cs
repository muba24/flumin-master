using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.FormatData1D;
using System.Xml;

namespace MetricAdder
{
    [Metric(nameof(Adder), "Math")]
    public class Adder : StateNode<Adder> {

        private readonly InputPortData1D PortIn1;
        private readonly InputPortData1D PortIn2;
        private readonly OutputPortData1D PortOut;
        private TimeLocatedBuffer1D<double> OutputBuffer;

        public Adder(XmlNode node, Graph g) : this(g) {
            Deserializing(node);
        }

        public Adder(Graph g) : base(nameof(Adder), g) {
            PortIn1 = new InputPortData1D(this, "In1");
            PortIn2 = new InputPortData1D(this, "In2");
            PortOut = new OutputPortData1D(this, "Out");

            PortIn1.SamplerateChanged += PortIn_SamplerateChanged;
            PortIn2.SamplerateChanged += PortIn_SamplerateChanged;
        }

        private void PortIn_SamplerateChanged(object sender, SamplerateChangedEventArgs e) {
            // we expect both inputs to have the same data rate, so PortIn1 or PortIn2 does not matter
            PortOut.Samplerate = PortIn1.Samplerate;
        }

        public override void PrepareProcessing() {
            if (PortIn1.Samplerate != PortIn2.Samplerate) {
                throw new InvalidOperationException("Samplerates do not match");
            }
            
            PortIn1.PrepareProcessing();
            PortIn2.PrepareProcessing();
            PortOut.PrepareProcessing();

            OutputBuffer = new TimeLocatedBuffer1D<double>(PortOut.Buffer.Capacity, PortOut.Samplerate);
        }

        public override bool CanProcess => PortIn1.Available > 0 && PortIn2.Available > 0;

        public override bool CanTransfer => PortOut.Buffer.Available > 0;

        public override void Process() {
            var samplesAvailable = Math.Min(PortIn1.Available, PortIn2.Available);
            var samplesWritable = PortOut.Buffer.Free;
            var samplesToRead = Math.Min(samplesWritable, samplesAvailable);

            var buf1 = PortIn1.Read(samplesToRead);
            var buf2 = PortIn2.Read(samplesToRead);
            var output = OutputBuffer.Data;

            if (buf1.Available != buf2.Available) {
                throw new InvalidOperationException("Expected both reads to have the same sample count");
            }

            // TODO: Very vectorizable
            for (int i = 0; i < samplesAvailable; i++) {
                output[i] = buf1.Data[i] + buf2.Data[i];
            }

            OutputBuffer.SetWritten(samplesAvailable);
            PortOut.Buffer.Write(OutputBuffer, OutputBuffer.Available);
        }

        public override FlushState FlushData() {
            if (CanProcess) {
                Process();
                return FlushState.Some;
            }
            return FlushState.Empty;
        }

        public override void Transfer() {
            PortOut.Transfer();
        }

        public override void StartProcessing() { }
        public override void StopProcessing() { }
        public override void SuspendProcessing() { }

    }
}
