using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib {

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MetricAttribute : Attribute {

        public string Category { get; }

        public string Name { get; }

        public bool OnlyOnePerGraph { get; }

        public bool Instantiable { get; }

        public MetricAttribute(string name, string category, bool instantiable = true, bool uniqueInGraph = false) {
            Name            = name;
            Category        = category;
            Instantiable    = instantiable;
            OnlyOnePerGraph = uniqueInGraph;
        }
    }

}
