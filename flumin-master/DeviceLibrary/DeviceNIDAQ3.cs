using NodeSystemLib2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DeviceLibrary.NidaqSingleton;
using System.Xml;

namespace DeviceLibrary {
    public class DeviceNidaqFactory : IMetricFactory {

        public DeviceNidaqFactory() {
        }

        public Guid Id => new Guid("e6a6ef5d-bf60-452d-8570-482ef0e686c8");

        public int Count => Instance.Devices.Sum(device => device.ChannelCount);

        public Node CreateInstance(string uniqueId, Graph g) {
            foreach (var dev in Instance.Devices) {
                var channels = dev.AnalogInputChannels.Concat(dev.AnalogOutputChannels)
                                                      .Concat(dev.DigitalInputChannels)
                                                      .Concat(dev.DigitalOutputChannels);

                foreach (var ch in channels) {
                    if (ch.Path.Equals(uniqueId)) {
                        return ch.CreateInstance(g);
                    }
                }
            }
            throw new KeyNotFoundException();
        }

        public Node CreateInstance(int index, Graph g) {
            var ch = ChannelFromIndex(index);
            return ch?.CreateInstance(g);
        }

        public MetricMetaData GetMetricInfo(int index) {
            var ch = ChannelFromIndex(index);
            if (ch != null) {
                return new MetricMetaData(ch.ToString(), "Nidaq", ch.ToString());
            }
            return null;
        }

        private Channel ChannelFromIndex(int index) {
            foreach (var dev in Instance.Devices) {
                var ch = dev.ChannelFromIndex(index);
                if (ch != null) return ch;
            }
            return null;
        }

        public Node CreateInstance(int index, Graph g, XmlNode node) {
            var ch = ChannelFromIndex(index);
            return ch?.CreateInstance(g, node);
        }

        public Node CreateInstance(string uniqueId, Graph g, XmlNode node) {
            foreach (var dev in Instance.Devices) {
                var channels = dev.AnalogInputChannels.Concat(dev.AnalogOutputChannels)
                                                      .Concat(dev.DigitalInputChannels)
                                                      .Concat(dev.DigitalOutputChannels);

                foreach (var ch in channels) {
                    if (ch.Path.Equals(uniqueId)) {
                        return ch.CreateInstance(g, node);
                    }
                }
            }
            throw new KeyNotFoundException();
        }

        public void SaveInternalState(Graph g, XmlWriter writer) {
            if (NidaqSingleton.Instance.Sessions.ContainsKey(g)) {
                NidaqSingleton.Instance.Sessions[g].SaveInternalState(writer);
            }
        }

        public void SetFactorySettings(Graph g, XmlNode factorySettings) {
            if (NidaqSingleton.Instance.FactorySettings.ContainsKey(g)) {
                System.Diagnostics.Debug.WriteLine("NidaqSingleton: Warning, factory settings for g already exist. Overwriting...");
                NidaqSingleton.Instance.FactorySettings[g] = factorySettings.CloneNode(deep: true);
            }
            NidaqSingleton.Instance.FactorySettings.Add(g, factorySettings.CloneNode(deep: true));
        }
    }
}
