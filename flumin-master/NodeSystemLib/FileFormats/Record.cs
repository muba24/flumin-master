using NodeSystemLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib {
    namespace FileFormatLib {

        public class Record {

            public List<RecordLine> Lines = new List<RecordLine>();

            public TimeStamp Begin => Lines.Min(line => line.Begin);

            public TimeStamp End => Lines.Min(line => line.End);

            public DateTime Date => Lines.Min(line => line.Date);

        }

    }
}