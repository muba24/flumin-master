using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static FluPy.PyDll;

namespace FluPy {

    public class BorrowedPyObject : PyObject {
        public BorrowedPyObject(IntPtr pyObj) : base(pyObj, false) { }
    }

    public class PyObject : IDisposable {

        public enum Type {
            Unknown,
            Long,
            Bool,
            Float,
            Complex,
            Bytes,
            ByteArray,
            Array,
            Unicode,
            Tuple,
            List,
            Dict,
            Function,
            Module,
            Code
        }

        private Type? _objType;

        private IntPtr _pyObject;
        private bool _doDispose;
        private bool _disposed;

        public PyObject(IntPtr pyObj, bool manageReference = true) {
            _pyObject = pyObj;
            _doDispose = manageReference;
        }

        public IntPtr Handle => _pyObject;

        public void AddReference() {
            IncRef(_pyObject);
        }

        public void RemoveReference() {
            DecRef(_pyObject);
        }

        public Type GetObjectType() {
            if (_objType.HasValue) {
                return _objType.Value;
            }

            var typeChecker = new Dictionary<Type, Func<IntPtr, int>>() {
                { Type.Long, PyLong_Check },
                { Type.Float, PyFloat_Check },
                { Type.Bool, PyBool_Check },
                { Type.Unicode, PyUnicode_Check },
                { Type.List, PyList_Check },
                { Type.Dict, PyDict_Check },
                { Type.Function, PyFunction_Check },
                { Type.Tuple, PyTuple_Check },
                { Type.Complex, PyComplex_Check },
                { Type.Bytes, PyBytes_Check },
                { Type.ByteArray, PyByteArray_Check },
                { Type.Module, PyModule_Check },
                { Type.Code, PyCode_Check },
                { Type.Array, PyArray_Check },
                { Type.Unknown, (_) => 1 }
            };

            _objType = typeChecker.First(kv => kv.Value(_pyObject) == 1).Key;
            return _objType.Value;
        }

        public long GetLong() {
            return PyLong_AsLong(_pyObject);
        }

        public double GetDouble() {
            return PyFloat_AsDouble(_pyObject);
        }

        public string GetString() {
            using (var encstr = new PyObject(PyUnicode_AsEncodedString(_pyObject, "utf-8", "strict"), true)) {
                var pStr = PyBytes_AsString(encstr.Handle);
                return StringFromNativeUtf8(pStr);
            }
        }

        public override string ToString() {
            using (var repr = new PyObject(PyObject_Repr(_pyObject), true)) {
                using (var encstr = new PyObject(PyUnicode_AsEncodedString(repr.Handle, "utf-8", "strict"), true)) {
                    var pStr = PyBytes_AsString(encstr.Handle);
                    return StringFromNativeUtf8(pStr);
                }
            }
        }

        public void Dispose() {
            if (!_disposed && _doDispose) {
                DecRef(_pyObject);
                _pyObject = IntPtr.Zero;
            }
            _disposed = true;
        }


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
