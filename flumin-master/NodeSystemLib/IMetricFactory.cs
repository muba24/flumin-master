using System;
using System.Xml;

namespace NodeSystemLib {

    public interface IMetricFactory {

        Guid Id { get; }
        int Count { get; }
        MetricMetaData GetMetricInfo(int index);
        void SetFactorySettings(Graph g, XmlNode factorySettings);
        Node CreateInstance(int index, Graph g);
        Node CreateInstance(int index, Graph g, XmlNode node);
        Node CreateInstance(string uniqueId, Graph g, XmlNode node);
        void SaveInternalState(Graph g, XmlWriter writer);

    }

}
