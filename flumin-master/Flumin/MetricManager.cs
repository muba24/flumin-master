using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib2;
using System.Xml;

namespace Flumin {

    public class MetricManager {

        public class MetricInfo {
            private Func<Graph, Node> Factory { get; }
            private Func<Graph, XmlNode, Node> FactoryXml { get; }

            public string Name { get; }
            public string Category { get; }
            public string UniqueName { get; }
            public Guid FactoryGuid { get; }

            public MetricInfo(
                string name, 
                string category, 
                string uniqueName, 
                Guid factoryGuid, 
                Func<Graph, Node> factory, 
                Func<Graph, XmlNode, Node> factoryXml
            ) {
                Name = name;
                Category = category;
                Factory = factory;
                FactoryXml = factoryXml;
                UniqueName = uniqueName;
                FactoryGuid = factoryGuid;
            }

            public Node CreateInstance(Graph g) {
                return Factory(g);
            }

            public Node CreateInstance(Graph g, XmlNode reader) {
                return FactoryXml(g, reader);
            }
        }

        public class MetricAddedEventArgs : EventArgs {
            public MetricInfo NewMetric { get; }
            public MetricAddedEventArgs(MetricInfo metric) {
                NewMetric = metric;
            }
        }

        private readonly Guid _genericFactoryGuid = new Guid("60040a7d-a9ad-46cf-9c14-898fdbda0e72");

        private readonly List<IMetricFactory> _factories = new List<IMetricFactory>();

        private readonly List<MetricInfo> _metrics = new List<MetricInfo>();

        public event EventHandler<MetricAddedEventArgs> MetricAdded;

        public IReadOnlyList<MetricInfo> Metrics => _metrics;

        public MetricInfo GetInfoFromUniqueName(string name) => _metrics.FirstOrDefault(i => i.UniqueName == name);

        public IReadOnlyList<IMetricFactory> Factories => _factories;

        /// <summary>
        /// Add the type information of a metric to the metric collection.
        /// This allows the editor to instantiate it.
        /// </summary>
        /// <param name="type">type of the metric</param>
        /// <returns>true if the type is compatible (has a metric attribute and is instantiable)</returns>
        public bool TryRegisterMetric(Type type) {
            MetricAttribute metricAttrib = null;

            try {
                metricAttrib = (MetricAttribute)type.GetCustomAttribute(typeof(MetricAttribute));
            } catch (Exception) {
                return false;
            }
            if (metricAttrib == null) return false;

            if (metricAttrib.Instantiable) {
                var metricInfo = new MetricInfo(
                    metricAttrib.Name,
                    metricAttrib.Category,
                    type.FullName,
                    _genericFactoryGuid,
                    FactoryForType(type),
                    FactoryXmlForType(type)
                );

                _metrics.Add(metricInfo);

                MetricAdded?.Invoke(this, new MetricAddedEventArgs(metricInfo));
            }

            return true;
        }

        public void SaveFactorySettings(Graph g, XmlWriter writer) {
            foreach (var factory in _factories) {
                writer.WriteStartElement("factory");
                writer.WriteAttributeString("guid", factory.Id.ToString());
                factory.SaveInternalState(g, writer);
                writer.WriteEndElement();
            }
        }

        public void FindMetrics(string path) {
            foreach (var dll in System.IO.Directory.GetFiles(path, "*.dll")) {
                Assembly metricAssembly = null;

                try {
                    metricAssembly = Assembly.LoadFile(dll);
                } catch (Exception) {
                    continue;
                }
                if (metricAssembly == null) continue;

                foreach (var type in metricAssembly.ExportedTypes) {
                    if (typeof(IMetricFactory).IsAssignableFrom(type)) {
                        var factory = (IMetricFactory)Activator.CreateInstance(type);
                        UseFactory(factory);
                        _factories.Add(factory);
                        continue;
                    }
                    TryRegisterMetric(type);
                }
            }
        }

        private void UseFactory(IMetricFactory factory) {
            for (int i = 0; i < factory.Count; i++) {
                // this copy is important as the lambda function below
                // would capture i, which equals factory.Count at the end of the loop.
                // Therefore the lambda would always try to create factory item #factory.Count
                // which does not exist.
                // Don't remove the index variable.
                int index = i;

                var info = factory.GetMetricInfo(index);
                var metricInfo = new MetricInfo(
                    info.Name,
                    info.Category,
                    info.UniqueName,
                    factory.Id,
                    g => factory.CreateInstance(index, g),
                    (g, x) => factory.CreateInstance(index, g, x)
                );

                _metrics.Add(metricInfo);

                MetricAdded?.Invoke(this, new MetricAddedEventArgs(metricInfo));
            }
        }

        private static Func<Graph, Node> FactoryForType(Type t) {
            return g => (Node)Activator.CreateInstance(t, g);
        }

        private static Func<Graph, XmlNode, Node> FactoryXmlForType(Type t) {
            return (g, x) => (Node)Activator.CreateInstance(t, x, g);
        }

    }

}
