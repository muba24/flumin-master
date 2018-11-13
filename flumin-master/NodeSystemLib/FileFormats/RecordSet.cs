using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib {
    namespace FileFormatLib {

        public class RecordSet {

            private readonly List<Record> _records = new List<Record>();
            
            public List<Record> Records => _records;

            public Graph Parent { get; }

            public RecordSet(Graph parent) {
                Parent = parent;
            }

            public void AddRecord(Record rcd) {
                _records.Add(rcd);
            }

        }

    }
}