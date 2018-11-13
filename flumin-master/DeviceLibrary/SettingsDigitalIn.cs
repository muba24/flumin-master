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
    partial class SettingsDigitalIn : Form {

        public SettingsDigitalIn(NidaqSingleton.Device dev, NidaqSessionDigitalIn.ClockSource clockSrc, string clockPath, int clockRate) {
            InitializeComponent();
            comboBoxClock.DataSource = Enum.GetValues(typeof(NidaqSessionDigitalIn.ClockSource));
            comboBoxClock.SelectedItem = clockSrc;
            comboBoxClockSource.Text = clockPath;
            textBoxClockRate.Text = clockRate.ToString();

            var terms = new StringBuilder(10000);
            var result = NidaQmxHelper.DAQmxGetDevTerminals(dev.Name, terms, terms.Capacity - 1);
            
            if (result >= 0) {
                comboBoxClockSource.Items.AddRange(terms.ToString().Split(','));
            }
        }

        private void buttonOK_Click(object sender, EventArgs e) {
            this.Close();
        }

        public NidaqSessionDigitalIn.ClockSource ClockSource => (NidaqSessionDigitalIn.ClockSource)comboBoxClock.SelectedItem;

        public string ClockPath => comboBoxClockSource.Text;

        public int ClockRate {
            get {
                int result;
                if (int.TryParse(textBoxClockRate.Text, out result)) {
                    return result;
                }
                return 0;
            }
        }

        private void SettingsDigitalIn_Load(object sender, EventArgs e) {
        }

        private void buttonCancel_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void comboBoxClock_SelectedIndexChanged(object sender, EventArgs e) {
        }

        private void comboBoxClock_SelectedValueChanged(object sender, EventArgs e) {
            comboBoxClockSource.Enabled = (NidaqSessionDigitalIn.ClockSource)comboBoxClock.SelectedItem == NidaqSessionDigitalIn.ClockSource.Extern;
        }
    }
}
