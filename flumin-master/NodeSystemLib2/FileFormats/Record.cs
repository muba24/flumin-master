using NodeSystemLib2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FileFormats {

    public class Record {

        public List<RecordLine> Lines = new List<RecordLine>();

        public TimeStamp Begin => Lines.Min(line => line.Begin);

        public TimeStamp End => Lines.Min(line => line.End);

        public DateTime Date => Lines.Min(line => line.Date);

    }

}
