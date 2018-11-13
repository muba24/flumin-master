using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static FluPy.PyDll;

namespace FluPy {

    public class PyArray : IDisposable {

        public enum Type {
            NPY_BOOL = 0,
            NPY_BYTE, NPY_UBYTE,
            NPY_SHORT, NPY_USHORT,
            NPY_INT, NPY_UINT,
            NPY_LONG, NPY_ULONG,
            NPY_LONGLONG, NPY_ULONGLONG,
            NPY_FLOAT, NPY_DOUBLE, NPY_LONGDOUBLE,
            NPY_CFLOAT, NPY_CDOUBLE, NPY_CLONGDOUBLE,
            NPY_OBJECT = 17,
            NPY_STRING, NPY_UNICODE,
            NPY_VOID,
            /*
             * New 1.6 types appended, may be integrated
             * into the above in 2.0.
             */
            NPY_DATETIME, NPY_TIMEDELTA, NPY_HALF,

            NPY_NTYPES,
            NPY_NOTYPE,
            NPY_CHAR
        }

        private PyObject _pyObject;

        public PyArray(int[] values, bool manageRef = true) {
            _pyObject = new PyObject(PyArray_Create(values, values.Length), manageRef);
        }

        public PyArray(double[] values, bool manageRef = true) {
            _pyObject = new PyObject(PyArray_Create(values, values.Length), manageRef);
        }

        public PyArray(int size, Type type, bool manageRef = true) {
            _pyObject = new PyObject(PyArray_CreateEmpty(size, type), manageRef);
        }

        public PyArray(PyObject pyObj) {
            System.Diagnostics.Debug.Assert(pyObj.GetObjectType() == PyObject.Type.Array);
            _pyObject = pyObj;
        }

        public int Length => PyArray_GetSize(_pyObject.Handle);

        public PyObject Object => _pyObject;

        public int[] ToArrayInt() {
            var dst = new int[Length];
            var ptr = PyArray_GetPointer(_pyObject.Handle);

            unsafe {
                int* p = (int*)ptr;
                for (int i = 0; i < dst.Length; i++) {
                    dst[i] = *p++; 
                }
            }

            return dst;
        }

        public double[] ToArrayDouble() {
            var dst = new double[Length];
            var ptr = PyArray_GetPointer(_pyObject.Handle);

            unsafe
            {
                double* p = (double*)ptr;
                for (int i = 0; i < dst.Length; i++) {
                    dst[i] = *p++;
                }
            }

            return dst;
        }

        public void Dispose() {
            _pyObject.Dispose();
        }

    }

}
