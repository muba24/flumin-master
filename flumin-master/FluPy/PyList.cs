using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FluPy.PyDll;

namespace FluPy {

    public class PyList : IEnumerable<PyObject>, IDisposable {

        PyObject _pyObject;

        public PyList(int size = 0, bool manageReference = true) {
            _pyObject = new PyObject(PyList_New(size), manageReference);
        }

        public PyList(PyObject pyObj) {
            System.Diagnostics.Debug.Assert(pyObj.GetObjectType() == PyObject.Type.List);
            _pyObject = pyObj;
        }

        public PyObject Object => _pyObject;

        public void Add(PyObject item, bool addRef = true) {
            PyList_Append(_pyObject.Handle, item.Handle);
            item.AddReference();
        }

        public int Size => PyList_Size(_pyObject.Handle);

        public PyObject this[int index] {
            get {
                return new BorrowedPyObject(PyList_GetItem(_pyObject.Handle, index));
            }
        }

        public void Dispose() {
            _pyObject.Dispose();
        }

        public IEnumerator<PyObject> GetEnumerator() {
            var currentSize = Size;
            for (int i = 0; i < currentSize; i++) {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
