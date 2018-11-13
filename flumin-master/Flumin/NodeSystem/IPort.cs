namespace SimpleADC.NodeSystem {
    public enum PortDirection {
        Input, Output
    }

    public enum PortDataType {
        Array, Value, FFT
    }

    public interface IPort {
        string Name { get; }
        Node Parent { get; set; }
        PortDirection Direction { get; }
        PortDataType DataType { get; }
    }
}
