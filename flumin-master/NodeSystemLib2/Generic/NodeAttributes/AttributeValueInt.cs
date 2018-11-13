using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic.NodeAttributes {

    public class AttributeValueInt : TypedNodeAttribute<int> {

        public AttributeValueInt(IAttributable parent, string name) : base(parent, name) {
        }

        public AttributeValueInt(IAttributable parent, string name, string unit) : base(parent, name, unit) {
        }

        public AttributeValueInt(IAttributable parent, string name, Func<int, int> transformer) : base(parent, name) {
            Transformer = transformer;
        }

        public AttributeValueInt(IAttributable parent, string name, string unit, Func<int, int> transformer) : base(parent, name, unit) {
            Transformer = transformer;
        }

        public AttributeValueInt(IAttributable parent, string name, int val) : base(parent, name) {
            Set(val);
        }

        public AttributeValueInt(IAttributable parent, string name, string unit, int val) : base(parent, name, unit) {
            Set(val);
        }

        public AttributeValueInt(IAttributable parent, string name, Func<int, int> transformer, int val) : base(parent, name) {
            Transformer = transformer;
            Set(val);
        }

        public AttributeValueInt(IAttributable parent, string name, string unit, Func<int, int> transformer, int val) : base(parent, name, unit) {
            Transformer = transformer;
            Set(val);
        }

        public Func<int, int> Transformer { get; set; }

        public override string Serialize() {
            return TypedGet().ToString();
        }

        protected override bool Validate(ref int value) {
            if (Transformer != null) {
                value = Transformer(value);
            }
            return true;
        }

        public override void Deserialize(string value) {
            Set(int.Parse(value));
        }
    }

}
