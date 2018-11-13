using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MoreLinq;
using System.ComponentModel;

namespace DeviceLibrary {


    class SimulatorFactory2 : IDeviceFactory2 {

        private readonly IIDGenerator _gen;

        public string Name => "Sim Factory 2";

        public SimulatorFactory2(IIDGenerator gen) {
            _gen = gen;
        }

        public List<IDevice> CreateDevices() {
            return new List<IDevice> { new DeviceSimulator(_gen) };
        }
    }

    interface ISimulator2DataPort {
        void Generate();
        void Start();
        void Stop();
    }

    class Simulator2Singleton {

        private static Simulator2Singleton _inst;

        public static Simulator2Singleton Instance {
            get {
                if (_inst == null) _inst = new Simulator2Singleton();
                return _inst;
            }
        }


        //++++++++++++++++++++++++++++++++++++++++++++++++++

        private PrecisionTimer _precTimer;

        private DeviceSimulator _device;

        private Simulator2Singleton() {
        }

        public bool Recording { get; private set; }

        public int PeriodMs { get; set; } = 100;

        public bool Start(DeviceSimulator device) {
            if (Recording) return false;

            _device = device;

            if (_precTimer == null) {
                _precTimer                       = new PrecisionTimer();
                _precTimer.Elapsed              += _precTimer_Elapsed;
                _precTimer.Milliseconds          = PeriodMs;
                _precTimer.ToleranceMilliseconds = 10;
            }

            _precTimer.Enabled = true;
            Recording = true;

            return true;
        }

        private void _precTimer_Elapsed(object sender, int elapsed) {
            foreach (var p in _device.ListeningPorts.OfType<Simulator2Port>()) {
                p.Generate();
            }
        }

        public bool Stop() {
            if (!Recording) return false;

            _precTimer.Enabled = false;
            Recording = false;

            return true;
        }
    }


    class DeviceSimulator : IDevice {

        public Guid UniqueId { get; } = Guid.Parse("a4fd1636-8790-4c9d-a2ab-9952ceb61208");

        private readonly List<IDevicePort> _ports          = new List<IDevicePort>();
        private readonly List<IDevicePort> _listeningPorts = new List<IDevicePort>();

        private const int MaxSamplerate                    = 1000000;


        public event EventHandler<DeviceErrorArgs> OnError;


        public DeviceSimulator(IIDGenerator gen) {
            Id = gen.GetID();
            _ports.Add(new Simulator2Port(this, "Port 1", gen));
            _ports.Add(new Simulator2Port(this, "Port 2", gen));
            _ports.Add(new Simulator2Port(this, "Port 3", gen));

            foreach (var p in _ports.OfType<Simulator2Port>()) {
                p.StateChanged += P_StateChanged;
            }
        }

        private void P_StateChanged(IDevicePort port, DevicePortStatus state) {
            var activeCount = _ports.Count(p => p.Status == DevicePortStatus.Active);
            if (activeCount == 0) return;
            foreach (var p in _ports) {
                p.Samplerate = MaxSamplerate / activeCount;
            }
        }

        public bool StartSampling() {
            if (Recording) return true;

            var ports = Ports.Where(p => p.Status == DevicePortStatus.Active);

            lock (_listeningPorts) {
                _listeningPorts.Clear();
                _listeningPorts.AddRange(ports);

                foreach (var port in _listeningPorts) {
                    ((Simulator2Port)port).PrepareRecording(port.Samplerate * Simulator2Singleton.Instance.PeriodMs / 1000);
                }
            }

            Recording = true;
            Simulator2Singleton.Instance.Start(this);

            return true;
        }

        public void StopSampling() {
            if (!Recording) return;
            Simulator2Singleton.Instance.Stop();
            Recording = false;
        }


        public int Id { get; }

        public string Name => "Simulator 2";

        public bool Recording { get; private set; }

        public IEnumerable<IDevicePort> Ports => _ports;

        public IEnumerable<IDevicePort> ListeningPorts => _listeningPorts;


    }


    class Simulator2Port : IDevicePortInput, ISimulator2DataPort {

        [Browsable(false)]
        public Guid UniqueId { get; } = Guid.Parse("1fd17cf7-abf1-4153-a331-1940456e7b52");

        public enum SignalType {
            Sine,
            Triangle
        }

        private DevicePortStatus    _state;
        private int                 _samplerate;
        private double[]            _buffer;
        private double              _phaseStep;
        private double              _phaseAcc;
        private double              _phaseOffset;
        private double              _frequency;
        private SignalType          _type;


        private readonly Dictionary<SignalType, Func<double, double>> Generators = new Dictionary<SignalType, Func<double, double>> {
            { SignalType.Sine,      (phase) => Math.Sin(phase) },
            { SignalType.Triangle,  (phase) => ((phase % (2 * Math.PI)) - Math.PI) / Math.PI }
        };


        public event SamplerateChangedHandler SamplerateChanged;
        public event StateChangedHandler StateChanged;
        public event BufferReady OnBufferReady;


        public Simulator2Port(DeviceSimulator owner, string name, IIDGenerator gen) {
            _state    = DevicePortStatus.Idle;
            Id        = gen.GetID();
            Owner     = owner;
            Name      = name;
            Frequency = 50;
        }


        public void Deserialize(XmlNode node) {
            Frequency   = double.Parse(node.Attributes?.GetNamedItem("frequency")?.Value ?? "50");
            Phase       = double.Parse(node.Attributes?.GetNamedItem("phase")?.Value     ??  "0");
            var sigtype =              node.Attributes?.GetNamedItem("type")?.Value      ?? "Sine";

            Signal      = (SignalType)Enum.Parse(typeof(SignalType), sigtype);
        }

        public void Serialize(XmlWriter xml) {
            xml.WriteAttributeString("frequency", Frequency.ToString(System.Globalization.CultureInfo.InvariantCulture));
            xml.WriteAttributeString("phase", Phase.ToString(System.Globalization.CultureInfo.InvariantCulture));
            xml.WriteAttributeString("type", Signal.ToString());
        }

        public void PrepareRecording(int bufferSize) {
            _buffer = new double[bufferSize];
            CalculatePhaseStep();
            _phaseAcc = 0;
        }

        public void Generate() {
            var gen = Generators[_type];
            for (int i = 0; i < _buffer.Length; i++) {
                _buffer[i] = gen(_phaseAcc + _phaseOffset * Math.PI / 180);
                _phaseAcc += _phaseStep;
            }
            OnBufferReady?.Invoke(this, _buffer);
        }


        public SignalType Signal {
            get {
                return _type;
            }
            set {
                _type = value;
                CalculatePhaseStep();
            }
        }

        public double Phase {
            get {
                return _phaseOffset;
            }
            set {
                _phaseOffset = value;
                CalculatePhaseStep();
            }
        }

        public double Frequency {
            get {
                return _frequency;
            }
            set {
                _frequency = value;
                CalculatePhaseStep();
            }
        }

        [Browsable(false)]
        public int Samplerate {
            get {
                return _samplerate;
            }
            set {
                if (Owner.Recording) throw new InvalidOperationException("Status can't be changed while graph is running");
                _samplerate = value;
                SamplerateChanged?.Invoke(this, value);
            }
        }

        [Browsable(false)]
        public DevicePortStatus Status {
            get {
                return _state;
            }
            set {
                if (Owner.Recording) throw new InvalidOperationException("Status can't be changed while graph is running");
                _state = value;
                StateChanged?.Invoke(this, value);
            }
        }


        private void CalculatePhaseStep() {
            _phaseStep = 2.0 * Math.PI * Frequency / Samplerate;
        }


        public void Start() { }

        public void Stop() { }

        [Browsable(false)]
        public int Id { get; }

        [Browsable(false)]
        public string Name { get; }

        [Browsable(false)]
        public IDevice Owner { get; }

        public ChannelDirection Direction => ChannelDirection.Input;

    }


}
