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
    public partial class FormOnly2D : Form {

        const int Samplerate = 1000;

        private DataSet set;
        private DataLine2D line;
        private DataLine2D line2;

        public FormOnly2D() {
            InitializeComponent();
        }

        private void plot1_GLLoaded(object sender, EventArgs e) {
            line  = new DataLine2D("Line 1", 2000) { LineColor = Color.Blue, SamplesPerSecond = 1000000 };
            line2 = new DataLine2D("Line 2", 2000) { LineColor = Color.Green, SamplesPerSecond = 1000000 };

            set = new TimeDataSet {
                SamplesPerSecond = Samplerate,
                Milliseconds = 2000
            };

            set.AxisX.VisibleMinimum = 0;
            set.AxisX.VisibleMaximum = 5000;
            set.AxisY.VisibleMinimum = -2;
            set.AxisY.VisibleMaximum = 10;

            set.Data.Add(line);
            set.Data.Add(line2);

            plot1.Set = set;
            plot1.CreateLegend();
        }

        float x = 0;

        private void timer1_Tick(object sender, EventArgs e) {
            line.Add(new PointF(x, 1 + x / 1000000));
            line2.Add(new PointF(x, 2 + x / 1000000));
            x += 1000000 / 10;

            plot1.Refresh();
        }
    }
}
