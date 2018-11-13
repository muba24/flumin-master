using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib2;
using NodeSystemLib2.FileFormats;
using NodeSystemLib2.Generic;
using System.ComponentModel;

namespace Flumin {

    public class GraphContext : IGraphContext {

        private System.IO.TextWriter _setFileHandle;
        private NodeSystemLib2.FileFormats.RecordSetWriter _setWriter;
        private object _setLock = new object();
        private bool _firstSetWrite = true;
        private int _writeCount = 0;

        private Dictionary<GraphNotification.NotificationType, LogMessage.LogType> _msgTypeConverter =
            new Dictionary<GraphNotification.NotificationType, LogMessage.LogType>() {
                { GraphNotification.NotificationType.Error, LogMessage.LogType.Error },
                { GraphNotification.NotificationType.Info, LogMessage.LogType.Info },
                { GraphNotification.NotificationType.Warning, LogMessage.LogType.Warning }
            };

        public event PropertyChangedEventHandler PropertyChanged;

        public object DockPanel {
            get {
                return GlobalSettings.Instance.DockPanelInstance;
            }
        }

        private string _workingDirectory;
        public string WorkingDirectory {
            get { return _workingDirectory; }
            set { _workingDirectory = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WorkingDirectory))); }
        }

        private string _fileMask;
        public string FileMask {
            get { return _fileMask; }
            set { _fileMask = value;  PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileMask))); }
        }

        private string _workingDirMask;
        public string WorkingDirectoryMask {
            get { return _workingDirMask; }
            set { _workingDirMask = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WorkingDirectoryMask))); }
        }

        public void Notify(GraphNotification notification) {
            try {
                if (notification.Source == null) {
                    GlobalSettings.Instance.UserLog.Add(new LogMessage(_msgTypeConverter[notification.Type], notification.Message));
                } else {
                    GlobalSettings.Instance.UserLog.Add(new NodeMessage(notification.Source, _msgTypeConverter[notification.Type], notification.Message));
                }

            } catch (KeyNotFoundException) {
                GlobalSettings.Instance.UserLog.Add(new LogMessage(LogMessage.LogType.Warning,
                    $"Unknown notification type: {notification.Type}. Message: {notification.Message}"));
            }
        }

        public void NewSession() {
            var dir = WorkingDirectoryMask.Replace("%date%", DateTime.Now.ToShortDateString())
                                          .Replace("%time%", DateTime.Now.ToShortTimeString());

            if (dir.Last() == '\\') {
                dir = dir.Substring(0, dir.Length - 1);
            }

            WorkingDirectory = dir;

            int count = 1;
            while (System.IO.Directory.Exists(WorkingDirectory)) {
                WorkingDirectory = dir + " " + (count++);
            }

            //System.IO.Directory.CreateDirectory(WorkingDirectory);

            _writeCount = 0;
            _firstSetWrite = true;
        }

        public void AddRecordedFileToSet(RecordLine line) {
            lock (_setLock) {
                if (_firstSetWrite) {
                    BeginSetWrite();
                    _firstSetWrite = false;
                }

                _setWriter.BeginRecord();
                _setWriter.WriteRecordLine(line);
                _setWriter.EndRecord();

                _writeCount++;
            }
        }

        private void BeginSetWrite() {
            lock (_setLock) {
                var file = System.IO.Path.Combine(WorkingDirectory, "index.lst");
                _setFileHandle = System.IO.File.CreateText(file);
                _setWriter = new NodeSystemLib2.FileFormats.RecordSetWriter(_setFileHandle);
                _setWriter.BeginWrite();
            }
        }

        private void EndSetWrite() {
            lock (_setLock) {
                if (_setWriter != null && _writeCount > 0) {
                    _setWriter.EndWrite();
                    _setWriter.Dispose();
                    _setFileHandle.Dispose();
                }
            }
        }

        public void EndSession() {
            lock (_setLock) {
                EndSetWrite();
            }
        }
    }

    public class NodeSystemHost : INodeSystemHost {

        public int BufferLengthMs => GlobalSettings.Instance.BufferSizeMilliseconds;

        public object DockPanelInstance => GlobalSettings.Instance.DockPanelInstance;

        public int RingBufferLengthMs => GlobalSettings.Instance.RingBufferSizeMilliseconds;

        public void RegisterGraph(Graph g) {
            GlobalSettings.Instance.RegisterGraph(g);
        }

        public void UnregisterGraph(Graph g) {
            GlobalSettings.Instance.UnregisterGraph(g);
        }

        public int GetDefaultBufferSize(int samplerate) {
            return GlobalSettings.Instance.BufferSize(samplerate);
        }

        public int GetDefaultRingBufferSize(int samplerate) {
            return GlobalSettings.Instance.RingBufferSize(samplerate);
        }

        public void ReportError(Node node, string message) {
            GlobalSettings.Instance.UserLog.Add(new NodeMessage(node, LogMessage.LogType.Error, message));
        }

        public void ReportError(string message) {
            GlobalSettings.Instance.UserLog.Add(new LogMessage(LogMessage.LogType.Error, message));
        }

        public void StopProcessing() {
            GlobalSettings.Instance.StopProcessing();
        }

        public RecordSet RecordSetForGraph(Graph g) {
            return GlobalSettings.Instance.ActiveRecordSets[g];
        }
    }
}
