using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DeviceLibrary {
    class NidaqSessionAnalogOut : INidaqSessionTask {

        private const int WriteTimeoutMs = 2000;

        public NidaqSessionAnalogOut(NidaqSingleton.Device dev, NidaqSession parent) {
            Parent = parent;
            Device = dev;
        }

        private readonly List<MetricAnalogOutput> _nodes = new List<MetricAnalogOutput>();
        private NationalInstruments.DAQmx.AnalogMultiChannelWriter _writer;
        private NationalInstruments.DAQmx.Task _task;

        public int ClockRate { get; set; }
        public int TaskHandle { get; private set; }
        public int SamplesPerChannel { get; private set; }

        public int BufferLengthMs { get; set; }
        public int PrebufferLengthMs { get; set; }

        public NidaqSession Parent { get; }
        public NidaqSingleton.Device Device { get; }
        public SessionTaskState State { get; private set; }

        public IReadOnlyList<INidaqMetric> Nodes => _nodes;

        private double[,] _bufferData;
        private double[,] _usableData;
        private int[] _channelCount;

        private IAsyncResult _writeResult;
        private bool _first;

        /// <summary>
        /// Write data to the device
        /// </summary>
        /// <param name="channel">index of channel to write to (ordered same as </param>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns>number of samples written</returns>
        /// <exception cref="NationalInstruments.DAQmx.DaqException">Device error</exception>
        public int Write(int channel, double[] data, int offset, int size) {
            // Exceptions should be dealt with outside, meaning in NidaqSession

            lock (this) {
                var spaceLeft = _bufferData.GetLength(1) - _channelCount[channel];
                size = Math.Min(spaceLeft, size);

                // write as much of the data as possible to the local buffer
                Buffer.BlockCopy(
                    data,
                    offset * sizeof(double),
                    _bufferData,
                    (channel * _bufferData.GetLength(1) + _channelCount[channel]) * sizeof(double),
                    size * sizeof(double)
                );
                _channelCount[channel] += size;

                // check if this will be the first write to the device.
                // If so, the amount of data has to surpass the defined prebuffer size.
                var prebufferSamples = (int)(PrebufferLengthMs * (long)ClockRate / 1000);
                if (_first && !_channelCount.All(x => x >= (SamplesPerChannel + prebufferSamples))) return size;

                if (_channelCount.All(x => x >= SamplesPerChannel)) {
                    // enough data collected to write to the device.
                    // Wait for the previous write.
                    if (_writeResult != null) {
                        _writeResult.AsyncWaitHandle.WaitOne();
                        _writer.EndWrite(_writeResult);
                    }

                    // copy parts of local channel buffers to a buffer of the size specified in the initialization of the nidaq task
                    for (int i = 0; i < _channelCount.Length; i++) {
                        Buffer.BlockCopy(
                            _bufferData,
                            i * _bufferData.GetLength(1) * sizeof(double),
                            _usableData,
                            i * _usableData.GetLength(1) * sizeof(double),
                            SamplesPerChannel * sizeof(double)
                        );
                    }

                    // start asynchronous write to device
                    _writeResult = _writer.BeginWriteMultiSample(true, _usableData, null, null);
                    System.Diagnostics.Debug.WriteLine("Write new data to dev. Length: " + _usableData.GetLength(1));

                    // copy unwritten parts of local channel buffers to front of buffers
                    for (int i = 0; i < _channelCount.Length; i++) {
                        Buffer.BlockCopy(
                            _bufferData,
                            _bufferData.GetLength(1) * i * sizeof(double),
                            _bufferData,
                            (_bufferData.GetLength(1) * i + SamplesPerChannel) * sizeof(double),
                            (_channelCount[i] - SamplesPerChannel) * sizeof(double)
                        );
                        _channelCount[i] -= SamplesPerChannel;
                    }

                    _first = false;
                }
            }

            return size;
        }

        /// <summary>
        /// Creates a new DAQmx task and adds channels to it
        /// </summary>
        /// <param name="nodes">Graph nodes of type MetricAnalogOutput</param>
        /// <exception cref="InvalidCastException">At least one element in <paramref name="nodes"/> not of type MetricAnalogOutput</exception>
        /// <exception cref="InvalidOperationException">At least one element in <paramref name="nodes"/> not connected to the instance's specified device</exception>
        /// <exception cref="NidaqException">Task or channel could not be created</exception>
        public void CreateTask(IEnumerable<INidaqMetric> nodes) {
            if (!nodes.All(n => n is MetricAnalogOutput)) {
                throw new InvalidCastException("all passed nodes must be of type MetricAnalogOutput");
            }

            if (!nodes.All(n => n.Channel.Device == Device)) {
                throw new InvalidOperationException("not all passed nodes are connected to device " + Device.Name);
            }

            ClockRate = ((MetricAnalogOutput)nodes.First()).Samplerate;

            if (!nodes.All(n => ((MetricAnalogOutput)n).Samplerate == ClockRate)) {
                ClockRate = 0;
                SamplesPerChannel = 0;
                throw new InvalidOperationException("NIDAQ: not all analog output nodes in graph have the same samplerate");
            }

            foreach (var n in nodes.OfType<MetricAnalogOutput>()) {
                n.Samplerate = ClockRate;
            }

            var nidaqBufferSizePerChannel = (int)(BufferLengthMs * (long)ClockRate / 1000);
            var localBufferSizePerChannel = (int)(2 * (BufferLengthMs + PrebufferLengthMs) * (long)ClockRate / 1000);

            SamplesPerChannel = nidaqBufferSizePerChannel;

            // ------------------------------
            // Configure DAQMX Task

            _task = new NationalInstruments.DAQmx.Task();
            foreach (var output in nodes.OfType<MetricAnalogOutput>()) {
                output.ChannelNumber = _nodes.Count;

                _task.AOChannels.CreateVoltageChannel(
                    output.Channel.Path, 
                    string.Empty, 
                    output.VMin, 
                    output.VMax, 
                    NationalInstruments.DAQmx.AOVoltageUnits.Volts
                );

                _nodes.Add(output);
            }

            _task.Timing.ConfigureSampleClock(
                "", 
                ClockRate, 
                NationalInstruments.DAQmx.SampleClockActiveEdge.Rising, 
                NationalInstruments.DAQmx.SampleQuantityMode.ContinuousSamples, 
                SamplesPerChannel
            );

            try {
                _task.Stream.ConfigureOutputBuffer(SamplesPerChannel);
            } catch (NationalInstruments.DAQmx.DaqException e) {
                throw new NidaqException(e.Error);
            }

            _task.Stream.Timeout = WriteTimeoutMs;
            _task.Stream.WriteRegenerationMode = NationalInstruments.DAQmx.WriteRegenerationMode.DoNotAllowRegeneration;

            try {
                _task.Control(NationalInstruments.DAQmx.TaskAction.Verify);
            } catch (NationalInstruments.DAQmx.DaqException e) {
                throw new NidaqException(e.Error);
            }

            _task.Stream.Buffer.OutputBufferSize = SamplesPerChannel;

            _writer = new NationalInstruments.DAQmx.AnalogMultiChannelWriter(_task.Stream);

            TaskHandle = _task.GetHashCode();

            _usableData = new double[_nodes.Count, SamplesPerChannel];
            _bufferData = new double[_nodes.Count, localBufferSizePerChannel];
            _channelCount = new int[_nodes.Count];

            State = SessionTaskState.Stopped;
            _first = true;
        }

        public void DestroyTask() {
            _task.Dispose();
            TaskHandle = 0;
            SamplesPerChannel = 0;
            _nodes.Clear();
            State = SessionTaskState.None;
        }

        public void Start() {
            if (TaskHandle == 0) throw new InvalidOperationException("Task not yet created. First create a task");
            _task.Start();
            State = SessionTaskState.Running;
        }

        public void Stop() {
            if (TaskHandle == 0) throw new InvalidOperationException("Task not yet created. First create a task");
            _task.Stop();

            // clear old data
            for (int i = 0; i < _nodes.Count; i++) {
                for (int j = 0; j < _usableData.GetLength(1); j++) {
                    _usableData[i, j] = 0;
                }

                for (int j = 0; j < _bufferData.GetLength(1); j++) {
                    _bufferData[i, j] = 0;
                }

                _channelCount[i] = 0;
            }

            State = SessionTaskState.Stopped;
        }

        public void Serialize(XmlWriter writer) {
            writer.WriteStartElement("task");
            writer.WriteAttributeString("type", "ao");
            writer.WriteAttributeString("dev", Device.Name);
            writer.WriteAttributeString("bufferlen", BufferLengthMs.ToString());
            writer.WriteAttributeString("prebufferlen", PrebufferLengthMs.ToString());
            writer.WriteEndElement();
        }

        public void LoadFactorySettings() {
            if (!NidaqSingleton.Instance.FactorySettings.ContainsKey(Parent.SessionGraph)) return;
            var factorySettings = NidaqSingleton.Instance.FactorySettings[Parent.SessionGraph];

            var taskSettings = factorySettings.SelectNodes("tasks/task");
            for (int i = 0; i < taskSettings.Count; i++) {
                var taskType = taskSettings[i].Attributes.GetNamedItem("type")?.Value ?? "";
                if (taskType == "ao") {
                    BufferLengthMs = int.Parse(taskSettings[i].Attributes.GetNamedItem("bufferlen")?.Value ?? "0");
                    PrebufferLengthMs = int.Parse(taskSettings[i].Attributes.GetNamedItem("prebufferlen")?.Value ?? "0");
                    break;
                }
            }
        }

    }

}
