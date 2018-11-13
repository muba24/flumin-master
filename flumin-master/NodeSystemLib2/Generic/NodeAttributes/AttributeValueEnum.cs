using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic.NodeAttributes {

    // Generic enum tipp from
    // http://www.growingwiththeweb.com/2013/02/using-enum-as-generic-type.html

    public class AttributeValueEnum<TEnum> : TypedNodeAttribute<TEnum> where TEnum : struct, IConvertible, IComparable, IFormattable {

        public AttributeValueEnum(IAttributable parent, string name) : base(parent, name) {
            if (!typeof(TEnum).IsEnum) {
                throw new ArgumentException("TEnum must be an enum.");
            }
        }

        public override string Serialize() {
            return TypedGet().ToString();
        }

        public override void Deserialize(string value) {
            Set((TEnum)Enum.Parse(typeof(TEnum), value));
        }

        protected override bool Validate(ref TEnum value) {
            return true;
        }
    }

}
