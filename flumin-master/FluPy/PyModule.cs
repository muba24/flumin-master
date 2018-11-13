using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static FluPy.PyDll;

namespace FluPy {

    public class PyModule : IDisposable {

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr PyCFunction(IntPtr self, IntPtr args);

        private Dictionary<string, PyCFunction> _functions;
        private PyMethodDef[] _defs;
        private PyObject _module;

        public PyObject Module => _module;

        public PyModule(string name, IDictionary<string, PyCFunction> funcs) {
            _functions = new Dictionary<string, PyCFunction>(funcs);
            var methodDefs = new List<PyMethodDef>();

            foreach (var func in _functions) {
                var methodDef = new PyMethodDef();
                methodDef.ml_name = Marshal.StringToHGlobalAnsi(func.Key);
                methodDef.ml_doc = IntPtr.Zero;
                methodDef.ml_flags = METH_VARARGS;
                methodDef.ml_meth = Marshal.GetFunctionPointerForDelegate(func.Value);
                methodDefs.Add(methodDef);
            }

            _defs = methodDefs.ToArray();
            var ptrModuleDef = new IntPtr();

            var modResult = create_module(name, _defs, _defs.Length, out ptrModuleDef);
            _module = new PyObject(modResult);
        }

        public void Dispose() {
            _module.Dispose();
        }

    }

}
