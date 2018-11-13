using log4net;
using NodeSystemLib2.Generic;
using NodeSystemLib2.Generic.NodeAttributes;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2 {

    public class Graph : IAttributable {

        private static readonly ILog logger = LogManager.GetLogger(typeof(Graph));

        private readonly Dictionary<string, NodeAttribute> _attributes
            = new Dictionary<string, NodeAttribute>(StringComparer.InvariantCultureIgnoreCase);

        private readonly List<Node> _nodes = new List<Node>();
        private readonly GraphProcessor _proc;
        private readonly object _statusLock = new object();

        private readonly object _stopLock = new object();
        private volatile bool _stopping;
        private bool _asyncStopped;

        private IGraphContext _ctx = new EmptyGraphContext();
        private AttributeValueString _attrWorkingDirMask;
        private AttributeValueString _attrFileMask;

        // TODO: Probably not atomic access
        private double CorrectionFactor = 0;

        public enum State {
            Running,
            Stopped,
            Paused
        }

        private State _state = State.Stopped;
        private DateTime _startedAt;

        public IGraphContext Context {
            get { return _ctx; }
            set {
                if (_ctx != null) {
                    _ctx.PropertyChanged -= _ctx_PropertyChanged;
                }
                _ctx = value ?? new EmptyGraphContext();
                _ctx.PropertyChanged += _ctx_PropertyChanged;
                UpdateAttributes();
            }
        }

        private void _ctx_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(_ctx.WorkingDirectoryMask)) {
                _attrWorkingDirMask.SetSilent(_ctx.WorkingDirectoryMask);
            } else if (e.PropertyName == nameof(_ctx.FileMask)) {
                _attrFileMask.SetSilent(_ctx.FileMask);
            }
        }

        private void UpdateAttributes() {
            _attrWorkingDirMask.Set(_ctx.WorkingDirectoryMask);
            _attrFileMask.Set(_ctx.FileMask);
        }

        public class StatusChanagedEventArgs : EventArgs {

            public State NewState { get; }

            public StatusChanagedEventArgs(State st) {
                NewState = st;
            }

        }

        public class ConnectedEventArgs : EventArgs {

            public InputPort Port { get; }

            public ConnectedEventArgs(InputPort p) {
                Port = p;
            }

        }

        public class DisconnectedEventArgs : EventArgs {

            public InputPort Port { get; }

            public DisconnectedEventArgs(InputPort p) {
                Port = p;
            }

        }

        public event EventHandler<StatusChanagedEventArgs> StatusChanged;
        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        public Graph() {
            OsClock = new PerformanceCounter();
            _proc = new GraphProcessor(this);

            _attrWorkingDirMask = new AttributeValueString(this, "WorkingDirMask");
            _attrWorkingDirMask.Changed += (o, e) => _ctx.WorkingDirectoryMask = _attrWorkingDirMask.TypedGet();

            _attrFileMask = new AttributeValueString(this, "FileMask");
            _attrFileMask.Changed += (o, e) => _ctx.FileMask = _attrFileMask.TypedGet();
        }

        public IReadOnlyList<Node> Nodes => _nodes;

        public State GraphState => _state;

        private PerformanceCounter OsClock { get; }


        private void ChangeState(State st) {
            _state = st;
            StatusChanged?.Invoke(this, new StatusChanagedEventArgs(st));
        }

        public void SynchronizeClock(TimeStamp shouldBe) {
            var now = GetCurrentClockTime();
            CorrectionFactor += (shouldBe.AsSeconds() - now.AsSeconds());
        }

        public TimeStamp GetCurrentClockTime() {
            var ticks = OsClock.GetTicks();
            var secs = OsClock.TicksToSeconds(ticks);
            return new TimeStamp(secs).Increment(CorrectionFactor);
        }

        /// <summary>
        /// First tells all nodes to prepare for the start,
        /// then calls start for all the nodes and then start the graph processor.
        /// </summary>
        /// <remarks>
        /// In future the order of starting the processor and informing the nodes
        /// about the start may change!
        /// </remarks>
        /// <exception cref="Generic.NodeException">Node can't prepare for processing</exception>
        public void Start() {
            if (_state == State.Running) {
                return;
            }

            CleanupAsyncStop();

            logger.Info($"Starting graph. Node count: {Nodes.Count}. Preparing...");

            var nodeCounter = 0;
            try {
                for (; nodeCounter < Nodes.Count; nodeCounter++) {
                    Nodes[nodeCounter].PrepareProcessing();
                }
            } catch (Exception e) {
                var msg = $"Exception while preparing node #{nodeCounter}, name: {Nodes[nodeCounter].Name}. Exception: {e}. Stopping already prepared nodes...";
                logger.Error(msg);
                Context.Notify(new GraphNotification(GraphNotification.NotificationType.Error, msg));
                var ex = new Generic.NodeException(Nodes[nodeCounter], e);
                for (; nodeCounter >= 0; nodeCounter--) {
                    Nodes[nodeCounter].StopProcessing();
                }
                throw ex;
            }

            Context.NewSession();

            ChangeState(State.Running);

            logger.Info($"All nodes prepared. Tell nodes processing is going to start...");

            foreach (var node in Nodes) {
                node.StartProcessing();
            }

            logger.Info($"Starting graph processor...");

            OsClock.Start();
            _proc.Start();

            _startedAt = DateTime.Now;
        }


        /// <summary>
        /// Cancels the graph processor and set its state to stopped.
        /// This is meant to be called by nodes incase of an unrecoverable error.
        /// Nodes won't be flushed.
        /// </summary>
        /// <param name="caller">Node calling the stop. Can be null.</param>
        /// <remarks>
        /// As the graph can not stop until the calling node returns control to the graph processor,
        /// there is no information about when that is going to happen. Therefore the nodes can't yet be
        /// told to stop as they could still be triggered by the graph processor so one can't risk freeing ressources yet.
        /// The information about the status change to stopped should be routed to the main thread which should
        /// call <see cref="CleanupAsyncStop"/> immediately.
        /// </remarks>
        public void AsyncEmergencyStop(Node caller) {
            // If the graph is in the process of stopping and a node calls this method,
            // the lock on _statusLock could be potential deadlock.
            // To circumvent this another lock is needed. StopLock checks wether currently a stop is in order
            // and can return immediately.
            lock (_stopLock) {
                if (_stopping) {
                    logger.Warn($"AsyncEmergencyStop called while (emergency)stop call in progress. Caller: {caller}. Ignoring call");
                    return;
                }
                _stopping = true;
            }

            lock (_statusLock) {
                if (_state == State.Stopped) return;

                logger.Info($"Async emergency stop. Caller name: {caller?.Name ?? "null"}. Stopping graph processor");

                _proc.AsyncEmergencyStop();
                _asyncStopped = true;

                OsClock.Stop();
                ChangeState(State.Stopped);
                _stopping = false;
            }
        }

        /// <summary>
        /// Cleanup after an asynchronous stop. Tell the nodes processing stopped.
        /// </summary>
        public void CleanupAsyncStop() {
            if (_asyncStopped) {
                _proc.CleanupAsyncStop();

                logger.Info("Cleanup last emergency stop in Graph. Tell nodes to stop...");

                foreach (var node in Nodes) {
                    node.SuspendProcessing();
                    node.StopProcessing();
                }

                Context.EndSession();

                _asyncStopped = false;
            }
        }

        /// <summary>
        /// Will pause the graph processor. Returns when all graph processor threads no longer do work
        /// </summary>
        public void Pause() {
            lock (_statusLock) {
                // TODO: PAUSE PERFORMANCECLOCK
                if (_state != State.Running) return;
                _proc.Pause();
                ChangeState(State.Paused);
            }
        }

        public void Flush() {
            lock (_statusLock) {
                if (_state == State.Running) throw new InvalidOperationException("Can't flush a running graph");

                Func<Node, bool> hasNoOutputConnections = (n) => !n.OutputPorts.Any(p => p.Connections.Count > 0);

                var nodeQueue = new Queue<Node>();
                var visited = new List<Node>();

                var lastNodes = Nodes.Where(hasNoOutputConnections);

                logger.Info($"Starting to flush");

                bool reflush;
                do {
                    if (nodeQueue.Any()) throw new InvalidOperationException($"Flush queue expected to be empty, but is not! Items: {nodeQueue.Count}");
                    foreach (var node in lastNodes) nodeQueue.Enqueue(node);

                    visited.Clear();
                    reflush = false;

                    try {
                        BreadthFirstSearch(
                            nodeQueue,
                            visited,
                            (node) => {
                                if (node.FlushData() == Node.FlushState.Some) {
                                    logger.Info($"Node {node.Name} still got data");
                                    reflush = true;
                                }
                            },
                            NodesReachableByInput
                        );
                    } catch (Exception e) {
                        logger.Error($"Error while flushing: {e}. Aborting flush...");
                        break;
                    }

                    if (reflush) logger.Info($"...flush again");
                } while (reflush);
            }
        }

        /// <summary>
        /// Resumes a paused graph. Returns when all graph processor threads are up again
        /// </summary>
        public void Resume() {
            lock (_statusLock) {
                // TODO: RESUME PERFORMANCECLOCK
                if (_state != State.Paused) return;
                _proc.Resume();
                ChangeState(State.Running);
            }
        }

        /// <summary>
        /// Stops the processing and returns all nodes to stopped state.
        /// </summary>
        /// <remarks>
        /// First all nodes starting from the source nodes will be suspended using a breadth-first search.
        /// It should tell the nodes to prepare for flushing.
        /// Next starting from the sinks all nodes will be flushed, again using a breadth-first search.
        /// This last step is repeated until all nodes are done flushing data through the pipes
        /// which means that a node can be told multiple times to flush.
        /// </remarks>
        public void Stop() {
            lock (_stopLock) {
                if (_stopping) return;
                _stopping = true;
            }

            lock (_statusLock) {
                if (_state == State.Stopped) return;

                logger.Info("Stopping graph processor...");

                _proc.Stop();

                Func<Node, bool> HasNoOutputConnections = (n) => !n.OutputPorts.Any(p => p.Connections.Count > 0);
                Func<Node, bool> HasNoInputConnections = (n) => !n.InputPorts.Any(p => p.Connection != null);

                var devNodes = Nodes.Where(HasNoInputConnections);
                logger.Info($"Prepare to stop nodes. Number of source nodes found in graph: {devNodes.Count()}");

                var nodeQueue = new Queue<Node>();
                foreach (var node in devNodes) nodeQueue.Enqueue(node);

                var visited = new List<Node>();

                logger.Info($"Suspending nodes...");
                BreadthFirstSearch(
                    nodeQueue,
                    visited,
                    (node) => {
                        try {
                            node.SuspendProcessing();
                        } catch (Exception e) {
                            logger.Error($"error while suspending node {node.Name}. Exception: {e}. Continuing...");
                        }
                    },
                    NodesReachableByOutput
                );

                var lastNodes = Nodes.Where(HasNoOutputConnections);
                logger.Info($"Number of sink nodes found in graph: {lastNodes.Count()}");

                logger.Info($"Starting to flush");

                bool reflush;
                do {
                    if (nodeQueue.Any()) throw new InvalidOperationException($"Flush queue expected to be empty, but is not! Items: {nodeQueue.Count}");
                    foreach (var node in lastNodes) nodeQueue.Enqueue(node);

                    visited.Clear();
                    reflush = false;

                    try {
                        BreadthFirstSearch(
                            nodeQueue,
                            visited,
                            (node) => {
                                if (node.FlushData() == Node.FlushState.Some) {
                                    logger.Info($"Node {node.Name} still got data");
                                    reflush = true;
                                }
                            },
                            NodesReachableByInput
                        );
                    } catch (Exception e) {
                        logger.Error($"Error while flushing: {e}. Aborting flush...");
                        break;
                    }

                    if (reflush) logger.Info($"...flush again");
                } while (reflush);

                logger.Info($"Stop all nodes");

                foreach (var node in Nodes) {
                    try {
                        node.StopProcessing();
                    } catch (Exception e) {
                        logger.Error($"Error while stopping node {node.Name}. Error: {e}. Continuing...");
                    }
                }

                logger.Info($"Graph runtime: {(DateTime.Now - _startedAt)}");

                OsClock.Stop();
                ChangeState(State.Stopped);
                Context.EndSession();
                _stopping = false;
            }
        }

        private static void BreadthFirstSearch<T>(Queue<T> queue, List<T> visited, Action<T> action, Func<T, IEnumerable<T>> selector) where T : class {
            while (queue.Any()) {
                var node = queue.Dequeue();
                if (node != null && !visited.Contains(node)) {
                    action(node);
                    visited.Add(node);
                    foreach (var n in selector(node)) {
                        queue.Enqueue(n);
                    }
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
        }

        public void AddNode(Node node) {
            if (node == null) throw new ArgumentNullException();

            if (_state != State.Stopped) throw new InvalidOperationException("Graph can't be changed when running");

            _nodes.Add(node);
        }

        public void RemoveNode(Node node) {
            if (node == null) throw new ArgumentNullException();

            if (_state != State.Stopped) throw new InvalidOperationException("Graph can't be changed when running");
            if (!_nodes.Contains(node)) throw new ArgumentException();

            foreach (var input in node.InputPorts) {
                if (input.Connection != null) {
                    Disconnect(input.Connection, input);
                }
            }

            foreach (var output in node.OutputPorts) {
                foreach (var input in output.Connections) {
                    Disconnect(output, input);
                }
            }

            _nodes.Remove(node);
        }

        public void Connect(OutputPort pout, InputPort pin) {
            if (pout == null) throw new ArgumentNullException();
            if (pin == null) throw new ArgumentNullException();

            if (_state != State.Stopped) throw new InvalidOperationException("Graph can't be changed when running");

            pout.AddConnection(pin);
        }

        public void Disconnect(OutputPort pout, InputPort pin) {
            if (pout == null) throw new ArgumentNullException();
            if (pin == null) throw new ArgumentNullException();
            if (!(pout.Connections.Contains(pin) && pin.Connection == pout)) throw new InvalidOperationException();

            if (_state != State.Stopped) throw new InvalidOperationException("Graph can't be changed when running");

            pout.RemoveConnection(pin);
            pin.Connection = null;
        }

        void IAttributable.AddAttribute(NodeAttribute attr) {
            _attributes.Add(attr.Name, attr);
        }

        IEnumerable<NodeAttribute> IAttributable.Attributes => _attributes.Values;

        public bool IsRunning => _state != State.Stopped;

    }

}
