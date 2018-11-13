using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FluPy.PyDll;

namespace FluPy {

    public class PyDict : IDisposable {

        private PyObject _pyObject;

        public PyDict(bool manageReference = true) {
            _pyObject = new PyObject(PyDict_New(), manageReference);
        }

        public PyDict(PyObject pyObj) {
            System.Diagnostics.Debug.Assert(pyObj.GetObjectType() == PyObject.Type.Dict);
            _pyObject = pyObj;
        }

        public PyObject Object => _pyObject;

        public PyObject this[PyObject key] {
            get {
                return new BorrowedPyObject(PyDict_GetItem(_pyObject.Handle, key.Handle));
            }
        }

        public PyObject this[string key] {
            get {
                return new BorrowedPyObject(PyDict_GetItemString(_pyObject.Handle, key));
            }
        }

        public IEnumerable<PyObject> Keys {
            get {
                using (var lst = new PyList(new PyObject(PyDict_Keys(_pyObject.Handle)))) {
                    foreach (var item in lst) {
                        yield return item;
                    }
                }
            }
        }

        public IEnumerable<PyObject> Values {
            get {
                using (var lst = new PyList(new BorrowedPyObject(PyDict_Values(_pyObject.Handle)))) {
                    foreach (var item in lst) {
                        yield return item;
                    }
                }
            }
        }

        public IEnumerable<PyObject> Items {
            get {
                using (var lst = new PyList(new BorrowedPyObject(PyDict_Items(_pyObject.Handle)))) {
                    foreach (var item in lst) {
                        yield return item;
                    }
                }
            }
        }

        public void Dispose() {
            _pyObject.Dispose();
        }

    }

}
