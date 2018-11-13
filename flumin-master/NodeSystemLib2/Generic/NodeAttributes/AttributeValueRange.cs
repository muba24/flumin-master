using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic.NodeAttributes {
    abstract class AttributeValueRange<T> : TypedNodeAttribute<T> where T : struct, IComparable<T> {

        public T Max { get; set; }
        public T Min { get; set; }

        protected AttributeValueRange(IAttributable parent, string name) : base(parent, name) {
        }

        protected override bool Validate(ref T value) {
            if (Comparer<T>.Default.Compare(Max, value) > 0) {
                value = Max;
            } else if (Comparer<T>.Default.Compare(Min, value) < 0) {
                value = Min;
            }

            return true;
        }

    }
}
