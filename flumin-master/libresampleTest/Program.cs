using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libresampleTest {


    class Program {

        static void Main(string[] args) {
            Resampler rsmp = new Resampler(100, 50, 100, 50);

            var arr = Enumerable.Range(0, 100)
                                .Select(x => Math.Sin(2 * Math.PI * 10 * x / 100))
                                .ToArray();

            var dst = new double[50];

            rsmp.PutData(arr, 0, arr.Length);
            rsmp.Resample();
            rsmp.Flush();
        }

    }
}
