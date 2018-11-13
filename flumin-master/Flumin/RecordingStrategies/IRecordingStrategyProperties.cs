using System;

namespace Flumin.RecordingStrategies {

    public class RecordingStrategyProperty {

        public readonly string Name;

        private readonly Type _type;

        private object _value;

        public RecordingStrategyProperty(string name, object value, Type type) {
            Name = name;
            _type = type;
            Set(value);
        }

        public static RecordingStrategyProperty Make<T>(string name, T value) {
            return new RecordingStrategyProperty(name, value, typeof(T));
        }

        public Type GetValueType() {
            return _type;
        }

        public object Get() {
            return _value;
        }

        public T GetAs<T>() {
            if (typeof(T) != _type) throw new ArgumentException("Types not compatible. Given type: " + typeof(T) + ", expected type: " + _type);
            return (T) _value;
        }

        // can't check for this as constructor is not generic, so T will always be object
        public void Set<T>(T value) {
            //if (typeof(T) == _type) {
                _value = value;
            //} else {
            //    throw new ArgumentException("Types not compatible. Given type: " + value.GetType() + ", expected type: " + _type);
            //}
        }

    }

}