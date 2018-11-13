using System;
using System.Xml;

namespace DeviceLibrary {

    public enum DevicePortStatus {
        Active,
        Idle
    }

    public enum ChannelDirection {
        Input, Output
    }

    public delegate void BufferReady(IDevicePort sender, double[] buffer);

    public delegate void SamplerateChangedHandler(IDevicePort sender, int newRate);

    public delegate void StateChangedHandler(IDevicePort sender, DevicePortStatus state);

    public interface IDevicePort {

        ChannelDirection Direction { get; }

        string Name { get; }

        int Samplerate { get; set; }

        Guid UniqueId { get; }

        int Id { get; }

        IDevice Owner { get; }

        DevicePortStatus Status { get; set; }

        void Serialize(XmlWriter xml);

        void Deserialize(XmlNode node);

    }

    public interface IDevicePortInput : IDevicePort {

        event BufferReady OnBufferReady;

        event SamplerateChangedHandler SamplerateChanged;

    }

    public interface IDevicePortOutput : IDevicePort {

        void Write(double[] data, int offset, int samples);

    }

}
