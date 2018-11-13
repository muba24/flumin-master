using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using DeviceLibrary;
using System.ComponentModel;

namespace SimpleADC.NodeSystem {

    class EmptyDevicePort : DeviceLibrary.IDevicePort {

        private XmlNode _node;

        [Browsable(false)]
        public int Id => -1;
        
        public string Name => "Empty Device Port";

        [Browsable(false)]
        public IDevice Owner => null;

        [ReadOnly(true)]
        public int Samplerate { get; set; }

        [Browsable(false)]
        public DevicePortStatus Status { get; set; }

        public Guid UniqueId { get; set; }

        public event BufferReady OnBufferReady;
        public event SamplerateChangedHandler SamplerateChanged;

        public ChannelDirection Direction { get; }

        public EmptyDevicePort(ChannelDirection direction) {
            Direction = direction;
        }

        public void Deserialize(XmlNode node) {
            _node = node.CloneNode(true);
        }

        public void Serialize(XmlWriter xml) {
            foreach (XmlAttribute attr in _node.Attributes) {
                try {
                    if (attr.Name != "device" && attr.Name != "port" && attr.Name != "type")
                        xml.WriteAttributeString(attr.Name, attr.Value);
                } catch {
                    //
                }
            }
        }
    }
}
