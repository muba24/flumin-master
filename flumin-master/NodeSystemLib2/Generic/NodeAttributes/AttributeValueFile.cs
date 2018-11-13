using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic.NodeAttributes {

    public class AttributeValueFile : TypedNodeAttribute<string> {
        public AttributeValueFile(IAttributable parent, string name, bool mustExist) : base(parent, name) {
            MustExist = mustExist;
        }

        public bool MustExist { get; }

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
            return value != null && (!MustExist || (MustExist && System.IO.File.Exists(value)));
        }
    }

}
