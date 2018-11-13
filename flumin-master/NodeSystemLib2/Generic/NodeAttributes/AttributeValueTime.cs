using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic.NodeAttributes {

    class AttributeValueTime : TypedNodeAttribute<TimeSpan> {

        public AttributeValueTime(IAttributable parent, string name, Func<bool> running) : base(parent, name) {
        }

        public AttributeValueTime(IAttributable parent, string name, Func<TimeSpan, TimeSpan> transformer) : base(parent, name) {
            Transformer = transformer;
        }

        public AttributeValueTime(IAttributable parent, string name, TimeSpan val) : base(parent, name) {
            Set(val);
        }

        public AttributeValueTime(IAttributable parent, string name, Func<TimeSpan, TimeSpan> transformer, TimeSpan val) : base(parent, name) {
            Transformer = transformer;
            Set(val);
        }

        public Func<TimeSpan, TimeSpan> Transformer { get; set; }

        public override string Serialize() {
            return TypedGet().ToString("o");    // o: culture neutral
        }

        public override void Deserialize(string value) {
            Set(DateTime.Parse(value));
        }

        protected override bool Validate(ref TimeSpan value) {
            if (Transformer != null) {
                value = Transformer(value);
            }
            return true;
        }
    }

}
