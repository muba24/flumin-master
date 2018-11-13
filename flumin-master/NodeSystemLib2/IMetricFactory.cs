using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NodeSystemLib2 {
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
