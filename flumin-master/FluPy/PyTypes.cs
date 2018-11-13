using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static FluPy.PyDll;

namespace FluPy {
    
    public class PyFloat : PyObject {
        public PyFloat(double value, bool manageRef = true) : base(PyFloat_FromDouble(value), manageRef) { }
        public PyFloat(IntPtr pyObj, bool manageRef = true) : base(pyObj, manageRef) {
            if (GetObjectType() != Type.Float) throw new InvalidOperationException("Type of passed PyObject is not float");
        }
        public double Value => PyFloat_AsDouble(Handle);
        public override string ToString() => base.ToString();   
    }

    public class PyLong : PyObject {
        public PyLong(long value, bool manageRef = true) : base(PyLong_FromLong(value), manageRef) { }
        public PyLong(IntPtr pyObj, bool manageRef = true) : base(pyObj, manageRef) {
            if (GetObjectType() != Type.Long) throw new InvalidOperationException("Type of passed PyObject is not long");
        }
        public long Value => PyLong_AsLong(Handle);
        public override string ToString() => base.ToString();
    }

    public class PyBool : PyObject {
        public PyBool(bool value, bool manageRef = true) : base(PyBool_FromLong(value ? 1 : 0), manageRef) { }
        public PyBool(IntPtr pyObj, bool manageRef = true) : base(pyObj, manageRef) {
            if (GetObjectType() != Type.Bool) throw new InvalidOperationException("Type of passed PyObject is not bool");
        }
        public bool Value => PyObject_IsTrue(Handle) == 1;
        public override string ToString() => base.ToString();
    }

    public class PyString : PyObject {
        public PyString(string value, bool manageRef = true) : base(PyUnicode_FromString(value), manageRef) { }
        public PyString(IntPtr pyObj, bool manageRef = true) : base(pyObj, manageRef) {
            if (GetObjectType() != Type.Unicode) throw new InvalidOperationException("Type of passed PyObject is not string");
        }

        public string Value {
            get {
                using (var encstr = new PyObject(PyUnicode_AsEncodedString(Handle, "utf-8", "strict"), true)) {
                    var pStr = PyBytes_AsString(encstr.Handle);
                    return StringFromNativeUtf8(pStr);
                }
            }
        }

        public override string ToString() => base.ToString();

        // http://stackoverflow.com/a/10773988
        private static string StringFromNativeUtf8(IntPtr nativeUtf8) {
            int len = 0;
            while (Marshal.ReadByte(nativeUtf8, len) != 0) ++len;
            byte[] buffer = new byte[len];
            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }
    }

}
