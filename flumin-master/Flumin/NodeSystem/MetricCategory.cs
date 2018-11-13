using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleADC.NodeSystem {

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    class MetricAttribute : Attribute {

        public string Category { get; }

        public string Name { get; }

        public MetricAttribute(string name, string category) {
            Name     = name;
            Category = category;
        }
    }

}
