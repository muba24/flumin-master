using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2 {

   public static class PortDataTypes {

        public static readonly PortDataType TypeIdSignal1D = new PortDataType(
            "Data1D",
            Guid.Parse("{39d4f0d9-1b41-4a21-b3c3-b828dedcf920}")
        );

        public static readonly PortDataType TypeIdFFT = new PortDataType(
            "FFT",
            Guid.Parse("{8c5db181-923a-49ba-8b9d-34d20b679b54}")
        );

        public static readonly PortDataType TypeIdValueDouble = new PortDataType(
            "Double Value",
            Guid.Parse("{b14d8acb-97ba-436d-85f9-9746aa7967b4}")
        );

    }

}
