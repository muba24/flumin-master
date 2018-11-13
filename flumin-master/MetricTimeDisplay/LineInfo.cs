using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MetricTimeDisplay {
    public partial class LineInfo : UserControl {

        public NewOpenGLRenderer.DataLine1D Line { get; set; }

        public void UpdateLabels() {
            if (Line != null) {
                labelName.Text = Line.Name;
                labelSamplerate.Text = Line.SamplesPerSecond.ToString();
                labelTime.Text = $"{Line.Duration.Begin.ToShortTimeString()} - {Line.Duration.End.ToShortTimeString()}";
            }
        }

        public LineInfo() {
            InitializeComponent();
        }
    }
}
