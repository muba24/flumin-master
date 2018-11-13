using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic.NodeAttributes {

    public abstract class TypedNodeAttribute<T> : NodeAttribute {

        private T _value;

        private Func<bool> _isRunning;

        protected TypedNodeAttribute(IAttributable parent, string name) : base(parent, name) {}

        protected TypedNodeAttribute(IAttributable parent, string name, string unit) : base(parent, name, unit) { }

        public override Type GetValueType() => typeof(T);

        public override object Get() => _value;

        public T TypedGet() => _value;

        public void Set(T val) {
            if (Validate(ref val)) {
                _value = val;
                OnChanged(new AttributeChangedEventArgs(this));
            }
        }

        public void SetSilent(T val) {
            if (Validate(ref val)) {
                _value = val;
            }
        }

        public override void Set(object val) {
            T typedVal;

            if (!Enabled) return;
            if (!RuntimeSettable && Parent.IsRunning) return;
            if (!(val is T)) {
                object obj = null;
                try {
                    obj = Convert.ChangeType(val, typeof(T));                    
                } catch (InvalidCastException) {
                    throw new TypeMismatchException();
                }
                if (obj == null) {
                    throw new TypeMismatchException();
                }
                typedVal = (T)obj;
            } else {
                typedVal = (T)val;

            }

            if (Validate(ref typedVal)) {
                _value = typedVal;
                OnChanged(new AttributeChangedEventArgs(this));
            }
        }

        public void SetRuntimeReadonly() {
            RuntimeSettable = false;
        }

        protected abstract bool Validate(ref T value);

    }

    public abstract class NodeAttribute {

        public event EventHandler<AttributeChangedEventArgs> Changed;

        public bool RuntimeSettable { get; protected set; }
        public bool Visible { get; protected set; }
        public bool Enabled { get; protected set; }
        public string Name { get; protected set; }
        public IAttributable Parent { get; }
        public string Unit { get; protected set; }

        protected NodeAttribute(IAttributable parent, string name, string unit) : this(parent, name) {
            Unit = unit;
        }

        protected NodeAttribute(IAttributable parent, string name) {
            RuntimeSettable = true;
            Enabled         = true;
            Visible         = true;
            Parent          = parent;
            Name            = name;
            parent.AddAttribute(this);
        }

        public bool Editable => Enabled && (RuntimeSettable || !Parent.IsRunning);

        public abstract object Get();
        public abstract void Set(object val);
        public abstract Type GetValueType();
        public abstract string Serialize();
        public abstract void Deserialize(string value);

        public T GetAs<T>() => (T)Get();

        protected void OnChanged(AttributeChangedEventArgs e) {
            Changed?.Invoke(this, e);
        }

    }

}
