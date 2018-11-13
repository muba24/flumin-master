using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2 {
    public class SamplerateChangedEventArgs : EventArgs {
        public readonly int NewSamplerate;

        public SamplerateChangedEventArgs(int newsr) {
            NewSamplerate = newsr;
        }
    }
}
