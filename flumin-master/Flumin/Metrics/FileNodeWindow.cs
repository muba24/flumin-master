using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace SimpleADC.Metrics {

    public partial class FileNodeWindow : DockContent {
        double _percent = 0.0;
        double _percentDone = 0.0;

        public event EventHandler<double> PercentChanged;

        private readonly List<double> SavedStatePercents = new List<double>();

        public FileNodeWindow() {
            InitializeComponent();
        }

        public void AddSavedStateTimePercent(double percent) {
            lock (SavedStatePercents) {
                SavedStatePercents.Add(percent);
            }
        }

        public double Percent {
            get {
                return _percent;
            }
            set {
                _percent = value;
                this.Invalidate();
                PercentChanged?.Invoke(this, value);
            }
        }

        public double PercentDone {
            get {
                return _percentDone;
            }
            set {
                _percentDone = value;
                this.Invalidate();
            }
        }

        private void FileNodeWindow_Paint(object sender, PaintEventArgs e) {
            e.Graphics.FillRectangle(Brushes.Blue, new Rectangle(0, 0, (int)(_percent * Width), Height));
            e.Graphics.FillRectangle(Brushes.LightBlue, new Rectangle(0, 0, (int)(_percentDone * Width), Height));

            lock (SavedStatePercents) {
                foreach (var statePercent in SavedStatePercents) {
                    var x = (int)(statePercent * Width);
                    e.Graphics.DrawLine(Pens.Yellow, x, 0, x, Height);
                }
            }
        }

        private void FileNodeWindow_KeyPress(object sender, KeyPressEventArgs e) {

        }

        private void FileNodeWindow_MouseDown(object sender, MouseEventArgs e) {
            Percent = e.X / (double)this.Width;
        }

    }
}
