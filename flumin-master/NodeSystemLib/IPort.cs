namespace NodeSystemLib {
    public enum PortDirection {
        Input, Output
    }

    public enum PortDataType {
        Array, Value, FFT, Event
    }

    public interface IPort {
        string Name { get; }
        Node Parent { get; set; }
        PortDirection Direction { get; }
        PortDataType DataType { get; }
    }
}
