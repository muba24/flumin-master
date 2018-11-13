using NodeSystemLib2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DeviceLibrary {
    class NidaqSingleton {

        private static NidaqSingleton _instance;

        public static NidaqSingleton Instance => _instance ?? (_instance = new NidaqSingleton());


        // --------------------------------------------------------
        // --------------------------------------------------------

        private Dictionary<Graph, NidaqSession> _sessions = new Dictionary<Graph, NidaqSession>();

        private Dictionary<Graph, XmlNode> _factorySettings = new Dictionary<Graph, XmlNode>();

        private Device[] _devices;

        public IReadOnlyList<Device> Devices => _devices;

        public Dictionary<Graph, NidaqSession> Sessions => _sessions;

        public Dictionary<Graph, XmlNode> FactorySettings => _factorySettings;

        private NidaqSingleton() {
            _devices = GetDevices();
        }

        public NidaqSession AddToSession(Node n) {
            if (!Sessions.ContainsKey(n.Parent)) {
                Sessions.Add(n.Parent, new NidaqSession());
            }

            Sessions[n.Parent].RegisterNode(n);
            return Sessions[n.Parent];
        }

        private Device[] GetDevices() {
            var indexCounter = 0;
            if (GetDeviceNames() == null) return new Device[0];
            return GetDeviceNames().Select(name => new Device(name, ref indexCounter)).ToArray();
        }

        private string[] GetDeviceNames() {
            var buffer = new StringBuilder(256 + 1);
            var result = NidaQmxHelper.DAQmxGetSysDevNames(buffer, buffer.Length - 1);

            if (result < 0) {
                throw new SystemException("Could not query nidaq device list");
            }

            if (buffer.ToString().Length > 0) {
                return buffer.ToString()
                             .Split(',')
                             .Select(s => s.Trim()).ToArray();
            }

            return null;
        }

        public class Device {

            private Channel[] _ai, _di, _ao, _do;

            public string Name { get; private set; }
            public Channel[] AnalogInputChannels => _ai;
            public Channel[] DigitalInputChannels => _di;
            public Channel[] AnalogOutputChannels => _ao;
            public Channel[] DigitalOutputChannels => _do;

            public Device(string name, ref int channelIndexOffset) {
                Name = name;
                GetAnalogInputChannels(ref channelIndexOffset);
                GetAnalogOutputChannels(ref channelIndexOffset);
                GetDigitalInputLines(ref channelIndexOffset);
                GetDigitalOutputLines(ref channelIndexOffset);
            }

            public int ChannelCount => AnalogInputChannels.Length + 
                                       AnalogOutputChannels.Length + 
                                       DigitalInputChannels.Length +
                                       DigitalOutputChannels.Length;

            public Channel ChannelFromIndex(int index) {
                var ai = AnalogInputChannels.FirstOrDefault(ch => ch.Index == index);
                if (ai != null) return ai;

                var di = DigitalInputChannels.FirstOrDefault(ch => ch.Index == index);
                if (di != null) return di;

                var ao = AnalogOutputChannels.FirstOrDefault(ch => ch.Index == index);
                if (ao != null) return ao;

                var @do = DigitalOutputChannels.FirstOrDefault(ch => ch.Index == index);
                if (@do != null) return @do;

                return null;
            }

            private void GetDigitalInputLines(ref int indexOffset) {
                GetDeviceChannelInfo(NidaQmxHelper.DAQmxGetDevDILines, ref _di, Channel.ChannelType.DigitalIn, ref indexOffset);
            }

            private void GetDigitalOutputLines(ref int indexOffset) {
                GetDeviceChannelInfo(NidaQmxHelper.DAQmxGetDevDOLines, ref _do, Channel.ChannelType.DigitalOut, ref indexOffset);
            }

            private void GetAnalogInputChannels(ref int indexOffset) {
                GetDeviceChannelInfo(NidaQmxHelper.DAQmxGetDevAIPhysicalChans, ref _ai, Channel.ChannelType.AnalogIn, ref indexOffset);
            }

            private void GetAnalogOutputChannels(ref int indexOffset) {
                GetDeviceChannelInfo(NidaQmxHelper.DAQmxGetDevAOPhysicalChans, ref _ao, Channel.ChannelType.AnalogOut, ref indexOffset);
            }

            private void GetDeviceChannelInfo(Func<string, StringBuilder, int, int> f, ref Channel[] dst, Channel.ChannelType type, ref int indexOffset) {
                var bufferNames = new StringBuilder(10000);
                var result = f(Name, bufferNames, bufferNames.Capacity - 1);
                if (result < 0) throw new SystemException($"Could not query lines of type {type} for nidaq device " + Name);

                var indexCounter = indexOffset;
                dst = bufferNames.ToString()
                                 .Split(',')
                                 .Select(s => new Channel(indexCounter++, this, s.Trim(), type))
                                 .ToArray();

                indexOffset = indexCounter;
            }

        }

        public class Channel {

            [Flags]
            public enum ChannelType {
                AnalogIn = 1 << 1,
                AnalogOut = 1 << 2,
                DigitalIn = 1 << 3,
                DigitalOut = 1 << 4
            }

            public override string ToString() => GetPrefixForType(Type) + " " + Path;

            public Device Device { get; }

            public string Path { get; }

            public ChannelType Type { get; }

            public int Index { get; }

            private static string GetPrefixForType(ChannelType type) {
                switch (type) {
                    case ChannelType.AnalogIn: return "AI";
                    case ChannelType.AnalogOut: return "AO";
                    case ChannelType.DigitalIn: return "DI";
                    case ChannelType.DigitalOut: return "DO";
                    default: return "";
                }
            }

            public Channel(int index, Device device, string path, ChannelType type) {
                Path = path;
                Device = device;
                Type = type;
                Index = index;
            }

            public NodeSystemLib2.Node CreateInstance(Graph g) {
                switch (Type) {
                    case ChannelType.AnalogIn:
                        return new MetricAnalogInput(Device, this, g);
                    case ChannelType.AnalogOut:
                        return new MetricAnalogOutput(Device, this, g);
                    case ChannelType.DigitalIn:
                        return new MetricDigitalInput(Device, this, g);
                    case ChannelType.DigitalOut:
                        return new MetricDigitalOutput(Device, this, g);
                    default:
                        throw new NotImplementedException();
                }
            }

            public NodeSystemLib2.Node CreateInstance(Graph g, XmlNode node) {
                switch (Type) {
                    case ChannelType.AnalogIn:
                        return new MetricAnalogInput(Device, this, g, node);
                    case ChannelType.AnalogOut:
                        return new MetricAnalogOutput(Device, this, g, node);
                    case ChannelType.DigitalIn:
                        return new MetricDigitalInput(Device, this, g, node);
                    case ChannelType.DigitalOut:
                        return new MetricDigitalOutput(Device, this, g, node);
                    default:
                        throw new NotImplementedException();
                }
            }

        }

    }
}
