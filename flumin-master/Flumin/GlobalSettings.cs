using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DeviceLibrary;
using EC = NodeEditorLib.EditorControl;
using WeifenLuo.WinFormsUI.Docking;
using NodeSystemLib2;
using System.Drawing;
using NodeSystemLib2.Generic;

namespace Flumin {

    public class GraphMessage : LogMessage {
        public Graph Graph;

        public GraphMessage(Graph g, LogType type, string description) : base(type, description) {
            Graph = g;
        }
    }

    public class NodeMessage : LogMessage {
        public Node Node;

        public NodeMessage(Node node, LogType type, string description) : base(type, description) {
            Node = node;
        }
    }

    public class FormMessage : LogMessage {
        public System.Windows.Forms.Form Form;

        public FormMessage(System.Windows.Forms.Form form, LogType type, string description) : base(type, description) {
            Form = form;
        }
    }

    public class LogMessage {
        public enum LogType {
            Info, Warning, Error
        }

        public DateTime Date { get; }
        public string Description { get; }
        public LogType Type { get; }

        public LogMessage(LogType type, string description) {
            Date        = DateTime.Now;
            Description = description;
            Type        = type;
        }
    }

    public class Recording {
        public DateTime Date { get; }
        public TimeStamp Begin { get; }
        public TimeStamp End { get; }
        public string Filename;
        public int Samplerate;

        public Recording(string file, int samplerate, TimeStamp begin, TimeStamp end) {
            Samplerate = samplerate;
            Date = DateTime.Now;
            Filename = file;
            Begin = begin;
            End = end;
        }
    }

    public class GlobalSettings {

        /////////////////////////////////////////////////////////////////
        /// SINGLETON

        private static GlobalSettings _instance;

        public static GlobalSettings Instance => _instance ?? (_instance = new GlobalSettings());

        /////////////////////////////////////////////////////////////////

        private NodeEditor _activeEditor;

        private Dictionary<NodeSystemLib2.PortDataType, EC.PortDataType> _portDataTypes = new Dictionary<PortDataType, EC.PortDataType>();

        private Dictionary<Graph, NodeSystemLib2.FileFormats.RecordSet> _activeRecordSets;

        public DockPanel DockPanelInstance { get; set; }

        public IReadOnlyDictionary<Graph, NodeSystemLib2.FileFormats.RecordSet> ActiveRecordSets => _activeRecordSets;

        public ThreadSafeObservableCollection<LogMessage> UserLog;

        public ThreadSafeObservableCollection<Recording> Recordings;

        public event EventHandler<object> SelectedObjectChanged;

        public event EventHandler<NodeEditor> ActiveEditorChanged;

        public int BufferSizeMilliseconds { get; set; } = 100;

        public int RingBufferSizeMilliseconds { get; set; } = 1000;

        public int BufferSize(int samplerate) => (int)(((long)BufferSizeMilliseconds * samplerate) / 1000);

        public int RingBufferSize(int samplerate) => (int)(((long)RingBufferSizeMilliseconds * samplerate) / 1000);

        //public ThreadSafeObservableCollection<MetricManager.MetricInfo> Metrics;
        public MetricManager MetricManager;

        private GlobalSettings() {
            _activeRecordSets = new Dictionary<Graph, NodeSystemLib2.FileFormats.RecordSet>();

            UserLog     = new ThreadSafeObservableCollection<LogMessage>();
            //Metrics    = new ThreadSafeObservableCollection<MetricManager.MetricInfo>();
            MetricManager = new MetricManager();
            Recordings = new ThreadSafeObservableCollection<Recording>();

            Recordings.CollectionChanged += Recordings_CollectionChanged;

            _portDataTypes.Add(PortDataTypes.TypeIdValueDouble, new NodeDataTypeValue());
            _portDataTypes.Add(PortDataTypes.TypeIdSignal1D, new NodeDataTypeArray());
            _portDataTypes.Add(PortDataTypes.TypeIdFFT, new NodeDataTypeFFT());
        }

        private object _addRecLock = new object();
        private void Recordings_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add) {
                lock (_addRecLock) {
                    foreach (var record in e.NewItems.OfType<Recording>()) {
                        var path      = System.IO.Path.GetDirectoryName(record.Filename);
                        var file      = System.IO.Path.GetFileName(record.Filename);
                        var indexFile = System.IO.Path.Combine(path, "index.lst");

                        using (var writer = System.IO.File.AppendText(indexFile)) {
                            writer.WriteLine($"{file}\t{record.Begin.ToShortTimeString()}\t{record.End.ToShortTimeString()}");
                        }
                    }
                }
            }
        }

        public NodeEditor ActiveEditor
        {
            get
            {
                return _activeEditor;
            }
            set
            {
                _activeEditor = value;
                ActiveEditorChanged?.Invoke(this, _activeEditor);
            }
        }

        public Graph RunningGraph => DockPanelInstance.Documents.OfType<NodeEditor>().FirstOrDefault(d => d.GraphRunning)?.Graph;

        public Dictionary<PortDataType, EC.PortDataType> NodeDataTypes => _portDataTypes;

        private volatile bool _stopping = false;
        public void StopProcessing(bool asynchronous = true) {
            if (_stopping) return;
            _stopping = true;

            var action = new Action(() => {
                DockPanelInstance.Documents.OfType<NodeEditor>().FirstOrDefault(d => d.GraphRunning)?.StopGraph();
                _stopping = false;
            });

            if (asynchronous) {
                DockPanelInstance.BeginInvoke(action);
            } else {
                DockPanelInstance.Invoke(action);
            }
        }

        public void SetSelectedObject(object sender, object obj) {
            SelectedObjectChanged?.Invoke(sender, obj);
        }

        public void RegisterGraph(Graph g) {
            _activeRecordSets.Add(g, new NodeSystemLib2.FileFormats.RecordSet(g));
        }

        public void UnregisterGraph(Graph g) {
            _activeRecordSets.Remove(g);
        }

    }

    //******************************************************
    // Darstellung der Datentypen im Graphen

    //public class NodeDataTypeEvent : EC.PortDataType {
    //    public override string ToString() => Name;
    //    public NodeDataTypeEvent() : base(NodeSystemLib2.PortDataTypes., "Event", Color.FromArgb(200, 105, 122)) { }
    //}

    public class NodeDataTypeFFT : EC.PortDataType {
        public override string ToString() => Name;
        public NodeDataTypeFFT() : base(PortDataTypes.TypeIdFFT.Type, "FFT", Color.FromArgb(105, 122, 200)) { }
    }

    public class NodeDataTypeArray : EC.PortDataType {
        public override string ToString() => Name;
        public NodeDataTypeArray() : base(PortDataTypes.TypeIdSignal1D.Type, "Array", Color.FromArgb(155, 192, 104)) { }
    }

    public class NodeDataTypeValue : EC.PortDataType {
        public override string ToString() => Name;
        public NodeDataTypeValue() : base(PortDataTypes.TypeIdValueDouble.Type, "Value", Color.FromArgb(255, 172, 54)) { }
    }
}