using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NodeSystemLib2.Generic.NodeAttributes;
using NodeSystemLib2.Generic;
using NodeSystemLib2;
using System.Xml;
using FluPy;
using NodeEditorLib.EditorControl;
using WeifenLuo.WinFormsUI.Docking;

namespace MetricPython {

    [Metric("Python Script", "Other")]
    public class MetricPython : StateNode<MetricPython>, INodeUi {

        private const string PythonDir = @"C:\python35";

        private string _code = "";
        private PyContext _ctx;
        private Editor _editor;

        private readonly Dictionary<string, PyModule.PyCFunction> _moduleDef;

        public MetricPython(XmlNode node, Graph g) : this(g) {
            _code = node?.Attributes?.GetNamedItem("code")?.Value ?? "";
            if (!string.IsNullOrWhiteSpace(_code)) {
                ReloadCode();
            }
        }

        protected override void Serializing(XmlWriter writer) {
            writer.WriteAttributeString("code", _code);
            base.Serializing(writer);
        }

        public MetricPython(Graph g) : base("Python", g) {
            PyDll.Initialize(PythonDir);

            _moduleDef = new Dictionary<string, PyModule.PyCFunction>() {
                { "GetSamplerate", GetSamplerate },
                { "SetSamplerate", SetSamplerate },
                { "ReadDataPort", ReadDataPort },
                { "WriteDataPort", WriteDataPort }
            };
        }

        public override bool CanProcess => InputPorts.OfType<NodeSystemLib2.FormatData1D.InputPortData1D>().Any(port => port.Available > 0);
        public override bool CanTransfer => OutputPorts.OfType<NodeSystemLib2.FormatData1D.OutputPortData1D>().Any(port => port.Buffer.Available > 0);

        public override void PrepareProcessing() {
            if (_editor != null && !_editor.IsDisposed && _editor.Edited) {
                _editor.AskForSave();
            }

            if (_ctx == null) {
                throw new Exception("Python code has errors");
            }

            try {
                _ctx.Call("prepare", new PyObject[0]);
            }
            catch (PyException e) {
                DisplayPythonError(e);
                throw;
            }

            foreach (var port in InputPorts.OfType<NodeSystemLib2.FormatData1D.InputPortData1D>()) {
                port.PrepareProcessing();
            }

            foreach (var port in OutputPorts.OfType<NodeSystemLib2.FormatData1D.OutputPortData1D>()) {
                port.PrepareProcessing();
            }
        }

        public override void Process() {
            try {
                _ctx?.Call("process", new PyObject[0]);
            }
            catch (PyException e) {
                DisplayPythonError(e);
                throw;
            }
        }

        public override void StartProcessing() {
            if (_editor != null && !_editor.IsDisposed) {
                _editor.EditorEnabled = false;
            }
        }

        public override void StopProcessing() {
            if (_editor != null && !_editor.IsDisposed) {
                _editor.EditorEnabled = true;
            }
        }

        public override void SuspendProcessing() {
        }

        public override void Transfer() {
            foreach (var port in OutputPorts.OfType<NodeSystemLib2.FormatData1D.OutputPortData1D>()) {
                port.Transfer();
            }
        }

        public void OnLoad(NodeEditorLib.EditorControl.Node node) {}

        public void OnDoubleClick() {
            if (_editor != null && !_editor.IsDisposed) {
                _editor.Show((DockPanel)Parent.Context.DockPanel);
                _editor.EditorEnabled = State == Graph.State.Stopped;
            } else {
                _editor = new Editor();
                _editor.OnReload += Editor_OnReload;
                _editor.Show((DockPanel)Parent.Context.DockPanel);
                _editor.Code = _code;
                _editor.EditorEnabled = State == Graph.State.Stopped;
                _editor.SetSaved();
            }
        }

        private IntPtr GetSamplerate(IntPtr self, IntPtr args) {
            var argTuple = new PyTuple(new PyObject(args, false));
            var portName = argTuple.Get(0).GetString();
            var dataPort = InputPorts.First(port => port.Name == portName) as NodeSystemLib2.FormatData1D.InputPortData1D;

            double samplerate = 0;
            if (dataPort != null) {
                samplerate = dataPort.Samplerate;
            }

            return new PyFloat(samplerate, false).Handle;
        }

        private IntPtr SetSamplerate(IntPtr self, IntPtr args) {
            var argTuple = new PyTuple(new PyObject(args, false));
            var portName = argTuple.Get(0).GetString();
            var samplerate = argTuple.Get(1).GetDouble();
            var dataPort = OutputPorts.First(port => port.Name == portName) as NodeSystemLib2.FormatData1D.OutputPortData1D;

            if (dataPort != null) {
                dataPort.Samplerate = (int)samplerate;
                return new PyLong(1, false).Handle;
            }
            return new PyLong(0, false).Handle;
        }

        private IntPtr ReadDataPort(IntPtr self, IntPtr args) {
            var argTuple = new PyTuple(new PyObject(args, false));
            var portName = argTuple.Get(0).GetString();
            var dataPort = InputPorts.First(port => port.Name == portName) as NodeSystemLib2.FormatData1D.InputPortData1D;

            var buffer = dataPort.Read();
            return new PyArray(buffer.Data.Take(buffer.Available).ToArray(), false).Object.Handle;
        }

        private IntPtr WriteDataPort(IntPtr self, IntPtr args) {
            var argTuple = new PyTuple(new PyObject(args, false));
            var data = new PyArray(argTuple.Get(0));
            var portName = argTuple.Get(1).GetString();
            var dataPort = OutputPorts.First(port => port.Name == portName) as NodeSystemLib2.FormatData1D.OutputPortData1D;

            var result = data.ToArrayDouble();
            dataPort.Buffer.Write(result, 0, result.Length);

            return new PyLong(1, false).Handle;
        }

        private void Editor_OnReload(object sender, Editor.ReloadEventArgs e) {
            if (State != Graph.State.Stopped) return;
            _code = e.Text;
            ReloadCode();

            if (_editor != null && !_editor.IsDisposed) {
                _editor.SetSaved();
            }
        }

        private void ReloadCode() {
            _ctx?.Dispose();
            _ctx = null;

            var mod = new PyModule("Flumin", _moduleDef);

            try {
                _ctx = PyContext.FromCode(_code, new[] { mod });
                DisplayPythonError(null);
            } catch (PyException e) {
                DisplayPythonError(e);
                Parent.Context.Notify(new GraphNotification(this, GraphNotification.NotificationType.Error, "Error in python code: " + e.Message));
            }

            if (_ctx != null) GetPortDefs();
        }

        private void DisplayPythonError(PyException e) {
            if (_editor != null && !_editor.IsDisposed) {
                if (e != null) {
                    _editor.BeginInvoke(new Action(() => {
                        _editor.SetError(e.Message, e.Line > 0 ? e.Line - 1 : e.Line, e.Offset);
                    }));
                }
                else {
                    _editor.BeginInvoke(new Action(() => {
                        _editor.ClearError();
                    }));
                }
            }
        }

        private void GetPortDefs() {
            PyObject callResult = null;

            try {
                callResult = _ctx.Call("init", new PyObject[0]);
                DisplayPythonError(null);
            }
            catch (PyException e) {
                DisplayPythonError(e);
                Parent.Context.Notify(new GraphNotification(this, GraphNotification.NotificationType.Error,
                    $"Python: While executing init function: {e.Message}"));
                return;
            }
            catch (EntryPointNotFoundException e) {
                Parent.Context.Notify(new GraphNotification(this, GraphNotification.NotificationType.Error,
                    $"Python: While executing init function: {e.Message}"));
                return;
            }

            var existingInputs = InputPorts.ToList();
            var existingOutputs = OutputPorts.ToList();

            using (var portDefs = new PyDict(callResult)) {

                using (var portInDefs = new PyList(portDefs["in"])) {
                    foreach (var item in portInDefs) {
                        var tuple = new PyTuple(item);
                        var portName = new PyString(tuple.Get(0).Handle, false).Value;
                        var portType = new PyString(tuple.Get(1).Handle, false).Value;

                        var existingPort = existingInputs.FirstOrDefault(p => p.Name == portName);

                        try {
                            switch (portType) {
                                case "data":
                                    if (existingPort != null) {
                                        if (!existingPort.DataType.Equals(PortDataTypes.TypeIdSignal1D)) {
                                            RemovePort(existingPort);
                                            new NodeSystemLib2.FormatData1D.InputPortData1D(this, portName);
                                        }
                                        existingInputs.Remove(existingPort);
                                    } else {
                                        new NodeSystemLib2.FormatData1D.InputPortData1D(this, portName);
                                    }
                                    break;
                                case "value":
                                    if (existingPort != null) {
                                        if (!existingPort.DataType.Equals(PortDataTypes.TypeIdValueDouble)) {
                                            RemovePort(existingPort);
                                            new NodeSystemLib2.FormatValue.InputPortValueDouble(this, portName);
                                        }
                                        existingInputs.Remove(existingPort);
                                    } else {
                                        new NodeSystemLib2.FormatValue.InputPortValueDouble(this, portName);
                                    }
                                    break;
                                default:
                                    Parent.Context.Notify(new GraphNotification(this, GraphNotification.NotificationType.Error, $"Python: unknown port format '{portType}' for input port named {portName}"));
                                    break;
                            }
                        } catch (Exception e) {
                            Parent.Context.Notify(new GraphNotification(this, GraphNotification.NotificationType.Error, $"Python: error while processing input port: {portName}: {e.Message}"));
                        }
                    }
                }

                foreach (var port in existingInputs) {
                    RemovePort(port);
                }

                using (var portOutDefs = new PyList(portDefs["out"])) {
                    foreach (var item in portOutDefs) {
                        var tuple = new PyTuple(item);
                        var portName = new PyString(tuple.Get(0).Handle, false).Value;
                        var portType = new PyString(tuple.Get(1).Handle, false).Value;

                        var existingPort = existingOutputs.FirstOrDefault(p => p.Name == portName);

                        try {
                            switch (portType)
                            {
                                case "data":
                                    if (existingPort != null) {
                                        if (!existingPort.DataType.Equals(PortDataTypes.TypeIdSignal1D)) {
                                            RemovePort(existingPort);
                                            new NodeSystemLib2.FormatData1D.OutputPortData1D(this, portName);
                                        }
                                        existingOutputs.Remove(existingPort);
                                    } else {
                                        new NodeSystemLib2.FormatData1D.OutputPortData1D(this, portName);
                                    }
                                    break;
                                case "value":
                                    if (existingPort != null) {
                                        if (!existingPort.DataType.Equals(PortDataTypes.TypeIdValueDouble)) {
                                            RemovePort(existingPort);
                                            new NodeSystemLib2.FormatValue.OutputPortValueDouble(this, portName);
                                        }
                                        existingOutputs.Remove(existingPort);
                                    } else {
                                        new NodeSystemLib2.FormatValue.OutputPortValueDouble(this, portName);
                                    }
                                    break;
                                default:
                                    Parent.Context.Notify(new GraphNotification(this, GraphNotification.NotificationType.Error, $"Python: unknown port format '{portType}' for output port named {portName}"));
                                    break;
                            }
                        } catch (Exception e) {
                            Parent.Context.Notify(new GraphNotification(this, GraphNotification.NotificationType.Error, $"Python: error while processing output port: {portName}: {e.Message}"));
                        }
                    }
                }

            }
        }

        public void OnDraw(System.Drawing.Rectangle node, System.Drawing.Graphics e) {}
    }

}
