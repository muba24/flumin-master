using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DeviceLibrary {
    class NidaqSessionDigitalOut : INidaqSessionTask {

        private const int WriteTimeoutMs = 2000;

        public NidaqSessionDigitalOut(NidaqSingleton.Device dev, NidaqSession parent) {
            Parent = parent;
            Device = dev;
        }

        private readonly List<MetricDigitalOutput> _nodes = new List<MetricDigitalOutput>();
        private NationalInstruments.DAQmx.DigitalMultiChannelWriter _writer;
        private NationalInstruments.DAQmx.Task _task;

        public string ClockPath { get; set; }
        public int ClockRate { get; set; }
        public int TaskHandle { get; private set; }
        public int SamplesPerChannel { get; private set; }

        public int BufferLengthMs { get; set; }
        public int PrebufferLengthMs { get; set; }

        public NidaqSession Parent { get; }
        public NidaqSingleton.Device Device { get; }
        public SessionTaskState State { get; private set; }

        public IReadOnlyList<INidaqMetric> Nodes => _nodes;

        private int[,] _bufferData;
        private int[,] _usableData;
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
                var sampleStart = _channelCount[channel];
                for (int i = 0; i < size; i++) {
                    _bufferData[channel, i + sampleStart] = data[i] > 0.5 ? 1 : 0;
                }
                //Buffer.BlockCopy(
                //    data,
                //    offset * sizeof(bool),
                //    _bufferData,
                //    (channel * _bufferData.GetLength(1) + _channelCount[channel]) * sizeof(bool),
                //    size * sizeof(bool)
                //);
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
                            i * _bufferData.GetLength(1) * sizeof(int),
                            _usableData,
                            i * _usableData.GetLength(1) * sizeof(int),
                            SamplesPerChannel * sizeof(int)
                        );
                    }

                    // start asynchronous write to device
                    _writeResult = _writer.BeginWriteMultiSamplePort(true, _usableData, null, null);

                    // copy unwritten parts of local channel buffers to front of buffers
                    for (int i = 0; i < _channelCount.Length; i++) {
                        Buffer.BlockCopy(
                            _bufferData,
                            _bufferData.GetLength(1) * i * sizeof(int),
                            _bufferData,
                            (_bufferData.GetLength(1) * i + SamplesPerChannel) * sizeof(int),
                            (_channelCount[i] - SamplesPerChannel) * sizeof(int)
                        );
                        _channelCount[i] -= SamplesPerChannel;
                    }

                    _first = false;
                }
            }

            return size;
        }

        public void CreateTask(IEnumerable<INidaqMetric> nodes) {
            if (!nodes.All(n => n is MetricDigitalOutput)) {
                throw new InvalidCastException("all passed nodes must be of type MetricDigitalOutput");
            }

            if (!nodes.All(n => n.Channel.Device == Device)) {
                throw new InvalidOperationException("not all passed nodes are connected to device " + Device.Name);
            }

            ClockRate = ((MetricDigitalOutput)nodes.First()).Samplerate;

            if (!nodes.All(n => ((MetricDigitalOutput)n).Samplerate == ClockRate)) {
                ClockRate = 0;
                SamplesPerChannel = 0;
                throw new InvalidOperationException("NIDAQ: not all digital output nodes in graph have the same samplerate");
            }

            foreach (var n in nodes.OfType<MetricDigitalOutput>()) {
                n.Samplerate = ClockRate;
            }

            var nidaqBufferSizePerChannel = (int)(BufferLengthMs * (long)ClockRate / 1000);
            var localBufferSizePerChannel = (int)(2 * (BufferLengthMs + PrebufferLengthMs) * (long)ClockRate / 1000);

            SamplesPerChannel = nidaqBufferSizePerChannel;

            // ------------------------------
            // Configure DAQMX Task

            _task = new NationalInstruments.DAQmx.Task();
            foreach (var output in nodes.OfType<MetricDigitalOutput>()) {
                output.ChannelNumber = _nodes.Count;

                _task.DOChannels.CreateChannel(
                    output.Channel.Path,
                    string.Empty,
                    NationalInstruments.DAQmx.ChannelLineGrouping.OneChannelForEachLine
                );

                _nodes.Add(output);
            }

            _task.Timing.ConfigureSampleClock(
                ClockPath,
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

            _writer = new NationalInstruments.DAQmx.DigitalMultiChannelWriter(_task.Stream);

            TaskHandle = _task.GetHashCode();

            _usableData = new int[_nodes.Count, SamplesPerChannel];
            _bufferData = new int[_nodes.Count, localBufferSizePerChannel];
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
            State = SessionTaskState.Stopped;
        }

        public void LoadFactorySettings() {
            if (!NidaqSingleton.Instance.FactorySettings.ContainsKey(Parent.SessionGraph)) return;
            var factorySettings = NidaqSingleton.Instance.FactorySettings[Parent.SessionGraph];

            var taskSettings = factorySettings.SelectNodes("tasks/task");
            for (int i = 0; i < taskSettings.Count; i++) {
                var taskType = taskSettings[i].Attributes.GetNamedItem("type")?.Value ?? "";
                if (taskType == "do") {
                    BufferLengthMs = int.Parse(taskSettings[i].Attributes.GetNamedItem("bufferlen")?.Value ?? "0");
                    PrebufferLengthMs = int.Parse(taskSettings[i].Attributes.GetNamedItem("prebufferlen")?.Value ?? "0");
                    ClockPath = taskSettings[i].Attributes.GetNamedItem("clksrc")?.Value ?? "";
                    break;
                }
            }
        }

        public void Serialize(XmlWriter writer) {
            writer.WriteStartElement("task");
            writer.WriteAttributeString("type", "do");
            writer.WriteAttributeString("dev", Device.Name);
            writer.WriteAttributeString("bufferlen", BufferLengthMs.ToString());
            writer.WriteAttributeString("prebufferlen", PrebufferLengthMs.ToString());
            writer.WriteAttributeString("clksrc", ClockPath);
            writer.WriteEndElement();
        }
    }
}
