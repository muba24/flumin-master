using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceLibrary {
    public class NidaqException : Exception {

        public int Code { get; }

        public NidaqException(int code) : base(NidaQmxHelper.GetError(code)) {}

    }
}
