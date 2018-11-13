using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Threading;
using MoreLinq;

namespace NodeSystemLib {

    public class Graph : IDisposable {

        public enum Status {
            Stopped, Running
        }

        private Status _state = Status.Stopped;

        public ThreadSafeObservableCollection<Node> Nodes = new ThreadSafeObservableCollection<Node>();

        internal void PlanEvent(TimeStamp stamp, EventInputPort con) {
            _processor.PostEvent(new Event(stamp, con));
        }

        public Node ClockSynchronizationNode { get; set; }

        private PerformanceCounter OsClock { get; }

        private GraphProcessor _processor;

        // TODO: Probably not atomic access
        private double CorrectionFactor = 0;

        public GraphProcessor Processor => _processor;

        public void PostData(OutputPort port, TimeLocatedBuffer buffer) {
            _processor.PostData(port, buffer);
        }

        public void SynchronizeClock(TimeStamp shouldBe) {
            var now = GetCurrentClockTime();
            CorrectionFactor += (shouldBe.Value - now.Value);
        }

        public TimeStamp GetCurrentClockTime() {
            var ticks = OsClock.GetTicks();
            var ms = OsClock.TicksToMilliseconds(ticks);
            return new TimeStamp(ms).Add(CorrectionFactor);
        }

        public Status State {
            get { return _state; }
            private set { _state = value; StatusChanged?.Invoke(this, value); }
        }

        [Browsable(false)]
        public int RunCount { get; private set; }

        public string BaseDirectory { get; set; } = "C:\\tmp\\";

        [Browsable(false)]
        public string WorkingDirectory { get; private set; }

        public string WorkingDirectoryMask { get; set; } = "set %count%";

        private readonly object _writerLock = new object();
        //private RecordSetWriter _setWriter;

        public event EventHandler<Status> StatusChanged;

        public event EventHandler<InputPort> Disconnected;

        public event EventHandler<InputPort> Connected;

        public Graph() {
            OsClock = new PerformanceCounter();
            NodeSystemSettings.Instance.SystemHost.RegisterGraph(this);
        }

        //public bool AddRecord(Recording rec) {
        //    lock (_writerLock) {
        //        if (_setWriter == null) {
        //            var file = "index.lst";
        //            var path = System.IO.Path.Combine(BaseDirectory, WorkingDirectory, file);

        //            try {
        //                _setWriter = new RecordSetWriter(path);
        //            } catch (Exception) {
        //                return false;
        //            }
        //        }

        //        _setWriter.AddRecord(rec);
        //        return true;
        //    }
        //}

        public bool Run() {
            if (State == Status.Running) return true;

            var workingDirRel = WorkingDirectoryMask.Replace("%count%", RunCount.ToString());
            WorkingDirectory  = System.IO.Path.Combine(BaseDirectory, workingDirRel);

            while (System.IO.Directory.Exists(WorkingDirectory)) {
                RunCount++;
                workingDirRel    = WorkingDirectoryMask.Replace("%count%", RunCount.ToString());
                WorkingDirectory = System.IO.Path.Combine(BaseDirectory, workingDirRel);
            }

            // 1. Hole Liste von Device Nodes
            var devNodes = Nodes.Where(HasNoInputConnections);

            // 2. Erstelle Queue mit Verknüpfungszielen
            var nodeQueue = new Queue<Node>();
            devNodes.ForEach(nodeQueue.Enqueue);

            var visited = new List<Node>();

            try {
                BFS(
                    nodeQueue,
                    visited,
                    (node) => {
                        if (!node.PrepareProcessing()) {
                            throw new OperationCanceledException();
                        }
                    },
                    NodesReachableByOutput
                );

            } catch (OperationCanceledException) {
                return false;

            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine($"Graph.Run Exception: {e}");
                return false;

            }

            OsClock.Start();
            SynchronizeClock(TimeStamp.Zero());

            _processor = new GraphProcessor(this);
            _processor.Start();

            Nodes.ForEach(n => n.StartProcessing());

            State = Status.Running;

            return true;
        }

        /// <summary>
        /// tries to stop the graph. This will only fail if Stop is already running in another thread.
        /// </summary>
        /// <returns></returns>
        public bool TryStop() {
            if (Monitor.TryEnter(this)) {
                Stop();
                Monitor.Exit(this);
                return true;
            }
            return false;
        }
        
        private Func<Node, bool> HasNoOutputConnections = (n) => n.OutputPorts.Count(p => p.Connections.Count > 0) == 0;
        private Func<Node, bool> HasNoInputConnections = (n) => n.InputPorts.Count(p => p.Connection != null) == 0;

        public void Stop() {
            lock (this) {
                if (State == Status.Stopped) return;

                // 1. Hole Liste von Device Nodes
                var devNodes = Nodes.Where(HasNoInputConnections);

                // 2. Erstelle Queue mit Verknüpfungszielen
                var nodeQueue = new Queue<Node>();
                devNodes.ForEach(nodeQueue.Enqueue);

                var visited = new List<Node>();

                // 3. Breitensuche mit Node Suspend
                BFS(
                    nodeQueue,
                    visited,
                    (node) => { System.Diagnostics.Debug.WriteLine($"Wait for {node.Name}"); node.SuspendProcessing(); },
                    NodesReachableByOutput
                );

                _processor.CompleteProcessing();
                _processor.Stop();

                // 4. Hole Liste mit Endpunkten für Vorgängerknoten
                var lastNodes = Nodes.Where(HasNoOutputConnections);


                // 5. Flush von hinten nach vorne (Breitensuche rückwärts)
                // 6. Falls Daten erzeugt wurden, gehe zu 5.
                bool reflush;
                do {
                    if (nodeQueue.Any()) throw new InvalidOperationException("Queue muss an diesem Punkt leer sein!");
                    lastNodes.ForEach(nodeQueue.Enqueue);

                    visited.Clear();
                    reflush = false;

                    BFS(
                        nodeQueue,
                        visited,
                        (node) => { reflush |= (node.FlushData() == Node.FlushState.Some); },
                        NodesReachableByInput
                    );
                } while (reflush);

                // 7. Alle Nodes stoppen (State reset)
                Nodes.ForEach(n => n.StopProcessing());

                // Vielleicht nicht so gut, weil eventuell Threads noch fertiglaufen müssen?
                // Garantie, dass Nodes alle gestoppt sind?
                OsClock.Stop();

                RunCount++;
                //_setWriter = null;
                State = Status.Stopped;
            }
        }

        private static void BFS<T>(Queue<T> queue, List<T> visited, Action<T> action, Func<T, IEnumerable<T>> selector) {
            while (queue.Any()) {
                var node = queue.Dequeue();
                if (node != null && !visited.Contains(node)) {
                    action(node);
                    visited.Add(node);
                    selector(node).ForEach(queue.Enqueue);
                }
            }
        }

        private IEnumerable<Node> NodesReachableByInput(Node node) {
            return node.InputPorts.Select(p => p.Connection?.Parent);
        }

        private IEnumerable<Node> NodesReachableByOutput(Node node) {
            foreach (var p in node.OutputPorts) {
                foreach (var c in p.Connections) {
                    yield return c?.Parent;
                }
            }
            yield break;
        }

        public void Connect(OutputPort outp, InputPort inp) {
            if (outp.DataType != inp.DataType) throw new InvalidOperationException("Port data types do not match");

            Disconnect(inp);
            inp.Connection = outp;
            outp.Connections.Add(inp);
            Connected?.Invoke(this, inp);
        }

        public void Disconnect(InputPort inp) {
            if (inp.Connection != null) {
                inp.Connection.Connections.Remove(inp);
                inp.Connection = null;
                Disconnected?.Invoke(this, inp);
            }
        }

        public void LoadState(Dictionary<Node, NodeState> states) {
            if (State == Status.Running) {
                _processor.CompleteProcessing();
                _processor.Stop();
            }

            // ok, load states
            foreach (var node in Nodes) {
                if (states.ContainsKey(node)) {
                    node.LoadState(states[node]);
                }
            }

            if (State == Status.Running) {
                _processor = new GraphProcessor(this);
                _processor.Start();
            }
        }

        public Dictionary<Node, NodeState> SaveState() {
            if (State == Status.Running) {
                _processor.CompleteProcessing();
                _processor.Stop();
            }

            // ok, save states
            var states = new Dictionary<Node, NodeState>();
            foreach (var node in Nodes) {
                var state = node.SaveState();
                if (state != null) {
                    states.Add(node, state);
                }
            }

            if (State == Status.Running) {
                _processor = new GraphProcessor(this);
                _processor.Start();
            }

            return states;
        }

        public void Dispose() {
            if (State != Status.Stopped) Stop();
            NodeSystemSettings.Instance.SystemHost.UnregisterGraph(this);
        }
    }

}
