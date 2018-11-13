using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Flumin;
using NodeSystemLib;
using MetricFileSource;

namespace SimpleADCTest {
    [TestClass]
    public class NodeStateTests {

        [TestMethod]
        public void run_filenode_save_state_and_restore() {
            var g    = new Graph();
            var node = new FileNode(g);
            var sink = new SinkNode(g);

            node.FileName = @"C:\tmp\set 1\signal 0.bin";
            node.Samplerate = 1000000;

            g.Connect(node.OutputPorts[0], sink.InputPorts[0]);

            Assert.IsTrue(g.Run());

            var fileSize = new System.IO.FileInfo(node.FileName).Length;

            node.TargetPosition = (fileSize / 3) - (fileSize / 3) % 8;

            while (node.Seeking) {
                System.Threading.Thread.Sleep(10);
            }

            var state = g.SaveState();

            node.TargetPosition = fileSize * 3 / 4 - (fileSize * 3 / 4) % 8;
            while (node.Seeking) {
                System.Threading.Thread.Sleep(10);
            }

            g.LoadState(state);
        }

        [TestMethod]
        public void run_graph_process_data_jump_back_restore_state() {
            var g      = new Graph();
            var node   = new BasicTestNode(g);
            var source = new SourceNode(g);
            var sink   = new SinkNode(g);

            g.Connect(source.OutputPorts[0], node.InputPorts[0]);
            g.Connect(node.OutputPorts[0], sink.InputPorts[0]);

            Assert.IsTrue(g.Run());
            Assert.AreEqual(node.Factor, 0);

            source.SendData(source.Samplerate);
            System.Threading.Thread.Sleep(1000);

            var state  = g.SaveState();
            var factor = node.Factor;
            var stamp  = ((DataInputPort)sink.InputPorts[0]).Queue.BeginTime;

            source.SendData(source.Samplerate);
            System.Threading.Thread.Sleep(1000);

            Assert.AreNotEqual(node.Factor, factor);

            g.LoadState(state);

            Assert.AreEqual(node.Factor, factor);
            Assert.AreEqual(stamp, ((DataInputPort)sink.InputPorts[0]).Queue.BeginTime);

            g.Stop();
        }

        [TestMethod]
        public void save_state_change_and_restore() {
            var g = new Graph();
            var n = new BasicTestNode(g);

            var before = n.Factor;

            var state = n.SaveState();
            n.Change();
            state.Load();

            var after = n.Factor;

            Assert.AreEqual(before, after);
        }

    }

    class BasicTestNode : Node {

        [State]
        private int factor;

        private readonly DataInputPort _inp;
        private readonly DataOutputPort _out;

        private TimeLocatedBuffer _buffer;

        public int Factor => factor;

        public BasicTestNode(Graph g) : base("Test", g) {
            _inp = InputPort.Create<DataInputPort>("input", this);
            _out = OutputPort.Create<DataOutputPort>("output", this);
        }

        protected override void InputSamplerateChanged(InputPort e) {
            _out.Samplerate = _inp.Samplerate;
        }

        public override bool PrepareProcessing() {
            _inp.InitBuffer();
            _buffer = new TimeLocatedBuffer(_inp.Samplerate / 10, _inp.Samplerate);
            return true;
        }

        protected override void DataAvailable(DataInputPort port) {
                _inp.Queue.Dequeue(_buffer);
                var samples = _buffer.GetSamples();
                for (int j = 0; j < _buffer.WrittenSamples; j++) {
                    samples[j] += factor++;
                }
                _out.SendData(_buffer);
        }

        public void Change() {
            factor++;
        }

        public override NodeState SaveState() {
            return NodeState.Save(this, Parent.GetCurrentClockTime());
        }

        public override void LoadState(NodeState state) {
            Assert.AreSame(state.Parent, this);
            state.Load();
        }

    }
}
