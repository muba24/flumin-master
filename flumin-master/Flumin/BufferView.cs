using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NodeSystemLib2;

namespace Flumin {
    public partial class BufferView : UserControl {

        public BufferView() {
            InitializeComponent();
        }

        InputPort port;

        public InputPort Port {
            get { return port; }

            set {
                port = value;
                Update();
            }
        }

        public string NodeName {
            get { return port.Parent.Name; }
        }

        public string PortName {
            get { return port.Name; }
        }

        private void UpdateDisplay(NodeSystemLib2.FormatDataFFT.InputPortDataFFT port) {
            labelTime.Text = "Time: " + port.Time.AsTimeSpan().ToString("c");
            if (port.Capacity == 0) {
                SetPercent(0);
            } else {
                SetPercent(port.Available / (double)port.Capacity);
            }
        }

        private void UpdateDisplay(NodeSystemLib2.FormatData1D.InputPortData1D port) {
            labelTime.Text = "Time: " + port.Time.AsTimeSpan().ToString("c");
            if (port.Capacity == 0) {
                SetPercent(0);
            } else {
                SetPercent(port.Available / (double)port.Capacity);
            }
        }

        public void UpdateDisplay() {
            labelNodeName.Text = NodeName;
            labelPortName.Text = PortName;

            if (port is NodeSystemLib2.FormatData1D.InputPortData1D) {
                UpdateDisplay((NodeSystemLib2.FormatData1D.InputPortData1D)port);
            } else if (port is NodeSystemLib2.FormatDataFFT.InputPortDataFFT) {
                UpdateDisplay((NodeSystemLib2.FormatDataFFT.InputPortDataFFT)port);
            } else {
                labelTime.Text = "Time: ?";
                SetPercent(0);
            }
        }

        /// <summary>
        /// Set percent of progressbar
        /// </summary>
        /// <param name="percent">Percent ranging [0..1]</param>
        private void SetPercent(double percent) {
            progressBarFill.Value = (int)(progressBarFill.Maximum * percent);
        }

    }
}
