using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using NodeSystemLib2;
using NodeSystemLib2.FileFormats;

namespace Flumin {
    public partial class RecordInfoView : DockContent {

        public RecordInfoView() {
            InitializeComponent();
            GlobalSettings.Instance.Recordings.CollectionChanged += Recordings_CollectionChanged;
            DockAreas = DockAreas.DockBottom |
                        DockAreas.DockLeft |
                        DockAreas.DockRight |
                        DockAreas.DockTop |
                        DockAreas.Float;

            InitializeExplorerExample();
        }

        private void Recordings_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (InvokeRequired) {
                BeginInvoke(new Action(RebuildTree));
            } else {
                RebuildTree();
            }
        }

        private void RebuildTree() {
            //treeView.Nodes.Clear();

            //var groups = from s in GlobalSettings.Instance.Recordings
            //             group s by new { Date = new DateTime(s.Date.Year, s.Date.Month, s.Date.Day) } into g
            //             select g;

            //foreach (var group in groups) {
            //    var root = treeView.Nodes.Add(group.Key.Date.ToLongDateString());
            //    foreach (var recording in group) {
            //        var recordNode = root.Nodes.Add(recording.Filename);
            //    }
            //}
        }

        private void treeView_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e) {

        }

        private void treeView_DoubleClick(object sender, EventArgs e) {
            //if (treeView.SelectedNode != null) {
            //    var file = treeView.SelectedNode.Text;
            //    if (System.IO.File.Exists(file)) {
            //        DisplayFile(file, 1000000);
            //    }
            //}
        }

        private void DisplayFile(string file, int samplerate) {
            //var frm = new FastZoomPreview();
            //frm.Show(GlobalSettings.Instance.DockPanelInstance);
            //frm.FitZoom = true;
            //frm.MaxSampleY = 2;
            //frm.MinSampleY = -2;
            //frm.LoadFilePreview(file, samplerate);
        }

        private void textBoxPath_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e) {
        }

        private string FullPath(string rel) {
            return System.IO.Path.Combine(textBoxPath.Text, rel);
        }

        void InitializeExplorerExample() {
            // Draw the system icon next to the name
            SysImageListHelper helper = new SysImageListHelper(objectListView);

            olvColumnName.AspectGetter = delegate (object x) {
                if (x is string) return x;

                if (x is FileSystemInfo) {
                    return (x as FileSystemInfo).Name;
                }
                return Path.GetFileName(FullPath((x as Record).Lines.First().Path));
            };

            this.olvColumnName.ImageGetter = delegate (object x) {
                if (x is FileSystemInfo) {
                    return helper.GetImageIndex(((FileSystemInfo)x).FullName);
                }
                if (x is Record) {
                    return helper.GetImageIndex(FullPath(((Record)x).Lines.First().Path));
                }
                return null;
            };

            this.olvColumnDuration.AspectGetter = delegate (object x) {
                if (x is Record) {
                    var rec = (Record)x;
                    return rec.End - rec.Begin;
                }
                return null;
            };

            this.olvColumnDuration.AspectToStringConverter = delegate (object x) {
                if (x != null) {
                    var stamp = (TimeStamp)x;
                    return stamp.ToShortTimeString();
                }
                return "";
            };

            // Show the size of files as GB, MB and KBs. Also, group them by
            // some meaningless divisions
            this.olvColumnSize.AspectGetter = delegate (object x) {
                if (x is DirectoryInfo || x is string)
                    return (long)-1;

                try {
                    if (x is FileInfo) {
                        return ((FileInfo)x).Length;
                    }
                    return new FileInfo(FullPath(((Record)x).Lines.First().Path)).Length;
                } catch (System.IO.FileNotFoundException) {
                    // Mono 1.2.6 throws this for hidden files
                    return (long)-2;
                }
            };

            this.olvColumnSize.AspectToStringConverter = delegate (object x) {
                if ((long)x == -1) // folder
                    return "";
                else
                    return this.FormatFileSize((long)x);
            };

            this.olvColumnSize.MakeGroupies(new long[] { 0, 1024 * 1024, 512 * 1024 * 1024 },
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

            objectListView.ClearObjects();
            objectListView.AddObject("..");

            DirectoryInfo pathInfo = null;
            try {
                pathInfo = new DirectoryInfo(path);
                if (!pathInfo.Exists) return;
            } catch (Exception) {
                return;
            }

            Cursor.Current = Cursors.WaitCursor;
            IEnumerable<FileSystemInfo> entries = pathInfo.GetFileSystemInfos().OfType<DirectoryInfo>();
            entries.ForEach(objectListView.AddObject);

            var indexFile = Path.Combine(path, "index.lst");
            if (File.Exists(indexFile)) {
                using (var reader = new NodeSystemLib2.FileFormats.RecordSetReader(System.IO.File.OpenText(indexFile))) {
                    var records = reader.Set;
                    objectListView.AddObjects(records.Records);
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
            int[] limits = new int[] { 1024 * 1024 * 1024, 1024 * 1024, 1024 };
            string[] units = new string[] { "GB", "MB", "KB" };

            for (int i = 0; i < limits.Length; i++) {
                if (size >= limits[i])
                    return String.Format("{0:#,##0.##} " + units[i], ((double)size / limits[i]));
            }

            return String.Format("{0} bytes", size);
        }

        private void objectListView_MouseDoubleClick(object sender, MouseEventArgs e) {
            
        }

        private void objectListView_ItemActivate(object sender, EventArgs e) {
            Object rowObject = objectListView.SelectedObject;
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
                var recording = (Record)rowObject;
                DisplayFile(
                    FullPath(recording.Lines.First().Path), 
                    ((RecordLineStream1D)recording.Lines.First()).Samplerate
                );
            }
        }

        private void objectListView_SelectedIndexChanged(object sender, EventArgs e) {

        }
    }

    public class SysImageListHelper {
        private SysImageListHelper() {
        }

        protected ImageList.ImageCollection SmallImageCollection
        {
            get
            {
                if (this.listView != null)
                    return this.listView.SmallImageList.Images;
                if (this.treeView != null)
                    return this.treeView.ImageList.Images;
                return null;
            }
        }

        protected ImageList.ImageCollection LargeImageCollection
        {
            get
            {
                if (this.listView != null)
                    return this.listView.LargeImageList.Images;
                return null;
            }
        }

        protected ImageList SmallImageList
        {
            get
            {
                if (this.listView != null)
                    return this.listView.SmallImageList;
                if (this.treeView != null)
                    return this.treeView.ImageList;
                return null;
            }
        }

        protected ImageList LargeImageList
        {
            get
            {
                if (this.listView != null)
                    return this.listView.LargeImageList;
                return null;
            }
        }


        /// <summary>
        /// Create a SysImageListHelper that will fetch images for the given tree control
        /// </summary>
        /// <param name="treeView">The tree view that will use the images</param>
        public SysImageListHelper(TreeView treeView) {
            if (treeView.ImageList == null) {
                treeView.ImageList = new ImageList();
                treeView.ImageList.ImageSize = new Size(16, 16);
            }
            this.treeView = treeView;
        }
        protected TreeView treeView;

        /// <summary>
        /// Create a SysImageListHelper that will fetch images for the given listview control.
        /// </summary>
        /// <param name="listView">The listview that will use the images</param>
        /// <remarks>Listviews manage two image lists, but each item can only have one image index.
        /// This means that the image for an item must occur at the same index in the two lists. 
        /// SysImageListHelper instances handle this requirement. However, if the listview already
        /// has image lists installed, they <b>must</b> be of the same length.</remarks>
        public SysImageListHelper(ListView listView) {
            if (listView.SmallImageList == null) {
                listView.SmallImageList = new ImageList();
                listView.SmallImageList.ColorDepth = ColorDepth.Depth32Bit;
                listView.SmallImageList.ImageSize = new Size(16, 16);
            }

            if (listView.LargeImageList == null) {
                listView.LargeImageList = new ImageList();
                listView.LargeImageList.ColorDepth = ColorDepth.Depth32Bit;
                listView.LargeImageList.ImageSize = new Size(32, 32);
            }

            //if (listView.SmallImageList.Images.Count != listView.LargeImageList.Images.Count)
            //    throw new ArgumentException("Small and large image lists must have the same number of items.");

            this.listView = listView;
        }
        protected ListView listView;

        /// <summary>
        /// Return the index of the image that has the Shell Icon for the given file/directory.
        /// </summary>
        /// <param name="path">The full path to the file/directory</param>
        /// <returns>The index of the image or -1 if something goes wrong.</returns>
        public int GetImageIndex(string path) {
            if (System.IO.Directory.Exists(path))
                path = System.Environment.SystemDirectory; // optimization! give all directories the same image
            else
                if (System.IO.Path.HasExtension(path))
                path = System.IO.Path.GetExtension(path);

            if (this.SmallImageCollection.ContainsKey(path))
                return this.SmallImageCollection.IndexOfKey(path);

            try {
                this.AddImageToCollection(path, this.SmallImageList, ShellUtilities.GetFileIcon(path, true, true));
                this.AddImageToCollection(path, this.LargeImageList, ShellUtilities.GetFileIcon(path, false, true));
            } catch (ArgumentNullException) {
                return -1;
            }

            return this.SmallImageCollection.IndexOfKey(path);
        }

        private void AddImageToCollection(string key, ImageList imageList, Icon image) {
            if (imageList == null)
                return;

            if (imageList.ImageSize == image.Size) {
                imageList.Images.Add(key, image);
                return;
            }

            using (Bitmap imageAsBitmap = image.ToBitmap()) {
                Bitmap bm = new Bitmap(imageList.ImageSize.Width, imageList.ImageSize.Height);
                Graphics g = Graphics.FromImage(bm);
                g.Clear(imageList.TransparentColor);
                Size size = imageAsBitmap.Size;
                int x = Math.Max(0, (bm.Size.Width - size.Width) / 2);
                int y = Math.Max(0, (bm.Size.Height - size.Height) / 2);
                g.DrawImage(imageAsBitmap, x, y, size.Width, size.Height);
                imageList.Images.Add(key, bm);
            }
        }
    }

    /// <summary>
    /// ShellUtilities contains routines to interact with the Windows Shell.
    /// </summary>
    public static class ShellUtilities {
        /// <summary>
        /// Execute the default verb on the file or directory identified by the given path.
        /// For documents, this will open them with their normal application. For executables,
        /// this will cause them to run.
        /// </summary>
        /// <param name="path">The file or directory to be executed</param>
        /// <returns>Values &lt; 31 indicate some sort of error. See ShellExecute() documentation for specifics.</returns>
        /// <remarks>The same effect can be achieved by <code>System.Diagnostics.Process.Start(path)</code>.</remarks>
        public static int Execute(string path) {
            return ShellUtilities.Execute(path, "");
        }

        /// <summary>
        /// Execute the given operation on the file or directory identified by the given path.
        /// Example operations are "edit", "print", "explore".
        /// </summary>
        /// <param name="path">The file or directory to be operated on</param>
        /// <param name="operation">What operation should be performed</param>
        /// <returns>Values &lt; 31 indicate some sort of error. See ShellExecute() documentation for specifics.</returns>
        public static int Execute(string path, string operation) {
            IntPtr result = ShellUtilities.ShellExecute(0, operation, path, "", "", SW_SHOWNORMAL);
            return result.ToInt32();
        }

        /// <summary>
        /// Get the string that describes the file's type.
        /// </summary>
        /// <param name="path">The file or directory whose type is to be fetched</param>
        /// <returns>A string describing the type of the file, or an empty string if something goes wrong.</returns>
        public static String GetFileType(string path) {
            SHFILEINFO shfi = new SHFILEINFO();
            int flags = SHGFI_TYPENAME;
            IntPtr result = ShellUtilities.SHGetFileInfo(path, 0, out shfi, Marshal.SizeOf(shfi), flags);
            if (result.ToInt32() == 0)
                return String.Empty;
            else
                return shfi.szTypeName;
        }

        /// <summary>
        /// Return the icon for the given file/directory.
        /// </summary>
        /// <param name="path">The full path to the file whose icon is to be returned</param>
        /// <param name="isSmallImage">True if the small (16x16) icon is required, otherwise the 32x32 icon will be returned</param>
        /// <param name="useFileType">If this is true, only the file extension will be considered</param>
        /// <returns>The icon of the given file, or null if something goes wrong</returns>
        public static Icon GetFileIcon(string path, bool isSmallImage, bool useFileType) {
            int flags = SHGFI_ICON;
            if (isSmallImage)
                flags |= SHGFI_SMALLICON;

            int fileAttributes = 0;
            if (useFileType) {
                flags |= SHGFI_USEFILEATTRIBUTES;
                if (System.IO.Directory.Exists(path))
                    fileAttributes = FILE_ATTRIBUTE_DIRECTORY;
                else
                    fileAttributes = FILE_ATTRIBUTE_NORMAL;
            }

            SHFILEINFO shfi = new SHFILEINFO();
            IntPtr result = ShellUtilities.SHGetFileInfo(path, fileAttributes, out shfi, Marshal.SizeOf(shfi), flags);
            if (result.ToInt32() == 0)
                return null;
            else
                return Icon.FromHandle(shfi.hIcon);
        }

        /// <summary>
        /// Return the index into the system image list of the image that represents the given file.
        /// </summary>
        /// <param name="path">The full path to the file or directory whose icon is required</param>
        /// <returns>The index of the icon, or -1 if something goes wrong</returns>
        /// <remarks>This is only useful if you are using the system image lists directly. Since there is
        /// no way to do that in .NET, it isn't a very useful.</remarks>
        public static int GetSysImageIndex(string path) {
            SHFILEINFO shfi = new SHFILEINFO();
            int flags = SHGFI_ICON | SHGFI_SYSICONINDEX;
            IntPtr result = ShellUtilities.SHGetFileInfo(path, 0, out shfi, Marshal.SizeOf(shfi), flags);
            if (result.ToInt32() == 0)
                return -1;
            else
                return shfi.iIcon;
        }

        #region Native methods

        private const int SHGFI_ICON               = 0x00100;     // get icon
        private const int SHGFI_DISPLAYNAME        = 0x00200;     // get display name
        private const int SHGFI_TYPENAME           = 0x00400;     // get type name
        private const int SHGFI_ATTRIBUTES         = 0x00800;     // get attributes
        private const int SHGFI_ICONLOCATION       = 0x01000;     // get icon location
        private const int SHGFI_EXETYPE            = 0x02000;     // return exe type
        private const int SHGFI_SYSICONINDEX       = 0x04000;     // get system icon index
        private const int SHGFI_LINKOVERLAY        = 0x08000;     // put a link overlay on icon
        private const int SHGFI_SELECTED           = 0x10000;     // show icon in selected state
        private const int SHGFI_ATTR_SPECIFIED     = 0x20000;     // get only specified attributes
        private const int SHGFI_LARGEICON          = 0x00000;     // get large icon
        private const int SHGFI_SMALLICON          = 0x00001;     // get small icon
        private const int SHGFI_OPENICON           = 0x00002;     // get open icon
        private const int SHGFI_SHELLICONSIZE      = 0x00004;     // get shell size icon
        private const int SHGFI_PIDL               = 0x00008;     // pszPath is a pidl
        private const int SHGFI_USEFILEATTRIBUTES  = 0x00010;     // use passed dwFileAttribute
        //if (_WIN32_IE >= 0x0500)
        private const int SHGFI_ADDOVERLAYS        = 0x00020;     // apply the appropriate overlays
        private const int SHGFI_OVERLAYINDEX       = 0x00040;     // Get the index of the overlay

        private const int FILE_ATTRIBUTE_NORMAL    = 0x00080;     // Normal file
        private const int FILE_ATTRIBUTE_DIRECTORY = 0x00010;     // Directory

        private const int MAX_PATH = 260;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO {
            public IntPtr hIcon;
            public int    iIcon;
            public int    dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAX_PATH)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=80)]
            public string szTypeName;
        }

        private const int SW_SHOWNORMAL = 1;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ShellExecute(int hwnd, string lpOperation, string lpFile,
            string lpParameters, string lpDirectory, int nShowCmd);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, int dwFileAttributes,
            out SHFILEINFO psfi, int cbFileInfo, int uFlags);

        #endregion
    }
}
