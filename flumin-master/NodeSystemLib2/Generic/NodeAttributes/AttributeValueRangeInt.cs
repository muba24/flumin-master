using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic.NodeAttributes {

    class AttributeValueRangeInt : AttributeValueRange<int> {

        public AttributeValueRangeInt(IAttributable parent, string name) : base(parent, name) {
        }

        public AttributeValueRangeInt(IAttributable parent, string name, int val) : base(parent, name) {
            Set(val);
        }

        public override string Serialize() {
            return TypedGet().ToString();
        }

        public override void Deserialize(string value) {
            Set(int.Parse(value));
        }
    }

}
