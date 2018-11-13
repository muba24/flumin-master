using System.Globalization;
using System.Windows.Forms;

namespace WaveDisplayControl {
    public partial class ChangeZoom : Form {

        public class ZoomResult {
            public double YMax;
            public double YMin;
        }

        private bool _cancel;

        public ChangeZoom() {
            InitializeComponent();
        }

        public ZoomResult ShowDialog(Control parent, double ymin, double ymax) {
            textYMax.Text = ymax.ToString(CultureInfo.InvariantCulture);
            textYMin.Text = ymin.ToString(CultureInfo.InvariantCulture);
            _cancel = false;
            
            StartPosition = FormStartPosition.CenterParent;
            textYMax.SelectAll();
            ShowDialog();
            if (_cancel) return null;

            return new ZoomResult() {
                YMax = double.Parse(textYMax.Text),
                YMin = double.Parse(textYMin.Text)
            };
        }

        private void buttonOK_Click(object sender, System.EventArgs e) {
            Hide();
        }

        private void ChangeZoom_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) _cancel = true;
        }

        private void textYMin_KeyPress(object sender, KeyPressEventArgs e) {
            
        }

        private void textYMin_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
                buttonOK_Click(sender, null);
            }
        }

        private void textYMax_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
                textYMin.Focus();
            }
        }
    }
}
