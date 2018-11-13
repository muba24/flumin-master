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

namespace MetricFileSource {

    public partial class FileNodeWindow : DockContent {

        public class ValueChangedEventArgs : EventArgs {
            public long Value;

            public ValueChangedEventArgs(long v) {
                Value = v;
            }
        }

        public event EventHandler<ValueChangedEventArgs> ValueChanged;

        private readonly List<long> _statePointMarkers = new List<long>();

        public FileNodeWindow() {
            InitializeComponent();
        }

        public void AddStatePointMarker(long value) {
            lock (_statePointMarkers) {
                _statePointMarkers.Add(value);
            }
        }

        long _max;
        long _min;
        long _value;
        long _valueBackground;

        private long Span => Max - Min;

        public long Max {
            get { return _max; }
            set { _max = value; }
        }

        public long Min {
            get {
                return _min;
            }

            set {
                _min = value;
            }
        }

        public long Value {
            get {
                return this._value;
            }

            set {
                this._value = value;
                this.Invalidate();
            }
        }

        public long ValueBackground {
            get { return _valueBackground; }

            set {
                _valueBackground = value;
                this.Invalidate();
            }
        }

        private void FileNodeWindow_Paint(object sender, PaintEventArgs e) {
            e.Graphics.FillRectangle(Brushes.Blue, new Rectangle(0, 0, (int)((Value - Min) * Width / Span), Height));
            e.Graphics.FillRectangle(Brushes.LightBlue, new Rectangle(0, 0, (int)((ValueBackground - Min) * Width / Span), Height));

            lock (_statePointMarkers) {
                foreach (var position in _statePointMarkers) {
                    var x = (int)((position - Min) * Width / Span);
                    e.Graphics.DrawLine(Pens.Yellow, x, 0, x, Height);
                }
            }
        }

        private void FileNodeWindow_KeyPress(object sender, KeyPressEventArgs e) {

        }

        private void FileNodeWindow_MouseDown(object sender, MouseEventArgs e) {
            Value = e.X * Span / Width + Min;
            ValueChanged?.Invoke(this, new ValueChangedEventArgs(Value));
        }

    }
}
