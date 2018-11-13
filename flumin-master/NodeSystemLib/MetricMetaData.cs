using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib {
    public class MetricMetaData {
        public string Name { get; }
        public string Category { get; }
        public string UniqueName { get; }

        public MetricMetaData(string name, string category, string uniqueName) {
            Name       = name;
            Category   = category;
            UniqueName = uniqueName;
        }
    }
}
