using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using NewOpenGLRenderer;
using NodeSystemLib2;
using NodeSystemLib2.FileFormats;

namespace Flumin {
    public partial class RecordSetView : DockContent {

        private Color[] _colors = { Color.Blue, Color.Green, Color.Red, Color.Orange, Color.Navy, Color.Black };

        private Plot _plot;

        private string _directory;

        private RecordLine _input;

        private Record _recording;

        private TimeDataSet _set;

        public RecordSetView() {
            InitializeComponent();

            DockAreas = DockAreas.DockBottom | 
                        DockAreas.DockLeft   |
                        DockAreas.DockTop    | 
                        DockAreas.DockRight  | 
                        DockAreas.Float;

            _plot = new Plot {
                Antialias      = true,
                BackColor      = Color.White,
                Border         = new Padding(40, 10, 20, 50),
                GraphBackColor = Color.FromArgb(224, 224, 224),
                Location       = new Point(0, 0),
                Name           = "plot",
                Set            = null,
                Size           = new Size(913, 466),
                TabIndex       = 1,
                Dock           = DockStyle.Fill
            };

            Controls.Add(_plot);

            _plot.GLLoaded += plot_GLLoaded;
            _plot.DragEnter += _plot_DragEnter;
            _plot.DragDrop += _plot_DragDrop;
        }

        private void _plot_DragDrop(object sender, DragEventArgs e) {
            System.Diagnostics.Debug.WriteLine($"{e.Data}");
            OnDragDrop(e);
        }

        private void _plot_DragEnter(object sender, DragEventArgs e) {
            e.Effect = DragDropEffects.All;
        }

        private RecordSetView(Record set, string directory) : this() {
            _recording = set;
            _input = null;
            _directory = directory;
        }

        private RecordSetView(RecordLineStream1D line, string directory) : this() {
            _input = line;
            _directory = directory;
        }

        private RecordSetView(RecordLineStream2D line, string directory) : this() {
            _input = line;
            _directory = directory;
        }

        public DataLineFile1D CreatePlot(RecordLineStream1D input, string directory) {
            Stream1DReader reader = null;

            try {
                reader = new Stream1DReader(
                    new System.IO.BinaryReader(
                        System.IO.File.Open(
                            System.IO.Path.Combine(directory, input.Path),
                            System.IO.FileMode.Open, 
                            System.IO.FileAccess.Read, 
                            System.IO.FileShare.Read
                        )
                    )
                );

            } catch (Exception ex) {
                GlobalSettings.Instance.UserLog.Add(new FormMessage(this, LogMessage.LogType.Error, $"Can't create stream for file {input.Path}: {ex}"));
                return null;
            }

            var line = new DataLineFile1D(input.Path, reader, input.Samplerate, input.Begin);
            line.LineColor = _colors[(_set.Data.Count + 1) % _colors.Length];
            line.Visible = true;

            _set.Data.Add(line);

            return line;
        }

        public DataLineFile2D CreatePlot(RecordLineStream2D input, string directory) {
            Stream2DReader reader = null;

            try {
                reader = new Stream2DReader(
                    System.IO.File.OpenText(System.IO.Path.Combine(directory, input.Path)), ','
                );
            } catch (Exception ex) {
                GlobalSettings.Instance.UserLog.Add(new FormMessage(this, LogMessage.LogType.Error, $"Can't create stream for file {input.Path}: {ex}"));
                return null;
            }

            var line2d = new DataLineFile2D(input.Path, reader, input.Begin, input.End);
            line2d.LineColor = _colors[(_set.Data.Count + 1) % _colors.Length];
            line2d.Visible = true;

            _set.Data.Add(line2d);

            return line2d;
        }

        private void plot_GLLoaded(object sender, EventArgs e) {
            _set = new TimeDataSet();

            _set.SamplesPerSecond     = 1000000;
            _set.Milliseconds         = double.MaxValue;
            _set.AlignLines           = false;
            _set.AxisX.VisibleMinimum = 0;
            _set.AxisX.VisibleMaximum = 1000000;
            _set.AxisY.VisibleMinimum = -1;
            _set.AxisY.VisibleMaximum = 1;

            if (_input != null) {
                if (_input is RecordLineStream1D) {
                    CreatePlot((RecordLineStream1D)_input, _directory);
                } else if (_input is RecordLineStream2D) {
                    CreatePlot((RecordLineStream2D)_input, _directory);
                }
            } else if (_recording != null) {
                foreach (var rec in _recording.Lines) {
                    if (rec is RecordLineStream1D) {
                        CreatePlot((RecordLineStream1D)rec, _directory);
                    } else if (rec is RecordLineStream2D) {
                        CreatePlot((RecordLineStream2D)rec, _directory);
                    }
                }
            }

            _plot.Set = _set;
            _plot.CreateLegend();
        }

        public static RecordSetView LoadRecording(Record recording, string directory) {
            return new RecordSetView(recording, directory);
        }

        public static RecordSetView LoadRecordLine(RecordLine line, string directory) {
            if (line is RecordLineStream1D) {
                return new RecordSetView((RecordLineStream1D)line, directory);
            } else if (line is RecordLineStream2D) {
                return new RecordSetView((RecordLineStream2D)line, directory);
            } else {
                return null;
            }
        }

        private void RecordSetView_FormClosed(object sender, FormClosedEventArgs e) {
            foreach (var rec in _set.Data.OfType<IDisposable>()) {
                rec.Dispose();
            }
        }
    }
}
