using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceLibrary;
using System.ComponentModel;
using System.Threading;

namespace SimpleADC.NodeSystem {

    public class Graph {

        public enum Status {
            Stopped, Running
        }

        private Status _state = Status.Stopped;

        public ThreadSafeObservableCollection<Node> Nodes = new ThreadSafeObservableCollection<Node>();

        private List<DeviceLibrary.IDevice> _devices = new List<IDevice>();

        public Node ClockSynchronizationNode { get; set; }

        private PerformanceCounter OsClock { get; }

        private long CorrectionFactor = 0;

        public IReadOnlyList<IDevice> Devices => _devices;

        public void SynchronizeClock(TimeStamp shouldBe) {
            var timeIs = GetCurrentClockTime();
            if (timeIs < shouldBe || timeIs > shouldBe) {
                // should be +=, not =
                // because GetCurrentClockTime's returned value is already biased by CorrectionFactor
                CorrectionFactor += (shouldBe.Value - timeIs.Value);
            }
        }

        public TimeStamp GetCurrentClockTime() {
            var ticks = OsClock.GetTicks();
            var ms = OsClock.TicksToMilliseconds(ticks);
            return new TimeStamp(ms).AddValue(CorrectionFactor);
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
        private RecordSetWriter _setWriter;

        public event EventHandler<Status> StatusChanged;

        public Graph() {
            Nodes.CollectionChanged += Nodes_CollectionChanged;
            OsClock = new PerformanceCounter();
            FillDevices();
        }

        private void FillDevices() {
            foreach (var factory in DeviceLibrary.DeviceFactory2.Instance.Factories) {
                _devices.AddRange(factory.CreateDevices());
            }
        }

        public IDevicePort GetOwnedPortFromPort(IDevicePort port) {
            foreach (var dev in _devices) {
                if (dev.UniqueId.Equals(port.Owner.UniqueId)) {
                    foreach (var p in dev.Ports) {
                        if (p.UniqueId.Equals(port.UniqueId)) {
                            if (p.Name == port.Name && p.Owner.Name == port.Owner.Name) {
                                return p;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public bool AddRecord(Recording rec) {
            lock (_writerLock) {
                if (_setWriter == null) {
                    var file = "index.lst";
                    var path = System.IO.Path.Combine(BaseDirectory, WorkingDirectory, file);

                    try {
                        _setWriter = new RecordSetWriter(path);
                    } catch (Exception) {
                        return false;
                    }
                }

                _setWriter.AddRecord(rec);
                return true;
            }
        }

        private void Nodes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            //switch (e.Action) {
            //    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
            //        foreach (var node in e.NewItems.OfType<Node>()) {
            //            if (node.Parent != null) throw new InvalidOperationException("Node can't have more than one parent graph");
            //            node.Parent = this;

            //            if (State == Status.Running) {
            //                node.PrepareProcessing();
            //                node.StartProcessing();
            //            }
            //        }
            //        break;

            //    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
            //        foreach (var node in e.OldItems.OfType<Node>()) {
            //            if (node.Parent != this) throw new InvalidOperationException("Node's parent graph does not match this graph instance");

            //            if (State == Status.Running) {
            //                node.StopProcessing();
            //            }

            //            node.Parent = null;
            //        }
            //        break;

            //}
        }

        public bool Run() {
            if (State == Status.Running) return true;

            var workingDirRel = WorkingDirectoryMask.Replace("%count%", RunCount.ToString());
            WorkingDirectory  = System.IO.Path.Combine(BaseDirectory, workingDirRel);

            while (System.IO.Directory.Exists(WorkingDirectory)) {
                RunCount++;
                workingDirRel    = WorkingDirectoryMask.Replace("%count%", RunCount.ToString());
                WorkingDirectory = System.IO.Path.Combine(BaseDirectory, workingDirRel);
            }

            try {
                if (!Nodes.All(n => n.PrepareProcessing())) return false;
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine($"Graph.Run Exception: {e}");
                return false;
            }

            OsClock.Start();
            Nodes.ForEach(n => n.StartProcessing());

            State = Status.Running;

            foreach (var dev in _devices) {
                if (HasActivePorts(dev) && !dev.StartSampling()) {
                    Stop();
                    return false;
                }
            }

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

        public void Stop() {
            lock (this) {
                if (State == Status.Stopped) return;

                Func<Node, bool> HasNoOutputConnections = (n) => n.OutputPorts.Count(p => p.Connections.Count > 0) == 0;
                Func<Node, bool> HasNoInputConnections = (n) => n.InputPorts.Count(p => p.Connection != null) == 0;

                if (State == Status.Stopped) return;

                Devices.Where(HasActivePorts).ForEach(n => n.StopSampling());

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
                _setWriter = null;
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

            //if (inp is DataInputPort) {
            //    ((DataInputPort)inp).Samplerate = ((DataOutputPort)outp).Samplerate;
            //} else if (inp is FFTInputPort) {
            //    ((FFTInputPort)inp).Samplerate = ((FFTOutputPort)outp).Samplerate;
            //    ((FFTInputPort)inp).FFTSize = ((FFTOutputPort)outp).FFTSize;
            //}
        }

        public void Disconnect(InputPort inp) {
            if (inp.Connection != null) {
                inp.Connection.Connections.Remove(inp);
                inp.Connection = null;
            }
        }


        // TODO: THIS IS VERY DANGEROUS!
        //       YOU HAVE TO BE SURE THERE IS NO MORE PROCESSING GOING ON
        //       OTHERWISE THIS WILL DEADLOOP. THERE MUST BE A GUARANTEE
        //       THAT THE POOL WILL IDLE!!
        public void LoadState(Dictionary<Node, NodeState> states) {
            // wait for processing queue to become empty
            while (CustomPool.Forker.CountRunning() > 0) {
                System.Threading.Thread.Sleep(0);
            }

            // ok, load states
            foreach (var node in Nodes) {
                if (states.ContainsKey(node)) {
                    node.LoadState(states[node]);
                }
            }
        }


        // TODO: THIS IS VERY DANGEROUS!
        //       YOU HAVE TO BE SURE THERE IS NO MORE PROCESSING GOING ON
        //       OTHERWISE THIS WILL DEADLOOP. THERE MUST BE A GUARANTEE
        //       THAT THE POOL WILL IDLE!!
        public Dictionary<Node, NodeState> SaveState() {
            // wait for processing queue to become empty
            while (CustomPool.Forker.CountRunning() > 0) {
                System.Threading.Thread.Sleep(0);
            }

            // ok, save states
            var states = new Dictionary<Node, NodeState>();
            foreach (var node in Nodes) {
                var state = node.SaveState();
                if (state != null) {
                    states.Add(node, state);
                }
            }

            return states;
        }

        //--------------------------------------------------------------------------------

        private bool HasActivePorts(IDevice dev) {
            return dev.Ports.Any(p => p.Status == DevicePortStatus.Active);
        }

    }

}
