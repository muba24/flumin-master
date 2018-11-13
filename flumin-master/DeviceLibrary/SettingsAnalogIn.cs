using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeviceLibrary {
    public partial class SettingsAnalogIn : Form {
        public SettingsAnalogIn(int clockRate) {
            InitializeComponent();
            textBoxClockRate.Text = clockRate.ToString();
        }

        private void buttonOK_Click(object sender, EventArgs e) {
            this.Close();
        }

        public int ClockRate {
            get {
                int result;
                if (int.TryParse(textBoxClockRate.Text, out result)) {
                    return result;
                }
                return 0;
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
