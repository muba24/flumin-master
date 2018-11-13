using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flumin {
    class PropertyRowContainer<T> : Panel where T : Control {

        private readonly T _control;
        private readonly Label _unitLabel;
        private readonly Func<object> _valueGetter;

        public PropertyRowContainer(T ctrl, string unit, Func<object> valueGetter) : this(ctrl, unit) {
            _valueGetter = valueGetter;
        }

        public PropertyRowContainer(T ctrl, string unit) {
            this.CausesValidation = false;
            this.GotFocus += PropertyRowContainer_GotFocus;

            _unitLabel = new Label() {
                Text = unit,
                Dock = DockStyle.Right,
                CausesValidation = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            using (var g = System.Drawing.Graphics.FromHwnd(_unitLabel.Handle)) {
                var size = g.MeasureString(unit, _unitLabel.Font);
                _unitLabel.Size = new System.Drawing.Size((int)(size.Width + 5), (int)size.Height);
            }

            _control = ctrl;
            _control.Dock = DockStyle.Fill;
            
            Controls.Add(_control);
            Controls.Add(_unitLabel);

            if (_control is TextBox) {
                _valueGetter = () => _control.Text;
            } else if (_control is NumericUpDown) {
                _valueGetter = () => (_control as NumericUpDown)?.Value;
            }
        }

        private void PropertyRowContainer_GotFocus(object sender, EventArgs e) {
            _control.Focus();
        }

        public T UserControl => _control;

        public object Value {
            get {
                if (_valueGetter != null) {
                    return _valueGetter();
                }
                return null;
            }
        }

    }
}
