using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2 {
    public static class DefaultParameters {
        public static readonly TimeSpan DefaultQueueMilliseconds  = TimeSpan.FromMilliseconds(500);
        public static readonly TimeSpan DefaultBufferMilliseconds = TimeSpan.FromMilliseconds(100);
        public static readonly int MinimumQueueFrameCount = 5;
        public static readonly int MinimumBufferFrameCount = 5;

        public static int ToSamples(this TimeSpan span, int samplerate) {
            return (int)Math.Ceiling(span.TotalSeconds * samplerate);
        }

        public static int ToFrames(this TimeSpan span, int samplerate, int frameSize) {
            return ToSamples(span, samplerate) / frameSize;
        }

    }
}
