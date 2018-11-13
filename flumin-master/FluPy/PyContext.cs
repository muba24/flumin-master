using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static FluPy.PyDll;

namespace FluPy {
    public class PyContext : IDisposable {

        private PyDict _locals;
        private PyDict _globals;
        private IntPtr _context;

        private PyContext(IntPtr context) {
            _locals = new PyDict(new BorrowedPyObject(GetLocalsFromContext(context)));
            _globals = new PyDict(new BorrowedPyObject(GetGlobalsFromContext(context)));
            _context = context;
        }

        public PyDict Locals => _locals;
        public PyDict Globals => _globals;

        public PyObject Call(string func, IEnumerable<PyObject> args) {
            var objFunc = Locals[func];
            if (objFunc.Handle.ToInt32() == 0) {
                objFunc = Globals[func];
                if (objFunc.Handle.ToInt32() == 0) {
                    throw new EntryPointNotFoundException("Function not found: " + func);
                }
            }

            return Call(objFunc, args);
        }

        public PyObject Call(PyObject func, IEnumerable<PyObject> args) {
            PyObject code;

            if (func.GetObjectType() != PyObject.Type.Code) {
                if (func.GetObjectType() == PyObject.Type.Function) {
                    code = new BorrowedPyObject(PyObject_GetAttrString(func.Handle, "__code__"));
                } else {
                    throw new PyException("Passed object is not a function or code block", -1, -1);
                }
            } else {
                code = func;
            }

            var argArray = args.Select(arg => arg.Handle).ToArray();

            var result = new PyObject(CallFunc(_context, code.Handle, argArray, argArray.Length));

            if (HasError(_context)) {
                var errorMsg = StringFromNativeUtf8(GetError(_context));
                var line = GetErrorLine(_context);
                var offset = GetErrorOffset(_context);
                throw new PyException(errorMsg, line, offset);
            }

            return result;
        }

        public static PyContext FromCode(string code) {
            return FromCode(code, new PyModule[0]);
        }

        public static PyContext FromCode(string code, IEnumerable<PyModule> modules) {
            var utfCode = "# -*- coding: utf-8 -*-\r\n" + code;
            var utfCodeBytes = Encoding.UTF8.GetBytes(utfCode);

            var moduleHandles = modules.Select(module => module.Module.Handle).ToArray();
            var context = CompileCode(utfCodeBytes, moduleHandles, moduleHandles.Length);

            if (HasError(context)) {
                var errorMsg = StringFromNativeUtf8(GetError(context));
                var line = GetErrorLine(context);
                var offset = GetErrorOffset(context);
                throw new PyException(errorMsg, line, offset);
            }

            return new PyContext(context);
        }

        // http://stackoverflow.com/a/10773988
        private static string StringFromNativeUtf8(IntPtr nativeUtf8) {
            int len = 0;
            while (Marshal.ReadByte(nativeUtf8, len) != 0) ++len;
            byte[] buffer = new byte[len];
            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        public void Dispose() {
            //
        }
    }
}
