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
    public partial class SettingsAnalogOut : Form {

        public SettingsAnalogOut(int bufferLength, int prebufferLength) {
            InitializeComponent();
            numericUpDownBufferLength.Value = bufferLength;
            numericUpDownPreBufferLength.Value = prebufferLength;
        }

        private void buttonOK_Click(object sender, EventArgs e) {
            this.Close();
        }

        public int BufferLength => (int)numericUpDownBufferLength.Value;
        public int PreBufferLength => (int)numericUpDownPreBufferLength.Value;

        private void buttonCancel_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

    }
}
