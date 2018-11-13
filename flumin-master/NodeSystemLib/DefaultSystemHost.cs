using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib.FileFormatLib;

namespace NodeSystemLib {

    class DefaultSystemHost : INodeSystemHost {

        public int BufferLengthMs => 100;

        public object DockPanelInstance => null;

        public int RingBufferLengthMs => 1000;

        public int GetDefaultBufferSize(int samplerate) {
            return (int)(((long)BufferLengthMs * samplerate) / 1000);
        }

        public int GetDefaultRingBufferSize(int samplerate) {
            return (int)(((long)RingBufferLengthMs * samplerate) / 1000);
        }

        public void ReportError(string message) {
            System.Diagnostics.Debug.WriteLine($"Error: {message}");
        }

        public void ReportError(Node node, string message) {
            System.Diagnostics.Debug.WriteLine($"Error in {node.Name}: {message}");
        }

        public void StopProcessing() {
            throw new NotImplementedException();
        }

        public RecordSet RecordSetForGraph(Graph g) {
            return new RecordSet(g);
        }

        public void RegisterGraph(Graph g) {
            //
        }

        public void UnregisterGraph(Graph g) {
            //
        }

    }

}
