using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic.NodeAttributes {

    class AttributeValueRangeDouble : AttributeValueRange<double> {

        public AttributeValueRangeDouble(IAttributable parent, string name) : base(parent, name) {
        }

        public AttributeValueRangeDouble(IAttributable parent, string name, double val) : base(parent, name) {
            Set(val);
        }

        public override string Serialize() {
            return TypedGet().ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public override void Deserialize(string value) {
            Set(double.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
        }

    }

}
