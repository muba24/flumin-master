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
    partial class SettingsDigitalOut : Form {
        public SettingsDigitalOut(NidaqSingleton.Device dev, string clockPath, int bufferLength, int prebufferLength) {
            InitializeComponent();
            numericUpDownBufferLength.Value = bufferLength;
            numericUpDownPreBufferLength.Value = prebufferLength;
            comboBoxClockSource.Text = clockPath;

            var terms = new StringBuilder(10000);
            var result = NidaQmxHelper.DAQmxGetDevTerminals(dev.Name, terms, terms.Capacity - 1);

            if (result >= 0) {
                comboBoxClockSource.Items.AddRange(terms.ToString().Split(','));
            }
        }

        private void buttonOK_Click(object sender, EventArgs e) {
            this.Close();
        }

        public string ClockPath => comboBoxClockSource.Text;
        public int BufferLength => (int)numericUpDownBufferLength.Value;
        public int PreBufferLength => (int)numericUpDownPreBufferLength.Value;

        private void buttonCancel_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
