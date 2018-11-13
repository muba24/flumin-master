using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FluPy.PyDll;

namespace FluPy {

    public class PyTuple : IDisposable {

        PyObject _pyObject;

        public PyTuple(int size, bool manageReference = true) {
            _pyObject = new PyObject(PyTuple_New(size), manageReference);
        }

        public PyTuple(PyObject pyObj) {
            System.Diagnostics.Debug.Assert(pyObj.GetObjectType() == PyObject.Type.Tuple);
            _pyObject = pyObj;
        }

        public PyObject Object => _pyObject;

        public int Length => PyTuple_GetSize(_pyObject.Handle);

        public void Set(int index, PyObject obj) {
            PyTuple_SetItem(_pyObject.Handle, index, obj.Handle);
        }

        public PyObject Get(int index) {
            return new BorrowedPyObject(PyTuple_GetItem(_pyObject.Handle, index));
        }

        public void Dispose() {
            _pyObject.Dispose();
        }
    }

}
