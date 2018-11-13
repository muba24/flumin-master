using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FluPy {
    public static class PyDll {

#if false
        const string PythonDll = "python35_d.dll";
#else
        const string PythonDll = "python35.dll";
#endif

        const string WrapDll = "PythonWrap.dll";

        [DllImport(PythonDll, CallingConvention=CallingConvention.Cdecl)]
        public static extern void Py_SetPythonHome(IntPtr str);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Py_GetPythonHome();

        [DllImport(PythonDll, CallingConvention=CallingConvention.Cdecl)]
        public static extern void Py_InitializeEx(int initSigs);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyRun_SimpleString([MarshalAs(UnmanagedType.LPStr)] string command);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyRun_StringFlags([MarshalAs(UnmanagedType.LPStr)] string command, int start, IntPtr globals, IntPtr locals, IntPtr compiler_flags);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyDict_New();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dict"></param>
        /// <returns>new reference</returns>
        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyDict_Keys(IntPtr dict);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dict"></param>
        /// <returns>new reference</returns>
        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyDict_Values(IntPtr dict);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dict"></param>
        /// <returns>new reference</returns>
        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyDict_Items(IntPtr dict);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyDict_Size(IntPtr dict);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyDict_SetItemString(IntPtr pyDict, [MarshalAs(UnmanagedType.LPStr)] string key, IntPtr pyItem);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pyDict"></param>
        /// <param name="key"></param>
        /// <param name="pyItem"></param>
        /// <returns>Borrowed reference</returns>
        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyDict_GetItemString(IntPtr pyDict, [MarshalAs(UnmanagedType.LPStr)] string key);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pyDict"></param>
        /// <param name="key"></param>
        /// <param name="pyItem"></param>
        /// <returns>Borrowed reference</returns>
        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyDict_GetItem(IntPtr pyDict, IntPtr key);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyTuple_New(int len);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyList_New(int len);

        [DllImport(WrapDll, EntryPoint ="PyList_MyCheck", CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyList_Check(IntPtr list);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyList_Size(IntPtr list);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyList_SetItem(IntPtr list, int index, IntPtr obj);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyList_Append(IntPtr list, IntPtr item);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <param name="index"></param>
        /// <returns>borrowed reference</returns>
        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyList_GetItem(IntPtr list, int index);

        [DllImport(WrapDll, EntryPoint = "PyBytes_MyCheck", CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyBytes_Check(IntPtr obj);

        [DllImport(WrapDll, EntryPoint = "PyByteArray_MyCheck", CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyByteArray_Check(IntPtr obj);

        [DllImport(WrapDll, EntryPoint = "PyUnicode_MyCheck", CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyUnicode_Check(IntPtr obj);

        [DllImport(WrapDll, EntryPoint = "PyComplex_MyCheck", CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyComplex_Check(IntPtr obj);

        [DllImport(WrapDll, EntryPoint = "PyBool_MyCheck", CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyBool_Check(IntPtr obj);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern long PyLong_AsLong(IntPtr obj);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyBool_FromLong(int value);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyLong_FromLong(long value);

        [DllImport(WrapDll, EntryPoint = "PyLong_MyCheck", CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyLong_Check(IntPtr obj);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyFloat_FromDouble(double value);

        [DllImport(WrapDll, EntryPoint = "PyFloat_MyCheck", CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyFloat_Check(IntPtr obj);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern double PyFloat_AsDouble(IntPtr obj);

        [DllImport(WrapDll, EntryPoint = "PyTuple_MyCheck", CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyTuple_Check(IntPtr obj);

        [DllImport(WrapDll, EntryPoint = "PyDict_MyCheck", CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyDict_Check(IntPtr obj);

        [DllImport(WrapDll, EntryPoint = "PyFunction_MyCheck", CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyFunction_Check(IntPtr obj);

        [DllImport(WrapDll, EntryPoint = "PyCodeObject_MyCheck", CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyCode_Check(IntPtr obj);

        [DllImport(WrapDll, EntryPoint = "PyModule_MyCheck", CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyModule_Check(IntPtr obj);

        [DllImport(WrapDll, EntryPoint = "PyArray_MyCheck", CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyArray_Check(IntPtr obj);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void init_numpy();

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyImport_ImportModule([MarshalAs(UnmanagedType.LPStr)]string name);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyTuple_SetItem(IntPtr tuple, int pos, IntPtr obj);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyTuple_GetItem(IntPtr tuple, int pos);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_GetItem(IntPtr obj, IntPtr key);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_CallObject(IntPtr obj, IntPtr args);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_GetAttrString(IntPtr obj, [MarshalAs(UnmanagedType.LPStr)] string attr);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyObject_IsTrue(IntPtr obj);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyObject_Repr(IntPtr obj);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyUnicode_FromString([MarshalAs(UnmanagedType.LPStr)] string str);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyUnicode_AsEncodedString(IntPtr pyObj, [MarshalAs(UnmanagedType.LPStr)]string encoding, [MarshalAs(UnmanagedType.LPStr)]string errors);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyBytes_AsString(IntPtr pyBytes);

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyThreadState_Get();

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyErr_Occurred();

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyErr_Print();

        [DllImport(PythonDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Py_InitModule([MarshalAs(UnmanagedType.LPStr)]string name, [MarshalAs(UnmanagedType.LPArray)]PyMethodDef[] defs);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr IncRef(IntPtr obj);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DecRef(IntPtr obj);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetError(IntPtr context);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetErrorLine(IntPtr context);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetErrorOffset(IntPtr context);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CallFunc(IntPtr context, IntPtr func, [MarshalAs(UnmanagedType.LPArray)]IntPtr[] args, int argc);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool HasError(IntPtr context);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetLocalsFromContext(IntPtr context);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetGlobalsFromContext(IntPtr context);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr create_module([MarshalAs(UnmanagedType.LPStr)]string name, [MarshalAs(UnmanagedType.LPArray)]PyMethodDef[] defs, int defCount, [MarshalAs(UnmanagedType.SysInt)]out IntPtr moduleDef);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CompileCode([MarshalAs(UnmanagedType.LPArray)] byte[] code, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] modules, int moduleCount);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyTuple_GetSize(IntPtr obj);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyArray_Create([MarshalAs(UnmanagedType.LPArray)]int[] values, int count, PyArray.Type type = PyArray.Type.NPY_INT);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyArray_Create([MarshalAs(UnmanagedType.LPArray)]double[] values, int count, PyArray.Type type = PyArray.Type.NPY_DOUBLE);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyArray_Create([MarshalAs(UnmanagedType.LPArray)]float[] values, int count, PyArray.Type type = PyArray.Type.NPY_FLOAT);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyArray_CreateEmpty(int count, PyArray.Type type);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyArray_GetPointer(IntPtr obj);

        [DllImport(WrapDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyArray_GetSize(IntPtr obj);

        [StructLayout(LayoutKind.Sequential)]
        public struct PyMethodDef {
            [MarshalAs(UnmanagedType.SysInt)]
            public IntPtr ml_name;

            [MarshalAs(UnmanagedType.SysInt)]
            public IntPtr ml_meth;

            public int ml_flags;

            [MarshalAs(UnmanagedType.SysInt)]
            public IntPtr ml_doc;
        }

        public const int METH_VARARGS  = 0x0001;
        public const int METH_KEYWORDS = 0x0002;

        public static void Initialize(string pythonPath) {
            var ptrPath = Marshal.StringToHGlobalUni(pythonPath);
            Py_SetPythonHome(ptrPath);
            Py_InitializeEx(1);
            PyImport_ImportModule("numpy");
            init_numpy();
        }

    }
}
