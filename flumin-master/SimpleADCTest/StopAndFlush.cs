using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using NodeSystemLib;
using Flumin;

namespace SimpleADCTest {

    [TestClass]
    public class StopAndFlush {

        [TestMethod]
        public void send_nice_data_and_test_if_all_stopped_and_empty_also_unconnected_nodes() {
            var graph   = new Graph();
            var source  = new SourceNode(graph);
            var sink    = new SinkNode(graph);
            var sink2   = new SinkNode(graph);
            var pass1   = new PassThroughNode(graph);
            var pass2   = new PassThroughNode(graph);

            graph.Connect(source.OutputPorts[0], pass1.InputPorts[0]);
            graph.Connect(pass1.OutputPorts[0], sink.InputPorts[0]);

            // pass2 is not connected a source
            graph.Connect(pass2.OutputPorts[0], sink2.InputPorts[0]);
            ((DataInputPort)pass2.InputPorts[0]).Samplerate = 100;

            Assert.IsTrue(graph.Run(),
                          "Graph.Run() must succeed");

            const int PaketsPerSecond = 4;
            const int Seconds = 1;

            for (int i = 0; i < Seconds * PaketsPerSecond; i++) {
                System.Threading.Thread.Sleep(1000 / PaketsPerSecond);
                source.SendData(source.Samplerate / PaketsPerSecond);
            }

            System.Diagnostics.Debug.WriteLine("Graph Stop");
            graph.Stop();
            System.Diagnostics.Debug.WriteLine("All Done");

            foreach (var node in graph.Nodes) {
                Assert.IsTrue(node.State == Node.ProcessingState.Stopped, "All nodes must stop");
            }

            Assert.IsTrue((sink.InputPorts[0] as DataInputPort).Queue.Length == 0);
            Assert.IsTrue((sink2.InputPorts[0] as DataInputPort).Queue.Length == 0);
            Assert.IsTrue((pass1.InputPorts[0] as DataInputPort).Queue.Length == 0);
            Assert.IsTrue((pass2.InputPorts[0] as DataInputPort).Queue.Length == 0);
        }

        [TestMethod]
        public void send_nice_data_and_test_if_all_stopped_and_empty_multi_way() {
            var graph   = new Graph();
            var source1 = new SourceNode(graph);
            var source2 = new SourceNode(graph);
            var sink    = new SinkNode(graph);
            var pass    = new PassThroughNode(graph);
            var adder   = new MetricAdder.Adder(graph);
            
            graph.Connect(source1.OutputPorts[0], adder.InputPorts[0]);
            graph.Connect(source2.OutputPorts[0], adder.InputPorts[1]);
            graph.Connect(adder.OutputPorts[0], pass.InputPorts[0]);
            graph.Connect(pass.OutputPorts[0], sink.InputPorts[0]);

            Assert.IsTrue(graph.Run(),
                          "Graph.Run() must succeed");

            const int PaketsPerSecond = 4;
            const int Seconds = 1;

            for (int i = 0; i < Seconds * PaketsPerSecond; i++) {
                System.Threading.Thread.Sleep(1000 / PaketsPerSecond);
                source1.SendData(source1.Samplerate / PaketsPerSecond);
                source2.SendData(source2.Samplerate / PaketsPerSecond);
            }

            System.Diagnostics.Debug.WriteLine("Graph Stop");
            graph.Stop();
            System.Diagnostics.Debug.WriteLine("All Done");

            foreach (var node in graph.Nodes) {
                Assert.IsTrue(node.State == Node.ProcessingState.Stopped, "All nodes must stop");
            }

            Assert.IsTrue((sink.InputPorts[0] as DataInputPort).Queue.Length == 0);
            Assert.IsTrue((pass.InputPorts[0] as DataInputPort).Queue.Length == 0);
            Assert.IsTrue((adder.InputPorts[0] as DataInputPort).Queue.Length == 0);
            Assert.IsTrue((adder.InputPorts[1] as DataInputPort).Queue.Length == 0);
        }

        [TestMethod]
        public void send_nice_data_and_test_if_all_stopped_and_empty() {
            var graph   = new Graph();
            var source  = new SourceNode(graph);
            var sink    = new SinkNode(graph);
            var pass1   = new PassThroughNode(graph);
            var pass2   = new PassThroughNode(graph);

            graph.Connect(source.OutputPorts[0], pass1.InputPorts[0]);
            graph.Connect(pass1.OutputPorts[0], pass2.InputPorts[0]);
            graph.Connect(pass2.OutputPorts[0], sink.InputPorts[0]);

            Assert.IsTrue(graph.Run(),
                          "Graph.Run() must succeed");

            const int PaketsPerSecond = 4;
            const int Seconds = 1;

            for (int i = 0; i < Seconds * PaketsPerSecond; i++) {
                System.Threading.Thread.Sleep(1000 / PaketsPerSecond);
                source.SendData(source.Samplerate / PaketsPerSecond);
            }
            
            System.Diagnostics.Debug.WriteLine("Graph Stop");
            graph.Stop();
            System.Diagnostics.Debug.WriteLine("All Done");

            foreach (var node in graph.Nodes) {
                Assert.IsTrue(node.State == Node.ProcessingState.Stopped, "All nodes must stop");
            }

            Assert.IsTrue((sink.InputPorts[0] as DataInputPort).Queue.Length == 0);
            Assert.IsTrue((pass1.InputPorts[0] as DataInputPort).Queue.Length == 0);
            Assert.IsTrue((pass2.InputPorts[0] as DataInputPort).Queue.Length == 0);
        }


        // for a description of what unfriendly means, see 
        // send_nice_data_and_test_if_sink_recieves_all().
        [TestMethod]
        public void send_unfriendly_data_and_test_if_sink_recieves_all() {
            var graph  = new Graph();
            var source = new SourceNode(graph);
            var sink   = new SinkNode(graph);

            graph.Connect(source.OutputPorts[0], sink.InputPorts[0]);

            Assert.IsTrue(graph.Run(),
                          "Graph.Run() must succeed");

            const int PaketsPerSecond = 7;
            const int Seconds = 1;

            double SamplesPerPaket = source.Samplerate / (double)PaketsPerSecond;
            double SampleCounter = SamplesPerPaket;
            int samplesToSend = (int)SamplesPerPaket;

            for (int i = 0; i < Seconds * PaketsPerSecond; i++) {
                source.SendData(samplesToSend);

                samplesToSend = (int)((SampleCounter + SamplesPerPaket) - Math.Floor(SampleCounter));
                SampleCounter += SamplesPerPaket;
                
                System.Threading.Thread.Sleep(1000 / PaketsPerSecond);
            }

            graph.Stop();

            var sumShouldBe = source.Samplerate * Seconds;
            var sumIs       = sink.Sum;
            var diff        = Math.Abs(sumIs - sumShouldBe);

            Assert.IsTrue(diff < 0.0000001,
                          $"Sum Diff should be 0, but is {diff}");
        }


        // "nice" means the paket sizes are a good fit for the samplerate.
        // Here PaketsPerSecond is chosen to be 4 - the source's samplerate is 100.
        // 100 / 4 = 25 -> 25 Samples per paket.
        // Now if we chose the paket rate to be 7 (as an example), 100 / 7 = 14.28571...
        // There is no 0.28571... of a sample. There can only be whole samples.
        // That case is covered in another "unfriendly" test.
        [TestMethod]
        public void send_nice_data_and_test_if_sink_recieves_all() {
            var graph  = new Graph();
            var source = new SourceNode(graph);
            var sink   = new SinkNode(graph);

            graph.Connect(source.OutputPorts[0], sink.InputPorts[0]);

            Assert.IsTrue(graph.Run(),
                          "Graph.Run() must succeed");

            const int PaketsPerSecond = 4;
            const int Seconds = 1;

            for (int i = 0; i < Seconds * PaketsPerSecond; i++) {
                source.SendData(source.Samplerate / PaketsPerSecond);
                System.Threading.Thread.Sleep(1000 / PaketsPerSecond);
            }

            graph.Stop();

            var sumShouldBe = source.Samplerate * Seconds;
            var sumIs       = sink.Sum;
            var diff        = Math.Abs(sumIs - sumShouldBe);

            Assert.IsTrue(diff < 0.0000001, 
                          $"Sum Diff should be 0, but is {diff}");
        }

    }

    // -----------------------------------------------------------
    // -----------------------------------------------------------

    class PassThroughNode : Node {
        private readonly DataInputPort _input;
        private readonly DataOutputPort _output;
        private TimeLocatedBuffer _buffer;

        public PassThroughNode(Graph g) : base("Passthrough", g) {
            _input = InputPort.Create<DataInputPort>("Inp", this);
            _output = OutputPort.Create<DataOutputPort>("Out", this);
        }

        public override bool PrepareProcessing() {
            _input.InitBuffer();
            _input.Queue.SizeFixed = true;

            if (_buffer == null) {
                _buffer = new TimeLocatedBuffer(_input.Samplerate / 10, _input.Samplerate);
            }

            return true;
        }

        protected override void InputSamplerateChanged(InputPort e) {
            _output.Samplerate = ((DataInputPort)e).Samplerate;
        }

        public override FlushState FlushData() {
            if (_input.Queue.Length > 0) {
                DataAvailable(_input);
                return FlushState.Some;
            }
            return FlushState.Empty;
        }

        protected override void DataAvailable(DataInputPort port) {
            System.Diagnostics.Debug.WriteLine($"{Name}: DataAvailable mit {_input.Queue.Length} Samples im InputBuffer");

            var timesReadable = Math.Max(1, _input.Queue.Length / _buffer.Length);

            for (int j = 0; j < timesReadable; j++) {
                _input.Queue.Dequeue(_buffer);
                _output.SendData(_buffer);
            }
        }


    }

    // -----------------------------------------------------------
    // -----------------------------------------------------------

    class SinkNode : Node {
        private readonly DataInputPort _input;
        private TimeLocatedBuffer _buffer;

        [State]
        public double Sum { get; private set; }

        public SinkNode(Graph g) : base("Sink", g) {
            _input = InputPort.Create<DataInputPort>("Inp", this);
        }

        public override bool PrepareProcessing() {
            _input.InitBuffer();
            _input.Queue.SizeFixed = true;

            if (_buffer == null) {
                _buffer = new TimeLocatedBuffer(_input.Samplerate / 10, _input.Samplerate);
            }

            return true;
        }

        public override FlushState FlushData() {
            if (_input.Queue.Length > 0) {
                System.Diagnostics.Debug.WriteLine($"Sink Flush with {_input.Queue.Length} samples");
                DataAvailable(_input);
            }
            return FlushState.Empty;
        }

        protected override void DataAvailable(DataInputPort port) {
            System.Diagnostics.Debug.WriteLine($"{Name}: DataAvailable mit {_input.Queue.Length} Samples im InputBuffer");

            var timesReadable = Math.Max(1, _input.Queue.Length / _buffer.Length);

            for (int j = 0; j < timesReadable; j++) {
                _input.Queue.Dequeue(_buffer);

                System.Diagnostics.Debug.WriteLine($"{Name}: DataAvailable Dequeue mit {_buffer.WrittenSamples} Samples");

                var samples = _buffer.GetSamples();
                for (int i = 0; i < _buffer.WrittenSamples; i++) {
                    Sum += samples[i];
                }
            }
        }

        public override NodeState SaveState() {
            return NodeState.Save(this, Parent.GetCurrentClockTime());
        }

        public override void LoadState(NodeState state) {
            Assert.AreSame(this, state.Parent);
            state.Load();
        }

    }

    // -----------------------------------------------------------
    // -----------------------------------------------------------

    class SourceNode : Node {
        private readonly DataOutputPort _output;
        private TimeLocatedBuffer _buffer;

        public int Samplerate {
            get { return _output.Samplerate; }
            set { _output.Samplerate = value; }
        }

        public SourceNode(Graph g) : base("Source", g) {
            _output = OutputPort.Create<DataOutputPort>("Out", this);
            Samplerate = 100;
        }

        public void SendData(int sampleCount) {
            System.Diagnostics.Debug.WriteLine($"{Name}: SendData mit {sampleCount}");

            CheckBufferSize(ref _buffer, sampleCount, Samplerate);

            var samples = _buffer.GetSamples();
            for (int i = 0; i < sampleCount; i++) {
                samples[i] = 1;
            }
            _buffer.SetWritten(sampleCount);

            _output.SendData(_buffer);
        }

        private static void CheckBufferSize(ref TimeLocatedBuffer buffer, int size, int rate) {
            if (buffer == null || buffer.Length < size) {
                buffer = new TimeLocatedBuffer(size, rate);
            }
        }

    }

}
