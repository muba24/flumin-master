using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib2.Generic;
using NodeSystemLib2.Generic.NodeAttributes;
using NodeSystemLib2;
using System.Runtime.InteropServices;
using System.Xml;

namespace MetricMatlab {

    [Metric("Matlab", "Other")]
    public class MatlabNode : StateNode<MatlabNode> {

        private class FunctionHeader {
            public string Name;
            public string[] Parameters;
            public string[] ReturnValues;
        }

        private AttributeValueString _attrFuncName;
        private FunctionHeader _funcHeader;
        private MLApp.MLApp _matlab;

        public MatlabNode(XmlNode xml, Graph g) : this(g) {
            _attrFuncName.Deserialize(xml.Attributes.GetNamedItem(_attrFuncName.Name)?.Value ?? "");
        }

        public MatlabNode(Graph g) : base("Matlab", g) {
            _attrFuncName = new AttributeValueString(this, "Scriptname");
            _attrFuncName.Changed += _attrFuncName_Changed;
            _attrFuncName.SetRuntimeReadonly();
        }

        protected override void Serializing(XmlWriter writer) {
            writer.WriteAttributeString(_attrFuncName.Name, _attrFuncName.Serialize());
            base.Serializing(writer);
        }

        private void _attrFuncName_Changed(object sender, AttributeChangedEventArgs e) {
            for (int i = InputPorts.Count - 1; i >= 0; i--) RemovePort(InputPorts[i]);
            for (int i = OutputPorts.Count - 1; i >= 0; i--) RemovePort(OutputPorts[i]);

            if (string.IsNullOrEmpty(_attrFuncName.TypedGet())) return;

            if (_matlab == null) {
                if (!TryFindMatlab()) {
                    Parent.Context.Notify(new GraphNotification(GraphNotification.NotificationType.Warning, "No matlab instance found. Make sure to enable the automation server:\nenableservice('AutomationServer', true)"));
                    return;
                }
            }

            var funcHeader = FindFunction();
            if (funcHeader == null) {
                Parent.Context.Notify(new GraphNotification(GraphNotification.NotificationType.Warning, $"Function {_attrFuncName.TypedGet()} not found or uses unsupported syntax in function header"));
                return;
            }

            foreach (var param in funcHeader.Parameters) {
                if (param.StartsWith("data")) {
                    var port = new NodeSystemLib2.FormatData1D.InputPortData1D(this, param.Substring("data".Length));
                    port.SamplerateChanged += Port_SamplerateChanged;
                } else if (param.StartsWith("value")) {
                    new NodeSystemLib2.FormatValue.InputPortValueDouble(this, param.Substring("value".Length));
                } else {
                    Parent.Context.Notify(new GraphNotification(GraphNotification.NotificationType.Warning, $"Unknown type of port: {param}"));
                }
            }

            foreach (var param in funcHeader.ReturnValues) {
                if (param.StartsWith("data")) {
                    new NodeSystemLib2.FormatData1D.OutputPortData1D(this, param.Substring("data".Length));
                } else if (param.StartsWith("value")) {
                    new NodeSystemLib2.FormatValue.OutputPortValueDouble(this, param.Substring("value".Length));
                } else if (param.StartsWith("used_")) {
                    // used count
                } else {
                    Parent.Context.Notify(new GraphNotification(GraphNotification.NotificationType.Warning, $"Unknown type of return value: {param}"));
                }
            }
        }

        private void Port_SamplerateChanged(object sender, SamplerateChangedEventArgs e) {
            foreach (var port in OutputPorts.OfType<NodeSystemLib2.FormatData1D.OutputPortData1D>()) {
                port.Samplerate = e.NewSamplerate;
            }
        }

        public override bool CanProcess {
            get {
                foreach (var input in InputPorts.OfType<NodeSystemLib2.FormatData1D.InputPortData1D>()) {
                    if (input.Available > 0) return true;
                }
                return false;
            }
        }

        public override bool CanTransfer {
            get {
                foreach (var output in OutputPorts.OfType<NodeSystemLib2.FormatData1D.OutputPortData1D>()) {
                    if (output.Buffer.Available > 0) return true;
                }
                return false;
            }
        }

        public override void PrepareProcessing() {
            if (string.IsNullOrEmpty(_attrFuncName.TypedGet())) {
                throw new Exception("No matlab function specified");
            }

            if (_matlab == null) {
                if (!TryFindMatlab()) throw new Exception("No matlab instance found. Make sure to enable the automation server:\nenableservice('AutomationServer', true)");
            }

            _funcHeader = FindFunction();
            if (_funcHeader == null) throw new Exception($"Function {_attrFuncName.TypedGet()} not found or uses unsupported syntax in function header");

            foreach (var input in InputPorts.OfType<NodeSystemLib2.FormatData1D.InputPortData1D>()) {
                input.PrepareProcessing();
            }

            foreach (var output in OutputPorts.OfType<NodeSystemLib2.FormatData1D.OutputPortData1D>()) {
                output.PrepareProcessing();
            }
        }

        public override void Process() {
            Dictionary<InputPort, double[]> datas = new Dictionary<InputPort, double[]>();

            foreach (var port in InputPorts.OfType<NodeSystemLib2.FormatData1D.InputPortData1D>()) {
                var read = port.Read();
                datas.Add(port, read.Data.Take(read.Available).ToArray());
            }

            object result = null;
            if (datas.Values.Count == 1) {
                _matlab.Feval(_funcHeader.Name, _funcHeader.ReturnValues.Length, out result, datas.Values.ElementAt(0));
            } else if (datas.Values.Count == 2) {
                _matlab.Feval(_funcHeader.Name, _funcHeader.ReturnValues.Length, out result, datas.Values.ElementAt(0), datas.Values.ElementAt(1));
            }

            var data = (double[,])((object[])result)[0];

            ((NodeSystemLib2.FormatData1D.OutputPortData1D)OutputPorts.First()).Buffer.Write(data, 0, data.Length);
        }

        public override void Transfer() {
            foreach (var port in OutputPorts) {
                var data = port as NodeSystemLib2.FormatData1D.OutputPortData1D;
                if (data != null) {
                    data.Transfer();
                    continue;
                }

                var value = port as NodeSystemLib2.FormatValue.OutputPortValueDouble;
                if (value != null) {
                    value.TransferBuffer();
                    continue;
                }
            }
        }

        public override void StartProcessing() {}
        public override void StopProcessing() {}
        public override void SuspendProcessing() {}

        private bool TryFindMatlab() {
            try {
                _matlab = (MLApp.MLApp)Marshal.GetActiveObject("Matlab.Desktop.Application");
            } catch (COMException) {
                _matlab = null;
            }

            return _matlab != null;
        }

        private FunctionHeader FindFunction() {
            if (_matlab == null) return null;

            object matlabPathCurrent;
            _matlab.Feval("pwd", 1, out matlabPathCurrent);
            var currentDirectory = ((string)((object[])matlabPathCurrent)[0]);

            object matlabPaths;
            _matlab.Feval("path", 1, out matlabPaths);
            var directories = ((string)((object[])matlabPaths)[0]).Split(';');

            foreach (var dir in new[] { currentDirectory }.Concat(directories)) {
                var mFiles = System.IO.Directory.GetFiles(dir, "*.m", System.IO.SearchOption.TopDirectoryOnly);
                foreach (var mFile in mFiles) {
                    var filename = System.IO.Path.GetFileNameWithoutExtension(mFile);
                    if (filename != _attrFuncName.TypedGet()) continue;

                    using (System.IO.TextReader reader = System.IO.File.OpenText(mFile)) {
                        while (true) {
                            var line = "";
                            try {
                                line = reader.ReadLine();
                                if (line == null) break;
                            } catch (System.IO.EndOfStreamException) {
                                break;
                            }

                            if (line.StartsWith("%")) continue;
                            if (string.IsNullOrEmpty(line)) continue;
                            if (!line.StartsWith("function")) continue;

                            return ParseFunctionLineTokens(TokenizeFunctionLine(line));
                        }
                    }
                }
            }
            
            return null;
        }

        enum Literal { Name, Operator, End };
        class Token { public Literal Type; public string Content; }

        private List<Token> TokenizeFunctionLine(string line) {
            var literals = new List<Token>();
            var type = Literal.Name;
            var literal = "";
            var i = 0;
            
            while (i < line.Length) {
                if (type == Literal.Name) {
                    while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_')) {
                        literal += line[i++];
                    }
                    literals.Add(new Token { Type = Literal.Name, Content = literal });
                    literal = "";

                } else if (type == Literal.Operator) {
                    literals.Add(new Token { Type = Literal.Operator, Content = line[i++].ToString() });
                    literal = "";

                }

                while (i < line.Length && line[i] == ' ') { i++; }

                if (i < line.Length) {
                    if (line[i] == '%') break;
                    if (char.IsLetterOrDigit(line[i]) || line[i] == '_') type = Literal.Name;
                    else type = Literal.Operator;
                }
            }

            return literals;
        }

        private FunctionHeader ParseFunctionLineTokens(List<Token> tokens) {
            var returnVals = new List<string>();
            var paramVals = new List<string>();
            var index = 0;

            Func<Token> Consume = () => (index < tokens.Count) ? tokens[index++] : new Token { Type = Literal.End };
            Func<Token> Peek = () => (index < tokens.Count) ? tokens[index] : new Token { Type = Literal.End };
            Func<string, bool> Match = (name) => {
                if (index < tokens.Count && tokens[index].Content == name) {
                    index++;
                    return true;
                }
                return false;
            };

            if (!Match("function")) return null;
            if (Match("[")) {
                while (Peek().Type == Literal.Name) {
                    returnVals.Add(Consume().Content);
                    if (!Match(",")) break;
                }
                if (!Match("]")) return null;
            } else {
                if (Peek().Type == Literal.Name) {
                    returnVals.Add(Consume().Content);
                }
            }

            if (!Match("=")) return null;

            if (!(Peek().Type == Literal.Name)) return null;
            var funcName = Consume().Content;

            if (!Match("(")) return null;
            while (Peek().Type == Literal.Name) {
                paramVals.Add(Consume().Content);
                if (!Match(",")) break;
            };
            if (!Match(")")) return null;

            return new FunctionHeader() {
                Name = funcName,
                Parameters = paramVals.ToArray(),
                ReturnValues = returnVals.ToArray()
            };
        }

    }

}
