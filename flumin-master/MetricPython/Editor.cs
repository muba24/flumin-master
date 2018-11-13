using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScintillaNET;
using WeifenLuo.WinFormsUI.Docking;

namespace MetricPython {
    public partial class Editor : DockContent {

        public class ReloadEventArgs : EventArgs {
            public string Text;
        }

        private const int MarkerError = 0;

        private int _errorLine = -1;
        private int _errorOffset = -1;

        private Scintilla _scintilla;
        private bool _edited;

        public event EventHandler<ReloadEventArgs> OnReload;

        public Editor() {
            InitializeComponent();
        }

        public string Code {
            get { return _scintilla.Text; }
            set { _scintilla.Text = value; }
        }

        public bool EditorEnabled {
            get { return _scintilla.ReadOnly; }
            set { _scintilla.ReadOnly = !value; }
        }

        public void AskForSave() {
            if (Edited) {
                var result = MessageBox.Show(this, "Python code changed. Do you want to save?", "Code changed",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes) {
                    OnReload?.Invoke(this, new ReloadEventArgs() {Text = Code});
                }
            }
        }

        public void SetSaved() {
            Edited = false;
        }

        private void Editor_Load(object sender, EventArgs e) {
            _scintilla = new Scintilla();

            _scintilla.BorderStyle = BorderStyle.None;
            _scintilla.Dock = DockStyle.Fill;
            _scintilla.TextChanged += Scintilla_TextChanged;
            _scintilla.KeyDown += _scintilla_KeyDown;
            _scintilla.AssignCmdKey(Keys.S | Keys.Control, Command.Null);
            Controls.Add(_scintilla);
            Controls.SetChildIndex(toolStrip1, 1);
            Controls.SetChildIndex(statusStrip1, 2);
            Controls.SetChildIndex(_scintilla, 0);

            _scintilla.StyleResetDefault();
            _scintilla.Styles[Style.Default].Font = "Consolas";
            _scintilla.Styles[Style.Default].Size = 10;
            _scintilla.StyleClearAll(); // i.e. Apply to all
            _scintilla.Lexer = Lexer.Python;

            _scintilla.SetProperty("tab.timmy.whinge.level", "1");
            _scintilla.SetProperty("fold", "1");

            _scintilla.Margins[0].Width = 16;

            _scintilla.Margins[2].Type = MarginType.Symbol;
            _scintilla.Margins[2].Mask = Marker.MaskFolders;
            _scintilla.Margins[2].Sensitive = true;
            _scintilla.Margins[2].Width = 20;

            for (int i = Marker.FolderEnd; i <= Marker.FolderOpen; i++) {
                _scintilla.Markers[i].SetForeColor(SystemColors.ControlLightLight);
                _scintilla.Markers[i].SetBackColor(SystemColors.ControlDark);
            }

            _scintilla.Markers[Marker.Folder].Symbol = MarkerSymbol.BoxPlus;
            _scintilla.Markers[Marker.Folder].SetBackColor(SystemColors.ControlText);
            _scintilla.Markers[Marker.FolderOpen].Symbol = MarkerSymbol.BoxMinus;
            _scintilla.Markers[Marker.FolderEnd].Symbol = MarkerSymbol.BoxPlusConnected;
            _scintilla.Markers[Marker.FolderEnd].SetBackColor(SystemColors.ControlText);
            _scintilla.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            _scintilla.Markers[Marker.FolderOpenMid].Symbol = MarkerSymbol.BoxMinusConnected;
            _scintilla.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            _scintilla.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            // error symbol
            _scintilla.Markers[MarkerError].Symbol = MarkerSymbol.FullRect;
            _scintilla.Markers[MarkerError].SetBackColor(Color.Red);

            _scintilla.AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);

            _scintilla.Styles[Style.Python.Default].ForeColor = Color.FromArgb(0x80, 0x80, 0x80);
            _scintilla.Styles[Style.Python.CommentLine].ForeColor = Color.FromArgb(0x00, 0x7F, 0x00);
            _scintilla.Styles[Style.Python.CommentLine].Italic = true;
            _scintilla.Styles[Style.Python.Number].ForeColor = Color.FromArgb(0x00, 0x7F, 0x7F);
            _scintilla.Styles[Style.Python.String].ForeColor = Color.FromArgb(0x7F, 0x00, 0x7F);
            _scintilla.Styles[Style.Python.Character].ForeColor = Color.FromArgb(0x7F, 0x00, 0x7F);
            _scintilla.Styles[Style.Python.Word].ForeColor = Color.FromArgb(0x00, 0x00, 0x7F);
            _scintilla.Styles[Style.Python.Word].Bold = true;
            _scintilla.Styles[Style.Python.Triple].ForeColor = Color.FromArgb(0x7F, 0x00, 0x00);
            _scintilla.Styles[Style.Python.TripleDouble].ForeColor = Color.FromArgb(0x7F, 0x00, 0x00);
            _scintilla.Styles[Style.Python.ClassName].ForeColor = Color.FromArgb(0x00, 0x00, 0xFF);
            _scintilla.Styles[Style.Python.ClassName].Bold = true;
            _scintilla.Styles[Style.Python.DefName].ForeColor = Color.FromArgb(0x00, 0x7F, 0x7F);
            _scintilla.Styles[Style.Python.DefName].Bold = true;
            _scintilla.Styles[Style.Python.Operator].Bold = true;
            // scintilla.Styles[Style.Python.Identifier] ... your keywords styled here
            _scintilla.Styles[Style.Python.CommentBlock].ForeColor = Color.FromArgb(0x7F, 0x7F, 0x7F);
            _scintilla.Styles[Style.Python.CommentBlock].Italic = true;
            _scintilla.Styles[Style.Python.StringEol].ForeColor = Color.FromArgb(0x00, 0x00, 0x00);
            _scintilla.Styles[Style.Python.StringEol].BackColor = Color.FromArgb(0xE0, 0xC0, 0xE0);
            _scintilla.Styles[Style.Python.StringEol].FillLine = true;
            _scintilla.Styles[Style.Python.Word2].ForeColor = Color.FromArgb(0x40, 0x70, 0x90);
            _scintilla.Styles[Style.Python.Decorator].ForeColor = Color.FromArgb(0x80, 0x50, 0x00);

            _scintilla.ViewWhitespace = WhitespaceMode.VisibleAlways;

            var python2 = "and as assert break class continue def del elif else except exec finally for from global if import in is lambda not or pass print raise return try while with yield";
            var python3 = "False None True and as assert break class continue def del elif else except finally for from global if import in is lambda nonlocal not or pass raise return try while with yield";
            var cython = "cdef cimport cpdef";

            _scintilla.SetKeywords(0, python3);

        }

        private void _scintilla_KeyDown(object sender, KeyEventArgs e) {
            System.Diagnostics.Debug.WriteLine(e.KeyCode);
            if (e.Control && e.KeyCode == Keys.S) {
                OnReload?.Invoke(this, new ReloadEventArgs() {Text = Code});
            }
        }

        public bool Edited {
            get { return _edited; }
            private set {
                _edited = value;
                UpdateWindowCaption();
            }
        }

        private void UpdateWindowCaption() {
            Text = "Editor" + (Edited ? "*" : "");
        }

        private int _maxLineNumberCharLength;
        private void Scintilla_TextChanged(object sender, EventArgs e) {
            Edited = true;

            // Did the number of characters in the lineNumber number display change?
            // i.e. nnn VS nn, or nnnn VS nn, etc...
            var maxLineNumberCharLength = _scintilla.Lines.Count.ToString().Length;
            if (maxLineNumberCharLength == _maxLineNumberCharLength)
                return;

            // Calculate the width required to display the last lineNumber number
            // and include some padding for good measure.
            const int padding = 2;
            _scintilla.Margins[0].Width = _scintilla.TextWidth(Style.LineNumber, new string('9', maxLineNumberCharLength + 1)) + padding;
            _maxLineNumberCharLength = maxLineNumberCharLength;
        }

        public void SetError(string message, int lineNumber, int offset) {
            if (lineNumber > 0) {
                toolStripStatusLabel.Text = $"Error in lineNumber {lineNumber}: {message}";
            } else {
                toolStripStatusLabel.Text = $"Error: {message}";
            }

            _errorLine = lineNumber;
            _errorOffset = offset;

            foreach (var line in _scintilla.Lines) {
                line.MarkerDelete(MarkerError);
            }

            if (_errorLine > -1) {
                _scintilla.Lines[_errorLine - 1].MarkerAdd(MarkerError);
            }
        }

        public void ClearError() {
            toolStripStatusLabel.Text = "OK";
            _errorLine = -1;
            _errorOffset = -1;

            foreach (var line in _scintilla.Lines) {
                line.MarkerDelete(MarkerError);
            }
        }

        private void toolStripButtonReload_Click(object sender, EventArgs e) {
            OnReload?.Invoke(this, new ReloadEventArgs() { Text = Code });
            Edited = false;
        }

        private void Editor_FormClosing(object sender, FormClosingEventArgs e) {
            if (Edited) {
                AskForSave();
            }
        }

        private void toolStripStatusLabel_DoubleClick(object sender, EventArgs e) {
            if (_errorLine > -1) {
                _scintilla.Lines[_errorLine].EnsureVisible();
            }
        }
    }
}
