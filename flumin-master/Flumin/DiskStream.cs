using System.IO;

namespace Flumin {
    public class DiskStream {

        private readonly FileStream _file;

        public DiskStream(string path, int samplerate) {
            _file = File.OpenWrite(path);
            Samplerate = samplerate;
        }

        ~DiskStream() {
            Close();
        }

        public int Samplerate { get; }

        public long BytesWritten { get; private set; }

        public void AddData(double[] data) {
            unsafe {
                fixed (double* d = data) {
                    long bytes = data.Length*sizeof (double);
                    var stream = new UnmanagedMemoryStream((byte*)d, bytes);
                    stream.CopyTo(_file);
                    stream.Close();
                    BytesWritten += bytes;
                }
            }
        }

        public void Close() {
            _file?.Close();
        }

}
    
}
