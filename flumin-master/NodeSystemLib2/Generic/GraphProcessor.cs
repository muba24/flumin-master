using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NodeSystemLib2 {

    class GraphProcessor {

        private static readonly ILog logger = LogManager.GetLogger(typeof(GraphProcessor));

        private Barrier _pauseBarrier;
        private Barrier _resumeBarrier;
        private AutoResetEvent _pauseEvent;
        private AutoResetEvent _resumeEvent;
        private volatile State _state;

        private BlockingCollection<Node> _nodeCollection;
        private readonly Thread[] _pool;
        private readonly Graph _g;

        private CancellationTokenSource _cancelSource;
        private CancellationToken _cancelToken;
        private volatile bool _asyncStopped;

        public enum State {
            Stopped, Running, Paused
        }

        public GraphProcessor(Graph g) {
            _pool = new Thread[Environment.ProcessorCount];
            _g = g;
        }

        public State Status {
            get { return _state; }
            private set { _state = value; }
        }

        public void Start() {
            if (Status == State.Running) return;
            Status = State.Running;

            logger.Info("Graph prcessor initializing...");

            CleanupAsyncStop();

            _nodeCollection = new BlockingCollection<Node>();

            foreach (var node in _g.Nodes) {
                _nodeCollection.Add(node);
            }

            logger.Info($"Setting up thread pool... number of threads: {_pool.Length}");

            _resumeEvent = new AutoResetEvent(false);
            _pauseEvent = new AutoResetEvent(false);
            _pauseBarrier = new Barrier(_pool.Length, (b) => {
                logger.Info($"Synchronization barrier phase for pausing complete");
                _pauseEvent.Set();
            });

            _resumeBarrier = new Barrier(_pool.Length, (b) => {
                logger.Info($"Synchronization barrier phase for resuming complete");
                _resumeEvent.Set();
            });

            _cancelSource = new CancellationTokenSource();
            _cancelToken = _cancelSource.Token;
            for (int i = 0; i < _pool.Length; i++) {
                _pool[i] = new Thread(Worker);
                _pool[i].Start();
            }
        }

        public void AsyncEmergencyStop() {
            if (Status == State.Stopped) return;
            Status = State.Stopped;
            _asyncStopped = true;
            _cancelSource.Cancel();
        }

        public void CleanupAsyncStop() {
            if (_asyncStopped) {
                for (int i = 0; i < _pool.Length; i++) {
                    _pool[i]?.Join();
                }
                _asyncStopped = false;
            }
        }

        public void Stop() {
            if (Status == State.Stopped) return;

            Status = State.Stopped;

            if (!_asyncStopped) {
                _cancelSource.Cancel();
            }

            for (int i = 0; i < _pool.Length; i++) {
                _pool[i].Join();
            }

            _asyncStopped = false;
        }

        public void Pause() {
            if (Status != State.Running) return;

            logger.Info($"Pausing...");

            _resumeEvent.Reset();
            _pauseEvent.Reset();

            Status = State.Paused;
            _pauseEvent.WaitOne();
        }

        public void Resume() {
            if (Status != State.Paused) return;

            Status = State.Running;
            _resumeEvent.WaitOne();

            logger.Info($"Resuming");
        }

        private void Worker() {
            try {
                foreach (var node in _nodeCollection.GetConsumingEnumerable(_cancelToken)) {
                    var processed = node.CanProcess | node.CanTransfer;

                    try {
                        if (node.CanTransfer) node.Transfer();
                        if (node.CanProcess) node.Process();
                    } catch (Exception e) {
                        _nodeCollection.CompleteAdding();
                        _g.AsyncEmergencyStop(node);
                        logger.Error($"Error while processing node with name {node.Name}: {e}");
                        _g.Context.Notify(new Generic.GraphNotification(node, Generic.GraphNotification.NotificationType.Error, e.Message));
                    }

                    try {
                        _nodeCollection.Add(node);
                    } catch (InvalidOperationException) {
                        // CompleteProcessing called, workers should run dry
                    }

                    if (Status == State.Paused) {
                        _pauseBarrier.SignalAndWait(_cancelToken);
                        while (Status == State.Paused && !_cancelToken.IsCancellationRequested) Thread.Sleep(1);
                        _resumeBarrier.SignalAndWait(_cancelToken);
                    } else {
                        if (!processed) Thread.Sleep(1);
                    }
                }
            } catch (OperationCanceledException) {
                logger.Debug("Worker canceled");
                return;
            }
        }

    }

}
