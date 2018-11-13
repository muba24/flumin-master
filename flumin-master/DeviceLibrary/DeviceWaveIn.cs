using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NAudio.Wave;
using System.ComponentModel;

namespace DeviceLibrary {

    //class WaveInFactory : IDeviceFactory {

    //    private readonly WaveInRecorderDevice _dev;

    //    public WaveInFactory(DeviceFactory.IIDGenerator idgen) {
    //        _dev = new WaveInRecorderDevice(idgen);
    //    }

    //    public IEnumerable<IDevice> Devices
    //    {
    //        get
    //        {
    //            yield return _dev;
    //        }
    //    }

    //}

    //class WaveInRecorderDevice : IDevice {

    //    private const int Samplerate = 22050;

    //    private readonly List<IDevicePort> _ports = new List<IDevicePort>();
    //    private readonly List<IDevicePort> _listeningPorts = new List<IDevicePort>();

    //    public WaveInRecorderDevice(DeviceFactory.IIDGenerator idgen) {
    //        Id = idgen.GetID();
    //        _ports.Add(new WaveInRecorderPort(this, idgen));
    //    }

    //    public int Id { get; }

    //    public IEnumerable<IDevicePort> ListeningPorts => _listeningPorts;

    //    public string Name => "WaveIn Recorder";

    //    public IEnumerable<IDevicePort> Ports => _ports;

    //    public bool Recording { get; private set; }

    //    public event EventHandler<DeviceErrorArgs> OnError;

    //    public bool StartSampling() {
    //        if (Recording) return true;

    //        var ports = Ports.Where(p => p.Status == DevicePortStatus.Active);

    //        lock (_listeningPorts) {
    //            _listeningPorts.Clear();
    //            _listeningPorts.AddRange(ports);

    //            foreach (var port in _listeningPorts) {
    //                port.Samplerate = 22050;
    //                ((WaveInRecorderPort)port).Start();
    //            }
    //        }

    //        Recording = true;
    //        return true;
    //    }

    //    public void StopSampling() {
    //        if (!Recording) return;

    //        lock (_listeningPorts) {
    //            foreach (var port in _listeningPorts) {
    //                ((WaveInRecorderPort)port).Stop();
    //            }
    //            _listeningPorts.Clear();
    //        }

    //        Recording = false;

    //    }
    //}

    //class WaveInRecorderPort : IDevicePort {

    //    private DevicePortStatus _status;

    //    private readonly WaveIn _waveIn;

    //    public WaveInRecorderPort(WaveInRecorderDevice owner, DeviceFactory.IIDGenerator idgen) {
    //        _waveIn                = new WaveIn();
    //        Id                     = idgen.GetID();
    //        Owner                  = owner;
    //        Samplerate             = 22050;
    //        _waveIn.DataAvailable += _waveIn_DataAvailable;
    //    }

    //    private void _waveIn_DataAvailable(object sender, WaveInEventArgs e) {
    //        var buffer = new double[e.BytesRecorded / 2];
    //        for (int index = 0; index < e.BytesRecorded; index += 2) {
    //            var sample        = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index]);
    //            buffer[index / 2] = sample / 32768f;
    //        }
    //        OnBufferReady?.Invoke(this, buffer);
    //    }

    //    [Browsable(false)]
    //    public int Id { get; }

    //    public string Name => "WaveIn";

    //    [Browsable(false)]
    //    public IDevice Owner { get; }

    //    [ReadOnly(true)]
    //    public int Samplerate
    //    {
    //        get
    //        {
    //            return _waveIn.WaveFormat.SampleRate;
    //        }

    //        set
    //        {
    //            if (Owner.Recording) throw new InvalidOperationException();
    //            _waveIn.WaveFormat = new WaveFormat(value, 16, 1);
    //            SamplerateChanged?.Invoke(this, value);
    //        }
    //    }

    //    [Browsable(false)]
    //    public DevicePortStatus Status
    //    {
    //        get
    //        {
    //            return _status;
    //        }

    //        set
    //        {
    //            if (Owner.Recording) throw new InvalidOperationException();
    //            _status = value;
    //        }
    //    }

    //    public event BufferReady OnBufferReady;
    //    public event SamplerateChangedHandler SamplerateChanged;

    //    public void Deserialize(XmlNode node) {
    //    }

    //    public void Serialize(XmlWriter xml) {
    //    }

    //    internal void Start() {
    //        _waveIn.StartRecording();
    //    }

    //    internal void Stop() {
    //        _waveIn.StopRecording();
    //    }
    //}
   
}
