using NodeSystemLib2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using NodeSystemLib2.FileFormats;

namespace Flumin {
    public partial class RecordInfoViewTree : DockContent {
        public RecordInfoViewTree() {
            InitializeComponent();
            DockAreas = DockAreas.DockBottom |
                        DockAreas.DockLeft |
                        DockAreas.DockRight |
                        DockAreas.DockTop |
                        DockAreas.Float;

            InitializeExplorerExample();
        }

        private string FullPath(string rel) {
            return System.IO.Path.Combine(textBoxPath.Text, rel);
        }

        void InitializeExplorerExample() {
            // Draw the system icon next to the name
            SysImageListHelper helper = new SysImageListHelper(treeListView);

            columnName.AspectGetter = delegate (object x) {
                if (x is string) return x;

                if (x is FileSystemInfo) {
                    return (x as FileSystemInfo).Name;
                }

                if (x is Record) {
                    return (x as Record).Date.ToString("dd.MM.yy HH:mm");
                }

                if (x is RecordLine) {
                    return ((RecordLine)x).Path;
                }

                return null;
            };

            treeListView.ChildrenGetter = delegate (object x) {
                if (x is Record) {
                    return ((Record)x).Lines;
                }
                return null;
            };

            treeListView.CanExpandGetter = delegate (object x) {
                if (x is Record) {
                    return true;
                }
                return false;
            };

            this.columnName.ImageGetter = delegate (object x) {
                if (x is FileSystemInfo) {
                    return helper.GetImageIndex(((FileSystemInfo)x).FullName);
                }
                if (x is Record) {
                    return helper.GetImageIndex(FullPath(((Record)x).Lines.First().Path));
                }
                return null;
            };

            this.columnDuration.AspectGetter = delegate (object x) {
                if (x is Record) {
                    var rec = (Record)x;
                    return rec.End - rec.Begin;
                }

                if (x is RecordLine) {
                    var rec = (RecordLine)x;
                    return rec.End - rec.Begin;
                }

                return null;
            };

            this.columnDuration.AspectToStringConverter = delegate (object x) {
                if (x != null) {
                    var stamp = (NodeSystemLib2.Generic.TimeInterval)x;
                    return TimeSpan.FromSeconds((stamp.End - stamp.Begin).AsSeconds()).ToString();
                }
                return "";
            };

            // Show the size of files as GB, MB and KBs. Also, group them by
            // some meaningless divisions
            this.columnSize.AspectGetter = delegate (object x) {
                if (x is DirectoryInfo || x is string)
                    return (long)-1;

                try {
                    if (x is FileInfo) {
                        return ((FileInfo)x).Length;
                    }

                    if (x is RecordLine) {
                        return new FileInfo(FullPath(((RecordLine)x).Path)).Length;
                    }

                    return null;

                } catch (System.IO.FileNotFoundException) {
                    // Mono 1.2.6 throws this for hidden files
                    return (long)-2;
                }
            };

            this.columnSize.AspectToStringConverter = delegate (object x) {
                if (x == null) return "";

                if ((long)x == -1) // folder
                    return "";
                else
                    return this.FormatFileSize((long)x);
            };

            this.columnSize.MakeGroupies(new long[] { 0, 1024 * 1024, 512 * 1024 * 1024 },
                new string[] { "Folders", "Small", "Big", "Disk space chewer" });

            textBoxPath.Text = "C:\\";
            PopulateListFromPath(textBoxPath.Text);
        }

        void PopulateListFromPath(string path) {
            try {
                if (!Directory.Exists(path)) return;
            } catch (Exception) {
                return;
            }

            treeListView.ClearObjects();
            treeListView.AddObject("..");

            DirectoryInfo pathInfo = null;
            try {
                pathInfo = new DirectoryInfo(path);
                if (!pathInfo.Exists) return;
            } catch (Exception) {
                return;
            }

            Cursor.Current = Cursors.WaitCursor;
            IEnumerable<FileSystemInfo> entries = pathInfo.GetFileSystemInfos().OfType<DirectoryInfo>();
            entries.ForEach(treeListView.AddObject);

            var indexFile = Path.Combine(path, "index.lst");
            if (File.Exists(indexFile)) {
                using (var reader = new NodeSystemLib2.FileFormats.RecordSetReader(System.IO.File.OpenText(indexFile))) {
                    var records = reader.Set;
                    treeListView.AddObjects(records.Records);
                }
            }

            Cursor.Current = Cursors.Default;
        }

        private void textBoxPath_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e) {
            if (e.KeyCode == System.Windows.Forms.Keys.Return) {
                PopulateListFromPath(textBoxPath.Text);
                e.SuppressKeyPress = true;
            }
        }

        string FormatFileSize(long size) {
            int[] limits = { 1024 * 1024 * 1024, 1024 * 1024, 1024 };
            string[] units = { "GB", "MB", "KB" };

            for (int i = 0; i < limits.Length; i++) {
                if (size >= limits[i])
                    return String.Format("{0:#,##0.##} " + units[i], ((double)size / limits[i]));
            }

            return String.Format("{0} bytes", size);
        }

        private void treeListView_ItemActivate(object sender, EventArgs e) {
            Object rowObject = treeListView.SelectedObject;
            if (rowObject == null) return;

            if (rowObject is string && (string)rowObject == "..") {
                var newDir = Directory.GetParent(textBoxPath.Text);
                if (newDir != null) rowObject = newDir;
                else rowObject = new DirectoryInfo(textBoxPath.Text);
            }

            if (rowObject is DirectoryInfo) {
                textBoxPath.Text = ((DirectoryInfo)rowObject).FullName;
                PopulateListFromPath(textBoxPath.Text);
            } else {
                if (rowObject is RecordLine) {
                    var view = RecordSetView.LoadRecordLine((RecordLine)rowObject, textBoxPath.Text);
                    if (view != null) {
                        view.Show(GlobalSettings.Instance.DockPanelInstance);
                    }
                    AddDragDropHandler(view);
                }

                if (rowObject is Record) {
                    var view = RecordSetView.LoadRecording((Record)rowObject, textBoxPath.Text);
                    if (view != null) {
                        view.Show(GlobalSettings.Instance.DockPanelInstance);
                    }
                    AddDragDropHandler(view);
                }
            }
        }

        private void AddDragDropHandler(RecordSetView view) {
            view.DragDrop += (o, e) => {
                var data = (BrightIdeasSoftware.OLVDataObject)e.Data;
                foreach (var recordLine in data.ModelObjects.OfType<RecordLine>()) {
                    if (recordLine is RecordLineStream1D) {
                        view.CreatePlot((RecordLineStream1D)recordLine, textBoxPath.Text);
                    } else if (recordLine is RecordLineStream2D) {
                        view.CreatePlot((RecordLineStream2D)recordLine, textBoxPath.Text);
                    }
                }
            };
        }

    }
}
