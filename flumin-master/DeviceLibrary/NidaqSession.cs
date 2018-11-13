using NodeSystemLib2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DeviceLibrary {

    class NidaqSession {

        public enum State {
            Stopped,
            Running
        }

        public enum NodeState {
            Stopped,
            Prepared,
            Running,
            Suspended
        }

        private Graph _graph;
        private readonly List<Node> _nodes = new List<Node>();
        private readonly Dictionary<Node, NodeState> _nodeStates = new Dictionary<Node, NodeState>();
        private readonly Dictionary<NidaqSingleton.Device, IntPtr> _inputAnalogDataBuffers = new Dictionary<NidaqSingleton.Device, IntPtr>();
        private readonly Dictionary<NidaqSingleton.Device, IntPtr> _inputDigitalDataBuffers = new Dictionary<NidaqSingleton.Device, IntPtr>();
        private volatile bool _stopThread;
        private Thread _pollThread;

        public State CurrentState { get; private set; }

        public Graph SessionGraph => _graph;

        private readonly List<INidaqSessionTask> _tasks = new List<INidaqSessionTask>();

        public NidaqSession() {
            foreach (var dev in NidaqSingleton.Instance.Devices) {
                _tasks.Add(new NidaqSessionAnalogIn(dev, this));
                _tasks.Add(new NidaqSessionAnalogOut(dev, this));
                _tasks.Add(new NidaqSessionDigitalIn(dev, this));
                _tasks.Add(new NidaqSessionDigitalOut(dev, this));
            }
        }
        
        public T GetTask<T>(NidaqSingleton.Device dev) where T : INidaqSessionTask {
            return (T)_tasks.FirstOrDefault(task => task is T && task.Device == dev);
        }

        public void RegisterNode(Node n) {
            if (CurrentState == State.Running) {
                throw new InvalidOperationException("Can't add node to session if it is currently running");
            }

            if (!(n is INidaqMetric)) {
                throw new InvalidCastException("Presented node is not a Nidaq metric");
            }

            if (n.Parent != _graph) {
                if (_graph == null) {
                    _graph = n.Parent;
                    LoadSessionSettingsFromFactorySettings();
                } else {
                    throw new InvalidOperationException("This session is using another graph");
                }
            }

            _nodes.Add(n);
            _nodeStates.Add(n, NodeState.Stopped);
        }

        private void LoadSessionSettingsFromFactorySettings() {
            foreach (var task in _tasks) {
                task.LoadFactorySettings();
            }
        }

        public void UnregisterNode(Node n) {
            if (CurrentState == State.Running) {
                throw new InvalidOperationException("Can't remove node from session if it is currently running");
            }

            _nodes.Remove(n);
            _nodeStates.Remove(n);
        }

        public void SetNodeState(Node n, NodeState s) {
            _nodeStates[n] = s;
            ShiftGlobalState();
        }

        private void ShiftGlobalState() {
            if (CurrentState == State.Stopped) {
                if (_nodeStates.Values.Any(v => v == NodeState.Prepared)) {
                    StartDevices();
                }
            } else if (CurrentState == State.Running) {
                if (_nodeStates.Values.Any(v => v == NodeState.Stopped)) {
                    StopDevices();
                }
            }
        }

        private void StartDevices() {
            var analogInputGroups = _nodes.OfType<MetricAnalogInput>().GroupBy(inp => inp.Device);
            var analogOutputGroups = _nodes.OfType<MetricAnalogOutput>().GroupBy(inp => inp.Device);
            var digitalInputGroups = _nodes.OfType<MetricDigitalInput>().GroupBy(inp => inp.Device);
            var digitalOutputGroups = _nodes.OfType<MetricDigitalOutput>().GroupBy(inp => inp.Device);

            foreach (var grp in analogInputGroups) {
                var task = GetTask<NidaqSessionAnalogIn>(dev: grp.Key);

                try {
                    task.CreateTask(grp);
                    _inputAnalogDataBuffers.Add(task.Device, Marshal.AllocHGlobal(sizeof(double) * task.SamplesPerChannel * task.Nodes.Count));
                    foreach (var node in grp) node.SetBufferSize(task.SamplesPerChannel * task.Nodes.Count);

                } catch (NidaqException) {
                    CleanTasksAndMemory();
                    throw;

                } catch (OutOfMemoryException) {
                    CleanTasksAndMemory();
                    throw;

                } catch (Exception) {
                    CleanTasksAndMemory();
                    throw;

                }
            }

            foreach (var grp in digitalInputGroups) {
                var task = GetTask<NidaqSessionDigitalIn>(dev: grp.Key);

                try {
                    task.CreateTask(grp);
                    _inputDigitalDataBuffers.Add(task.Device, Marshal.AllocHGlobal(sizeof(uint) * task.SamplesPerChannel * task.Nodes.Count));
                    foreach (var node in grp) node.SetBufferSize(task.SamplesPerChannel * task.Nodes.Count);

                } catch (NidaqException) {
                    CleanTasksAndMemory();
                    throw;

                } catch (OutOfMemoryException) {
                    CleanTasksAndMemory();
                    throw;

                } catch (Exception) {
                    CleanTasksAndMemory();
                    throw;

                }
            }

            foreach (var grp in analogOutputGroups) {
                var task = GetTask<NidaqSessionAnalogOut>(dev: grp.Key);

                try {
                    task.CreateTask(grp);

                } catch (NidaqException) {
                    CleanTasksAndMemory();
                    throw;

                } catch (OutOfMemoryException) {
                    CleanTasksAndMemory();
                    throw;

                } catch (Exception) {
                    CleanTasksAndMemory();
                    throw;

                }
            }

            foreach (var grp in digitalOutputGroups) {
                var task = GetTask<NidaqSessionDigitalOut>(dev: grp.Key);

                try {
                    task.CreateTask(grp);

                } catch (NidaqException) {
                    CleanTasksAndMemory();
                    throw;

                } catch (OutOfMemoryException) {
                    CleanTasksAndMemory();
                    throw;

                } catch (Exception) {
                    CleanTasksAndMemory();
                    throw;

                }
            }

            if (analogInputGroups.Any() || digitalInputGroups.Any()) {
                // start all input tasks. output tasks will start automatically on the first write
                try {
                    foreach (var task in _tasks.Where(t => t is NidaqSessionAnalogIn && t.Nodes.Count > 0)) task.Start();
                    foreach (var task in _tasks.Where(t => t is NidaqSessionDigitalIn && t.Nodes.Count > 0)) task.Start();
                } catch (NidaqException) {
                    CleanTasksAndMemory();
                    throw;
                }

                _stopThread = false;
                _pollThread = new Thread(EveryNCallback);
                _pollThread.Start();
            }

            CurrentState = State.Running;
        }

        private bool StopDevices() {
            _stopThread = true;
            if (_pollThread != null && Thread.CurrentThread != _pollThread) {
                _pollThread.Join();
            }

            CleanTasksAndMemory();

            CurrentState = State.Stopped;

            return true;
        }
        
        private void CleanTasksAndMemory() {
            foreach (var task in _tasks.Where(t => t.Nodes.Count > 0)) {
                if (task.State == SessionTaskState.Running) task.Stop();
                task.DestroyTask();
            }

            foreach (var buf in _inputAnalogDataBuffers) {
                Marshal.FreeHGlobal(buf.Value);
            }

            foreach (var buf in _inputDigitalDataBuffers) {
                Marshal.FreeHGlobal(buf.Value);
            }

            _inputAnalogDataBuffers.Clear();
            _inputDigitalDataBuffers.Clear();

        }

        private void EveryNCallback() {
            var dict = new Dictionary<INidaqSessionTask, NILoop.HandleInfo>();

            foreach (var task in _tasks.OfType<NidaqSessionAnalogIn>().Where(t => t.Nodes.Count > 0)) {
                dict.Add(
                    task,
                    new NILoop.HandleInfo {
                        handle           = task.TaskHandle,
                        buffer_size      = task.SamplesPerChannel * task.Nodes.Count,
                        samples_per_chan = task.SamplesPerChannel,
                        type             = (int)NILoop.TASK_TYPE.TASK_TYPE_ANALOG_INPUT,
                        result           = 0,
                        mutex_buffers    = IntPtr.Zero
                    }
                );
            }

            foreach (var task in _tasks.OfType<NidaqSessionDigitalIn>().Where(t => t.Nodes.Count > 0)) {
                dict.Add(
                    task,
                    new NILoop.HandleInfo {
                        handle           = task.TaskHandle,
                        buffer_size      = task.SamplesPerChannel * task.Nodes.Count,
                        samples_per_chan = task.SamplesPerChannel,
                        type             = (int)NILoop.TASK_TYPE.TASK_TYPE_DIGITAL_INPUT,
                        result           = 0,
                        mutex_buffers    = IntPtr.Zero
                    }
                );
            }

            var hPoll = NILoop.start_polling(dict.Values.ToArray(), dict.Count);

            while (!_stopThread) {
                foreach (var task in dict) {
                    if (task.Key is NidaqSessionAnalogIn) {
                        ProcessInputData(task.Key.Nodes, task.Value, _inputAnalogDataBuffers[task.Key.Device], sizeof(double), hPoll);
                    } else if (task.Key is NidaqSessionDigitalIn) {
                        ProcessInputData(task.Key.Nodes, task.Value, _inputDigitalDataBuffers[task.Key.Device], sizeof(uint), hPoll);
                    }
                }
            }

            NILoop.stop_polling(hPoll);
        }

        private void ProcessInputData(IReadOnlyList<INidaqMetric> listeningPorts, NILoop.HandleInfo taskInfo, IntPtr buffer, int sample_size_bytes, int hPoll) {
            var result = NILoop.read_buffer(hPoll, taskInfo.handle, buffer, sample_size_bytes * (taskInfo.samples_per_chan * listeningPorts.Count));
            switch (result) {
                case 3:
                case 1:
                    // task not found
                    SessionGraph.AsyncEmergencyStop(null);
                    _stopThread = true;
                    break;

                case 2:
                    // queue empty, ignore
                    Thread.Sleep(1);
                    break;

                case 0:
                    var channelData = buffer;

                    foreach (var port in listeningPorts) {
                        ((IMetricInput)port).DistributeData(channelData, taskInfo.samples_per_chan);
                        channelData = IntPtr.Add(channelData, sample_size_bytes * taskInfo.samples_per_chan);
                    }

                    break;

                default:
                    // read error
                    System.Diagnostics.Debug.WriteLine(NidaQmxHelper.GetError(result));
                    SessionGraph.AsyncEmergencyStop(null);
                    _stopThread = true;
                    break;

            }
        }

        public void DigitalWrite(NidaqSingleton.Device dev, int channelNumber, double[] data, int offset, int samples) {
            var task = GetTask<NidaqSessionDigitalOut>(dev);

            if (task == null) {
                System.Diagnostics.Debug.WriteLine("Nidaq: Can't query task from session. Result null");
                SessionGraph.AsyncEmergencyStop(null);
                return;
            }

            try {
                int written = task.Write(channelNumber, data, offset, samples);
                if (written != samples) {
                    System.Diagnostics.Debug.WriteLine("Didn't write " + (samples - written));
                }
            } catch (NationalInstruments.DAQmx.DaqException e) {
                System.Diagnostics.Debug.WriteLine("Nidaq: Error while writing: " + e.Message);
                SessionGraph.AsyncEmergencyStop(null);
            }
        }

        public void AnalogWrite(NidaqSingleton.Device dev, int channelNumber, double[] data, int offset, int samples) {
            var task = GetTask<NidaqSessionAnalogOut>(dev);

            if (task == null) {
                System.Diagnostics.Debug.WriteLine("Nidaq: Can't query task from session. Result null");
                SessionGraph.AsyncEmergencyStop(null);
                return;
            }

            try {
                int written = task.Write(channelNumber, data, offset, samples);
                if (written != samples) {
                    System.Diagnostics.Debug.WriteLine("Didn't write " + (samples - written));
                }
            } catch (NationalInstruments.DAQmx.DaqException e) {
                System.Diagnostics.Debug.WriteLine("Nidaq: Error while writing: " + e.Message);
                SessionGraph.AsyncEmergencyStop(null);
            }
        }
        
        public void SaveInternalState(XmlWriter writer) {
            writer.WriteStartElement("tasks");
            foreach (var task in _tasks) {
                task.Serialize(writer);
            }
            writer.WriteEndElement();
        }

    }

}
