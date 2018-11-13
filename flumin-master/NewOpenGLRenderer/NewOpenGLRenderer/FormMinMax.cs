using NodeSystemLib2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NewOpenGLRenderer {
    public partial class FormMinMax : Form {

        const int Samplerate = 1000000;
        private DataLine1D line;
        private DataSet set;
        private DataLine1D line2;

        public FormMinMax() {
            InitializeComponent();
        }

        private void plot1_GLLoaded(object sender, EventArgs e) {
            line  = new DataLine1D("Line 1", 2 * Samplerate / 2, Samplerate) { LineColor = Color.Blue };
            line2 = new DataLine1D("Line 2", 2 * Samplerate, Samplerate / 2) { LineColor = Color.Green };

            var data = new NodeSystemLib2.FormatData1D.TimeLocatedBuffer1D<double>(2 * Samplerate / 2, Samplerate);
            var data2 = new NodeSystemLib2.FormatData1D.TimeLocatedBuffer1D<double>(2 * Samplerate, Samplerate / 2);

            for (int i = 0; i < data.Capacity; i++) {
                data.Data[i] = (float) Math.Sin(2 * Math.PI * 10 / Samplerate * i);
            }

            for (int i = 0; i < data2.Capacity; i++) {
                data2.Data[i] = (float)Math.Cos(2 * Math.PI * 20 / Samplerate * i);
            }

            data.SetWritten(data.Capacity);
            data2.SetWritten(data2.Capacity);
            line.Add(data);
            line2.Add(data2);

            set = new TimeDataSet {
                SamplesPerSecond = Samplerate,
                Milliseconds = 2000
            };

            set.AxisX.VisibleMinimum = 0;
            set.AxisX.VisibleMaximum = 2000;
            set.AxisY.VisibleMinimum = -2;
            set.AxisY.VisibleMaximum = 2;

            set.Data.Add(line);
            set.Data.Add(line2);

            plot1.Set = set;
            plot1.CreateLegend();
        }

        private void timer1_Tick(object sender, EventArgs e) {
            plot1.Refresh();
        }

        private void FormMinMax_Load(object sender, EventArgs e) {

        }
    }
}
