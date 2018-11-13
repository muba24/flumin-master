using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib2.FileFormats;

namespace NodeSystemLib2.Generic {
    class EmptyGraphContext : IGraphContext {

        public object DockPanel {
            get {
                return null;
            }
        }

        public string FileMask { get; set; }

        public string WorkingDirectory { get; set; }

        public string WorkingDirectoryMask { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddRecordedFileToSet(RecordLine line) {
            //
        }

        public void EndSession() {
            //
        }

        public void NewSession() {
            //
        }

        public void Notify(GraphNotification notification) {
            System.Diagnostics.Debug.WriteLine($"EmptyGraphContext Notification of type {notification.Type}: {notification.Message}");
        }
    }
}
