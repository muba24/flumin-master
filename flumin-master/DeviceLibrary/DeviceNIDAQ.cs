using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DeviceLibrary {

    class DeviceNIDAQ2Factory : IDeviceFactory2 {

        private readonly IIDGenerator _gen;

        public string Name => "NIDAQ Dev Factory";

        public DeviceNIDAQ2Factory(IIDGenerator gen) {
            _gen = gen;
        }

        public List<IDevice> CreateDevices() {
            var buffer = new StringBuilder(256 + 1);
            var result = NidaQmxHelper.DAQmxGetSysDevNames(buffer, buffer.Length - 1);

            if (result < 0) {
                throw new SystemException("Could not query nidaq device list");
            }

            if (buffer.ToString().Length > 0) {
                return buffer.ToString()
                             .Split(',')
                             .Select(s => s.Trim())
                             .Select(s => new NidaQmxDevice2(s, _gen))
                             .ToList<IDevice>();
            } else {
                return new List<IDevice>();
            }
        }

    }

    class NidaQmx2Singleton {

        [Flags]
        private enum InOutPortFlags {
            UseInput  = 1 << 0,
            UseOutput = 1 << 1
        }

        // TODO: CLEARING THE TASK ALLOCATED BY A SINGLE DEVICE OBJECT MAY BE STUPID.
        //       GIVE THE RESPONSIBILITY TO THE DEVICE OBJECT.

        private static NidaQmx2Singleton _inst = null;

        public static NidaQmx2Singleton Instance => _inst ?? (_inst = new NidaQmx2Singleton());

        private NidaQmx2Singleton() {
        }

        private NidaQmxDevice2 _device;

        public bool Recording { get; private set; }

        private volatile bool _stopThread;

        private Thread _pollThread;

        private IntPtr Data { get; set; }

        public bool Start(NidaQmxDevice2 device) {
            if (Recording) return false;

            _device = device;

            InOutPortFlags flags = 0;
            if (device.ListeningPorts.Any(p => p.Direction == ChannelDirection.Input))  flags |= InOutPortFlags.UseInput;
            if (device.ListeningPorts.Any(p => p.Direction == ChannelDirection.Output)) flags |= InOutPortFlags.UseOutput;

            if (!CreateVirtualChannel(flags)) return false;
            if (!ConfigureDevClock(flags)) return false;

            if (flags.HasFlag(InOutPortFlags.UseInput)) {
                Data = Marshal.AllocHGlobal(sizeof(double) * device.SamplesPerChannelInput * device.ListeningPorts.Count());
                foreach (var port in device.ListeningPorts.OfType<NidaQmxChannelInput>()) {
                    port.SetBufferSize(device.SamplesPerChannelInput);
                }
            }

            if (!RunTask(flags)) return false;

            _stopThread = false;
            if (flags.HasFlag(InOutPortFlags.UseInput)) {
                _pollThread = new Thread(EveryNCallback);
                _pollThread.Start();
            }

            Recording = true;

            return true;
        }

        public bool Stop() {
            if (!Recording) return false;

            _stopThread = true;
            if (_pollThread != null && Thread.CurrentThread != _pollThread) {
                _pollThread.Join();
            }

            Console.WriteLine("Stop call von Thread " + Thread.CurrentThread.ManagedThreadId);
            var resultStopTask = NidaQmxHelper.DAQmxStopTask(_device.InputTaskHandle);
            if (resultStopTask < 0)
                _device.ReportError(new DeviceErrorArgs(NidaQmxHelper.GetError(resultStopTask)));

            Marshal.FreeHGlobal(Data);
            Data = IntPtr.Zero;

            Recording = false;

            return true;
        }


        private bool CreateVirtualChannel(InOutPortFlags flags) {
            // ------------------------------------

            if (flags.HasFlag(InOutPortFlags.UseInput)) {
                var inputs = _device.ListeningPorts.Where(port => port.Direction == ChannelDirection.Input)
                                               .Select(port => port.Name);

                if (inputs.Any()) {
                    var inputPortStrings = string.Join(",", inputs);

                    var result = NidaQmxHelper.DAQmxCreateAIVoltageChan(
                        maxVal:                 10.0,
                        minVal:                 -10.0,
                        units:                  NidaQmxHelper.DaQmxValVolts,
                        terminalConfig:         NidaQmxHelper.DaQmxValRse,
                        physicalChannel:        inputPortStrings,
                        taskHandle:             _device.InputTaskHandle,
                        nameToAssignToChannel:  "chanI" + _device.Name,
                        customScaleName:        null
                    );

                    if (result < 0) {
                        NidaQmxHelper.DAQmxClearTask(_device.InputTaskHandle);
                        _device.ReportError(new DeviceErrorArgs(NidaQmxHelper.GetError(result)));
                        return false;
                    }
                }
            }

            // ------------------------------------

            if (flags.HasFlag(InOutPortFlags.UseOutput)) {
                var outputs = _device.ListeningPorts.Where(port => port.Direction == ChannelDirection.Output)
                                                    .Select(port => port.Name);

                if (outputs.Any()) {
                    var outputPortStrings = string.Join(",", outputs);

                    var channelNumber = 0;
                    foreach (var output in _device.ListeningPorts.Where(port => port.Direction == ChannelDirection.Output)) {
                        ((NidaQmxChannelOutput)output).ChannelNumber = channelNumber++;
                    }

                    var result = NidaQmxHelper.DAQmxCreateAOVoltageChan(
                        maxVal: 10.0,
                        minVal: -10.0,
                        units: NidaQmxHelper.DaQmxValVolts,
                        physicalChannel: outputPortStrings,
                        taskHandle: _device.OutputTaskHandle,
                        nameToAssignToChannel: "chanO" + _device.Name,
                        customScaleName: null
                    );

                    if (result < 0) {
                        NidaQmxHelper.DAQmxClearTask(_device.OutputTaskHandle);
                        _device.ReportError(new DeviceErrorArgs(NidaQmxHelper.GetError(result)));
                        return false;
                    }
                }
            }

            // ------------------------------------

            return true;
        }

        private bool ConfigureDevClock(InOutPortFlags flags) {
            if (flags.HasFlag(InOutPortFlags.UseInput)) {
                var result = NidaQmxHelper.DAQmxCfgSampClkTiming(
                    activeEdge: NidaQmxHelper.DaQmxValRising,
                    sampleMode: NidaQmxHelper.DaQmxValContSamps,
                    sampsPerChan: (ulong)_device.SamplesPerChannelInput,
                    taskHandle: _device.InputTaskHandle,
                    source: "",
                    rate: _device.SamplerateInput
                );

                if (result < 0) {
                    NidaQmxHelper.DAQmxClearTask(_device.InputTaskHandle);
                    _device.ReportError(new DeviceErrorArgs(NidaQmxHelper.GetError(result)));
                    return false;
                }
            }

            if (flags.HasFlag(InOutPortFlags.UseOutput)) {
                var result = NidaQmxHelper.DAQmxCfgSampClkTiming(
                    activeEdge: NidaQmxHelper.DaQmxValRising,
                    sampleMode: NidaQmxHelper.DaQmxValContSamps,
                    sampsPerChan: (ulong)_device.SamplesPerChannelOutput,
                    taskHandle: _device.OutputTaskHandle,
                    source: "",
                    rate: _device.SamplerateOutput
                );

                if (result < 0) {
                    NidaQmxHelper.DAQmxClearTask(_device.InputTaskHandle);
                    NidaQmxHelper.DAQmxClearTask(_device.OutputTaskHandle);
                    _device.ReportError(new DeviceErrorArgs(NidaQmxHelper.GetError(result)));
                    return false;
                }

                result = NidaQmxHelper.DAQmxCfgOutputBuffer(_device.OutputTaskHandle, _device.SamplesPerChannelOutput);
                if (result < 0) {
                    NidaQmxHelper.DAQmxClearTask(_device.InputTaskHandle);
                    NidaQmxHelper.DAQmxClearTask(_device.OutputTaskHandle);
                    _device.ReportError(new DeviceErrorArgs(NidaQmxHelper.GetError(result)));
                    return false;
                }
            }

            return true;
        }

        private bool RunTask(InOutPortFlags flags) {
            if (flags.HasFlag(InOutPortFlags.UseInput)) {
                var result = NidaQmxHelper.DAQmxStartTask(_device.InputTaskHandle);
                if (result < 0) {
                    NidaQmxHelper.DAQmxClearTask(_device.InputTaskHandle);
                    _device.ReportError(new DeviceErrorArgs(NidaQmxHelper.GetError(result)));
                    return false;
                }
            }

            //if (flags.HasFlag(InOutPortFlags.UseOutput)) {
            //    var result = NidaQmxHelper.DAQmxStartTask(_device.OutputTaskHandle);
            //    if (result < 0) {
            //        NidaQmxHelper.DAQmxStopTask(_device.InputTaskHandle);
            //        NidaQmxHelper.DAQmxClearTask(_device.InputTaskHandle);
            //        NidaQmxHelper.DAQmxClearTask(_device.OutputTaskHandle);
            //        _device.ReportError(new DeviceErrorArgs(NidaQmxHelper.GetError(result)));
            //        return false;
            //    }
            //}

            return true;
        }


        private void EveryNCallback() {
            var hPoll = start_polling(_device.InputTaskHandle, _device.SamplesPerChannelInput * _device.ListeningPorts.Count(), _device.SamplesPerChannelInput);

            while (!_stopThread) {
                var result = read_buffer(hPoll, Data, 8 * (_device.SamplesPerChannelInput * _device.ListeningPorts.Count()));
                switch (result) {
                    case 2:
                        // queue empty, ignore
                        Thread.Sleep(1);
                        break;

                    case 1:
                        _device.ReportError(new DeviceErrorArgs("Buffer too small"));
                        _stopThread = true;
                        break;

                    case 0:
                        var channelData = Data;

                        // this can not be a foreach loop because listeningPorts may change.
                        // For example when there is a buffering problem and listeningPorts is cleared
                        foreach (var port in _device.ListeningPorts.OfType<NidaQmxChannelInput>()) {
                            port.DistributeData(channelData, _device.SamplesPerChannelInput);
                            channelData = IntPtr.Add(channelData, sizeof(double) * _device.SamplesPerChannelInput);
                        }

                        break;

                    default:
                        // read error
                        _device.ReportError(new DeviceErrorArgs(NidaQmxHelper.GetError(result)));
                        _stopThread = true;
                        break;

                }
            }

            stop_polling(hPoll);
        }

        [DllImport("NILoop.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int start_polling(
            int task_handle,
            int buf_size,
            int samps_per_chan
        );

        [DllImport("NILoop.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void stop_polling(
            int poll_handle
        );

        [DllImport("NILoop.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int read_buffer(
            int poll_handle,
            IntPtr ptr_data,
            int size
        );


    }

    class NidaQmxDevice2 : IDevice {

        public Guid UniqueId { get; } = Guid.Parse("e71b7f61-19dd-4d46-9093-a3e1af454e1e");

        private readonly List<NidaQmxChannel> _channels;

        private List<NidaQmxChannel> _listeningPorts;

        public int InputTaskHandle { get; set; }

        public int OutputTaskHandle { get; set; }

        public event EventHandler<DeviceErrorArgs> OnError;

        public NidaQmxDevice2(string device, IIDGenerator idgen) {
            Name = device;

            var bufferInputChannelNames = new StringBuilder(256 + 1);
            var resultQueryAI = NidaQmxHelper.DAQmxGetDevAIPhysicalChans(device, bufferInputChannelNames, bufferInputChannelNames.Length - 1);
            if (resultQueryAI < 0) throw new SystemException("Could not query input channels for nidaq device " + device);

            var bufferOutputChannelNames = new StringBuilder(256 + 1);
            var resultQueryAO = NidaQmxHelper.DAQmxGetDevAOPhysicalChans(device, bufferOutputChannelNames, bufferOutputChannelNames.Length - 1);
            if (resultQueryAO < 0) throw new SystemException("Could not query input channels for nidaq device " + device);

            var ai = bufferInputChannelNames.ToString()
                                            .Split(',')
                                            .Select(s => (NidaQmxChannel) new NidaQmxChannelInput(this, s.Trim(), idgen));

            var ao = bufferOutputChannelNames.ToString()
                                             .Split(',')
                                             .Select(s => (NidaQmxChannel) new NidaQmxChannelOutput(this, s.Trim(), idgen));

            _channels = ai.Concat(ao).ToList();

            if (!CreateTaskHandle()) throw new SystemException("Could not acquire nidaq task handle");

            Id = idgen.GetID();
        }

        public IEnumerable<IDevicePort> ListeningPorts => _listeningPorts;
        public IEnumerable<IDevicePort> Ports => _channels;

        public DataOutputStage OutputBuffer { get; private set; }
        public int SamplesPerChannelInput { get; private set; }
        public int SamplesPerChannelOutput { get; private set; }
        public int SamplerateInput { get; private set; }
        public int SamplerateOutput { get; private set; }
        public bool Recording { get; private set; }

        public string Name { get; }
        public int Id { get; }

        public bool StartSampling() {
            if (Recording) return true;

            var outputPorts = Ports.OfType<NidaQmxChannelOutput>().Where(p => p.Status == DevicePortStatus.Active);

            if (outputPorts.Any()) {
                var sameRate = outputPorts.All(p => p.Samplerate == outputPorts.First().Samplerate);
                if (!sameRate) {
                    ReportError(new DeviceErrorArgs("Output Ports must all have the same rate"));
                    return false;
                }

                const int SubBufferCount = 5;
                SamplerateOutput = outputPorts.First().Samplerate;
                SamplesPerChannelOutput = SamplerateOutput / SubBufferCount;

                // maximum buffer of 1 second, but smaller subbuffers for less latency
                OutputBuffer = new DataOutputStage(outputPorts.Count(), SamplerateOutput / SubBufferCount, 2 * SubBufferCount);
            }

            _listeningPorts.Clear();
            _listeningPorts.AddRange(Ports.OfType<NidaQmxChannel>().Where(p => p.Status == DevicePortStatus.Active));

            Recording = NidaQmx2Singleton.Instance.Start(this);
            return Recording;
        }

        public void StopSampling() {
            if (!Recording) return;

            NidaQmx2Singleton.Instance.Stop();
            NidaQmxHelper.DAQmxClearTask(InputTaskHandle);
            NidaQmxHelper.DAQmxClearTask(OutputTaskHandle);
            CreateTaskHandle();

            Recording = false;
        }

        public void UpdateListening() {
            if (Recording) return;

            var inputPorts  = Ports.OfType<NidaQmxChannel>().Where(p => p.Direction == ChannelDirection.Input  && p.Status == DevicePortStatus.Active);
            var outputPorts = Ports.OfType<NidaQmxChannel>().Where(p => p.Direction == ChannelDirection.Output && p.Status == DevicePortStatus.Active);

            _listeningPorts = inputPorts.Concat(outputPorts).ToList();
            if (_listeningPorts.Count == 0) return;

            double maxMultiChannelRate;
            var result = NidaQmxHelper.DAQmxGetDevAIMaxMultiChanRate(Name, out maxMultiChannelRate);
            if (result < 0) {
                OnError?.Invoke(this, new DeviceErrorArgs(NidaQmxHelper.GetError(result)));
                return;
            }

            SamplerateInput = (int)(maxMultiChannelRate / inputPorts.Count());
            SamplesPerChannelInput = (int)(10000.0 * Math.Floor(SamplerateInput / 20.0 / 10000.0));

            foreach (var port in inputPorts) {
                port.Samplerate = SamplerateInput;
            }
        }

        private bool CreateTaskHandle() {
            int[] taskHandle = new int[1];
            var result = NidaQmxHelper.DAQmxCreateTask(null, taskHandle);
            if (result < 0) {
                OnError?.Invoke(this, new DeviceErrorArgs("Could not create input nidaq task"));
                return false;
            }
            InputTaskHandle = taskHandle[0];

            result = NidaQmxHelper.DAQmxCreateTask(null, taskHandle);
            if (result < 0) {
                OnError?.Invoke(this, new DeviceErrorArgs("Could not create output nidaq task"));
                return false;
            }
            OutputTaskHandle = taskHandle[0];

            return true;
        }

        public void ReportError(DeviceErrorArgs arg) {
            OnError?.Invoke(this, arg);
        }

    }


    class NidaQmxChannel : IDevicePort {

        [Browsable(false)]
        public Guid UniqueId { get; } = Guid.Empty;

        private ChannelDirection    _direction;
        private DevicePortStatus    _status;
        private int                 _samplerate;

        public event SamplerateChangedHandler SamplerateChanged;

        public NidaQmxChannel(NidaQmxDevice2 device, string channel, ChannelDirection direction, IIDGenerator idgen) {
            _status    = DevicePortStatus.Idle;
            Id         = idgen.GetID();
            Channel    = channel;
            Device     = device;
            _direction = direction;
        }

        public void Serialize(XmlWriter xml) {

        }

        public void Deserialize(XmlNode node) {

        }

        public ChannelDirection Direction => _direction;

        [Browsable(false)]
        public int Samplerate {
            get { return _samplerate; }
            set {
                _samplerate = value;
                SamplerateChanged?.Invoke(this, value);
            }
        }

        [Browsable(false)]
        public DevicePortStatus Status {
            get { return _status; }
            set {
                if (Owner.Recording) throw new InvalidOperationException();
                if (value != _status) {
                    _status = value;
                    ((NidaQmxDevice2)Owner)?.UpdateListening();
                }
            }
        }

        public override string ToString() => Channel;

        [Browsable(false)]
        public string Name => Channel;

        [Browsable(false)]
        public int Id { get; }

        [Browsable(false)]
        public NidaQmxDevice2 Device { get; }

        [Browsable(false)]
        public IDevice Owner => Device;

        [Browsable(false)]
        public string Channel { get; }

    }



    /// <summary>
    /// write data to NIDAQ device
    /// </summary>
    class NidaQmxChannelOutput : NidaQmxChannel, IDevicePortOutput {

        [Browsable(false)]
        public new Guid UniqueId { get; } = Guid.Parse("e23a7885-447e-463f-b4cc-44ecfd4faedd");

        public NidaQmxChannelOutput(NidaQmxDevice2 device, string channel, IIDGenerator idgen)
            : base(device, channel, ChannelDirection.Output, idgen) {

        }

        [Browsable(false)]
        public int ChannelNumber { get; set; }

        public void Write(double[] data, int offset, int samples) {
            int written;

            Device.OutputBuffer.Write(ChannelNumber, data, offset, samples);

            if (Device.OutputBuffer.BufferReady) {
                unsafe {
                    fixed (double* ptr = Device.OutputBuffer.CurrentBuffer.Data) {
                        var result = NidaQmxHelper.DAQmxWriteAnalogF64(
                            taskHandle:             Device.OutputTaskHandle, 
                            numSampsPerChan:        Device.OutputBuffer.CurrentBuffer.SamplesPerChannel, 
                            autoStart:              1, 
                            timeout:                10, 
                            dataLayout:             NidaQmxHelper.DaQmxValGroupByChannel, 
                            writeArray:             new IntPtr(ptr), 
                            sampsPerChanWritten:    out written, 
                            reserved:               IntPtr.Zero
                        );

                        if (result < 0) {
                            Device.ReportError(new DeviceErrorArgs(NidaQmxHelper.GetError(result)));
                        }
                    }
                }

                Device.OutputBuffer.MoveToNextBuffer();
            }
        }

        public override string ToString() => Channel;

    }

    /// <summary>
    /// read data from NIDAQ device
    /// </summary>
    class NidaQmxChannelInput : NidaQmxChannel, IDevicePortInput {

        [Browsable(false)]
        public new Guid UniqueId { get; } = Guid.Parse("e23a7885-447e-463f-b4cc-44ecfd4faedd");

        private double[] _buffer;

        public event BufferReady OnBufferReady;

        public NidaQmxChannelInput(NidaQmxDevice2 device, string channel, IIDGenerator idgen) 
            : base(device, channel, ChannelDirection.Input, idgen) {
        }

        public void DistributeData(IntPtr channelData, int samples) {
            Marshal.Copy(channelData, _buffer, 0, samples);
            OnBufferReady?.Invoke(this, _buffer);
        }

        public void SetBufferSize(int size) {
            if (_buffer != null && size == _buffer.Length) return;
            _buffer = new double[size];
        }

        public override string ToString() => Channel;

    }

}