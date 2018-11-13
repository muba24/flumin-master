#undef _DEBUG
#include <Python.h>
#include <frameobject.h>
#include <string>
#include <numpy/arrayobject.h>

struct module_state {
	PyObject *error;
};

#define EXPORTABLE extern "C" __declspec(dllexport)

#define GETSTATE(m) ((struct module_state*)PyModule_GetState(m))

static int myextension_traverse(PyObject *m, visitproc visit, void *arg) {
	Py_VISIT(GETSTATE(m)->error);
	return 0;
}

static int myextension_clear(PyObject *m) {
	Py_CLEAR(GETSTATE(m)->error);
	return 0;
}

EXPORTABLE PyObject* create_module(char* module_name, PyMethodDef* methods, int methodCount, PyModuleDef** defOut) {
	PyMethodDef* methodsCopy = new PyMethodDef[methodCount + 1];
	char* name_copy = strdup(module_name);

	for (int i = 0; i < methodCount; i++) {
		methodsCopy[i].ml_name = strdup(methods[i].ml_name);
		methodsCopy[i].ml_meth = methods[i].ml_meth;
		methodsCopy[i].ml_flags = methods[i].ml_flags;
		methodsCopy[i].ml_doc = methods[i].ml_doc != NULL ? strdup(methods[i].ml_doc) : NULL;
	}

	methodsCopy[methodCount].ml_name = NULL;
	methodsCopy[methodCount].ml_meth = NULL;
	methodsCopy[methodCount].ml_flags = 0;
	methodsCopy[methodCount].ml_doc = NULL;

	struct PyModuleDef* moduledef = new PyModuleDef();
	memset(moduledef, 0, sizeof(*moduledef));

	moduledef->m_name = name_copy;
	moduledef->m_methods = methodsCopy;
	moduledef->m_size = sizeof(struct module_state);
	moduledef->m_traverse = myextension_traverse;
	moduledef->m_clear = myextension_clear;

	if (defOut != NULL) {
		*defOut = moduledef;
	}

	PyObject *module = PyModule_Create(moduledef);

	PyImport_AddModule(name_copy);
	PyObject* sys_modules = PyImport_GetModuleDict();
	PyDict_SetItemString(sys_modules, name_copy, module);

	return module;
}

EXPORTABLE void init_numpy() {
	_import_array();
}

EXPORTABLE void IncRef(PyObject* obj) {
	Py_XINCREF(obj);
}

EXPORTABLE void DecRef(PyObject* obj) {
	Py_XDECREF(obj);
}

struct PyContext {
	PyObject* globals;
	PyObject* locals;

	std::string error;
	int error_line;
	int error_offset;
	bool is_error;
};

EXPORTABLE int HasError(PyContext* context) { return context->is_error; }

EXPORTABLE const char* GetError(PyContext* context) { return context->error.c_str(); }
EXPORTABLE int GetErrorLine(PyContext* context) { return context->error_line; }
EXPORTABLE int GetErrorOffset(PyContext* context) { return context->error_offset; }


EXPORTABLE int PyLong_MyCheck(PyObject* obj) {
	return PyLong_Check(obj);
}

EXPORTABLE int PyBytes_MyCheck(PyObject* obj) {
	return PyBytes_Check(obj);
}

EXPORTABLE int PyByteArray_MyCheck(PyObject* obj) {
	return PyByteArray_Check(obj);
}

EXPORTABLE int PyFloat_MyCheck(PyObject* obj) {
	return PyFloat_Check(obj);
}

EXPORTABLE int PyComplex_MyCheck(PyObject* obj) {
	return PyComplex_Check(obj);
}

EXPORTABLE int PyList_MyCheck(PyObject* obj) {
	return PyList_Check(obj);
}

EXPORTABLE int PyDict_MyCheck(PyObject* obj) {
	return PyDict_Check(obj);
}

EXPORTABLE int PyUnicode_MyCheck(PyObject* obj) {
	return PyUnicode_Check(obj);
}

EXPORTABLE int PyTuple_MyCheck(PyObject* obj) {
	return PyTuple_Check(obj);
}

EXPORTABLE int PyFunction_MyCheck(PyObject* obj) {
	return PyFunction_Check(obj);
}

EXPORTABLE int PyBool_MyCheck(PyObject* obj) {
	return PyBool_Check(obj);
}

EXPORTABLE int PyModule_MyCheck(PyObject* obj) {
	return PyModule_Check(obj);
}

EXPORTABLE int PyCodeObject_MyCheck(PyObject* obj) {
	return PyCode_Check(obj);
}

EXPORTABLE int PyTuple_GetSize(PyObject* obj) {
	return PyTuple_GET_SIZE(obj);
}

EXPORTABLE int PyArray_MyCheck(PyObject* obj) {
	return PyArray_Check(obj);
}

EXPORTABLE void* PyArray_GetPointer(PyObject* obj) {
	return PyArray_DATA(obj);
}

EXPORTABLE int PyArray_GetSize(PyObject* obj) {
	return PyArray_SIZE(obj);
}

EXPORTABLE PyObject* PyArray_CreateEmpty(int size, int type) {
	npy_int dim[1] = { size };
	auto array = PyArray_SimpleNew(1, dim, type);
	auto data = (int*)PyArray_DATA(array);
	
	memset(data, '\0', PyArray_NBYTES(array));

	return array;
}

EXPORTABLE PyObject* PyArray_Create(void* values, int size, int type) {
	npy_int dim[1] = { size };
	auto array = PyArray_SimpleNew(1, dim, type);
	auto data = (int*)PyArray_DATA(array);
	auto elemSize = PyArray_ITEMSIZE(array);

	memcpy(data, values, size * elemSize);

	return array;
}

EXPORTABLE PyObject* GetLocalsFromContext(PyContext* context) {
	return context->locals;
}

EXPORTABLE PyObject* GetGlobalsFromContext(PyContext* context) {
	return context->globals;
}

EXPORTABLE PyObject* CallFunc(PyContext* context, PyObject* func, PyObject** args, int argc) {
	context->is_error = false;

	PyErr_Clear();

	auto result = PyEval_EvalCodeEx(func, context->globals, context->locals, args, argc, NULL, 0, NULL, 0, NULL, NULL);
	if (PyErr_Occurred()) {
		context->is_error = true;

		PyObject *ptype, *pvalue, *ptraceback;
		PyErr_Fetch(&ptype, &pvalue, &ptraceback);
		PyErr_NormalizeException(&ptype, &pvalue, &ptraceback);

		PyObject *lineObj = PyObject_GetAttrString(pvalue, "lineno");
		if (lineObj) {
			context->error_line = PyLong_AsLong(lineObj);
			Py_DECREF(lineObj);
		}
		else {
			context->error_line = -1;
		}

		PyObject *offsetObj = PyObject_GetAttrString(pvalue, "offset");
		if (offsetObj) {
			context->error_offset = PyLong_AsLong(offsetObj);
			Py_DECREF(offsetObj);
		}
		else {
			context->error_offset = -1;
		}

		PyObject *textObj = PyObject_Repr(pvalue);
		if (textObj) {
			PyObject * temp_bytes = PyUnicode_AsEncodedString(textObj, "utf-8", "strict");
			if (temp_bytes != NULL) {
				auto my_result = PyBytes_AS_STRING(temp_bytes); // Borrowed pointer
				context->error = std::string(my_result);
				Py_DECREF(temp_bytes);
			}
			else {
				context->error = "unknown error";
			}
			Py_DECREF(textObj);
		}
		else {
			context->error = "unknown error";
		}
	}

	return result;
}

EXPORTABLE PyContext* CompileCode(char* code, PyObject** modules, int moduleCount) {
	auto context = new PyContext();
	context->is_error = false;

	auto moduleDict = PyImport_GetModuleDict();
	auto moduleNumPy = PyDict_GetItemString(moduleDict, "numpy");

	auto globals = PyDict_New();
	auto builtins = PyThreadState_GET()->interp->builtins;
	PyDict_SetItemString(globals, "__builtins__", builtins);
	PyDict_SetItemString(globals, "numpy", moduleNumPy);

	for (int i = 0; i < moduleCount; i++) {
		auto name = PyModule_GetName(modules[i]);
		PyDict_SetItemString(globals, name, modules[i]);
	}

	auto locals = PyDict_New();

	PyErr_Clear();
	PyRun_String(code, Py_file_input, globals, globals);

	if (PyErr_Occurred()) {
		context->is_error = true;

		PyObject *ptype, *pvalue, *ptraceback;
		PyErr_Fetch(&ptype, &pvalue, &ptraceback);
		PyErr_NormalizeException(&ptype, &pvalue, &ptraceback);
		
		PyObject *lineObj = PyObject_GetAttrString(pvalue, "lineno");
		if (lineObj) {
			context->error_line = PyLong_AsLong(lineObj);
			Py_DECREF(lineObj);
		} else {
			context->error_line = -1;
		}

		PyObject *offsetObj = PyObject_GetAttrString(pvalue, "offset");
		if (offsetObj) {
			context->error_offset = PyLong_AsLong(offsetObj);
			Py_DECREF(offsetObj);
		}
		else {
			context->error_offset = -1;
		}

		PyObject *textObj = PyObject_Repr(pvalue);
		if (textObj) {
			PyObject * temp_bytes = PyUnicode_AsEncodedString(textObj, "utf-8", "strict");
			if (temp_bytes != NULL) {
				auto my_result = PyBytes_AS_STRING(temp_bytes); // Borrowed pointer
				context->error = std::string(my_result);
				Py_DECREF(temp_bytes);
			} else {
				context->error = "unknown error";
			}
			Py_DECREF(textObj);
		} else {
			context->error = "unknown error";
		}

		return context;
	}

	context->locals = locals;
	context->globals = globals;
	context->is_error = false;

	return context;
}
