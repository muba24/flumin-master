using System.Collections.Generic;
using System.Xml;

namespace DeviceLibrary {
    enum SessionTaskState {
        None,
        Stopped,
        Running
    }

    interface INidaqSessionTask {
        NidaqSingleton.Device Device { get; }
        NidaqSession Parent { get; }
        int TaskHandle { get; }
        IReadOnlyList<INidaqMetric> Nodes { get; }
        SessionTaskState State { get; }

        void CreateTask(IEnumerable<INidaqMetric> nodes);
        void DestroyTask();
        void Start();
        void Stop();
        void LoadFactorySettings();
        void Serialize(XmlWriter writer);
    }
}