using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic.NodeAttributes {

    public class AttributeValueDouble : TypedNodeAttribute<double> {

        public AttributeValueDouble(IAttributable parent, string name) : base(parent, name) {
        }

        public AttributeValueDouble(IAttributable parent, string name, string unit) : base(parent, name, unit) {
        }

        public AttributeValueDouble(IAttributable parent, string name, double val) : base(parent, name) {
            Set(val);
        }

        public AttributeValueDouble(IAttributable parent, string name, string unit, double val) : base(parent, name, unit) {
            Set(val);
        }

        public override string Serialize() {
            return TypedGet().ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public override void Deserialize(string value) {
            Set(double.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
        }

        protected override bool Validate(ref double value) {
            return !(double.IsNaN(value) || double.IsInfinity(value));
        }

    }

}
