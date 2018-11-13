using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2 {

    public class PortDataType {

        public Guid Type { get; }
        public string Description { get; }

        public PortDataType(string descr, Guid type) {
            Type        = type;
            Description = descr;
        }

        public override bool Equals(object obj) {
            if (obj != null && obj is PortDataType) {
                var typeInfo = (PortDataType)obj;
                return typeInfo.Type.Equals(Type);
            }
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode() {
            return Type.GetHashCode();
        }

    }
}
