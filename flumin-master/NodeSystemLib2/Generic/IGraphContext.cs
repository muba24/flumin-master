using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic {

    public interface IGraphContext : INotifyPropertyChanged {
        void Notify(GraphNotification notification);
        string WorkingDirectory { get; }
        string WorkingDirectoryMask { get; set; }
        string FileMask { get; set; }
        object DockPanel { get; }
        void NewSession();
        void EndSession();
        void AddRecordedFileToSet(NodeSystemLib2.FileFormats.RecordLine line);
    }

}
