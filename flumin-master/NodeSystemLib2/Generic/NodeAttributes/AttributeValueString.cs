using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic.NodeAttributes {
    public class AttributeValueString : TypedNodeAttribute<string> {

        public AttributeValueString(IAttributable parent, string name) : base(parent, name) {
        }

        public AttributeValueString(IAttributable parent, string name, string val) : base(parent, name) {
            Set(val);
        }
        
        public void SetReadOnly() {
            Enabled = false;
        }

        public override string Serialize() {
            return TypedGet();
        }

        public override void Deserialize(string value) {
            Set(value);
        }

        protected override bool Validate(ref string value) {
            return value != null;
        }

    }
}
