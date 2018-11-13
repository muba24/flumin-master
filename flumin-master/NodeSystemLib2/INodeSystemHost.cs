using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2 {
    public interface INodeSystemHost {
        int GetDefaultBufferSize(int samplerate);
        int GetDefaultRingBufferSize(int samplerate);
        int BufferLengthMs { get; }
        int RingBufferLengthMs { get; }
        object DockPanelInstance { get; }

        //FileFormatLib.RecordSet RecordSetForGraph(Graph g);

        void RegisterGraph(Graph g);
        void UnregisterGraph(Graph g);

        void ReportError(Node node, string message);
        void ReportError(string message);
        void StopProcessing();
    }
}
