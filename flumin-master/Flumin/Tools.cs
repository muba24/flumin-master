using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Flumin {
    public partial class Tools : DockContent {
        public Tools() {
            InitializeComponent();

            DockAreas = DockAreas.DockBottom |
                        DockAreas.DockLeft |
                        DockAreas.DockRight |
                        DockAreas.DockTop |
                        DockAreas.Float;

            GlobalSettings.Instance.MetricManager.MetricAdded += MetricManager_MetricAdded;

            BuildCollection();
        }

        private void MetricManager_MetricAdded(object sender, MetricManager.MetricAddedEventArgs e) {
            BuildCollection();
        }

        private void BuildCollection() {
            toolBox1.DeleteAllTabs(true);

            var groups = GlobalSettings.Instance.MetricManager.Metrics.GroupBy(t => t.Category);

            foreach (var group in groups) {
                var tab = new Silver.UI.ToolBoxTab(group.Key, -1);
                tab.TabBackgroundColor = NextPastelColor();
                toolBox1.AddTab(tab);

                foreach (var item in group) {
                    var boxItem = new Silver.UI.ToolBoxItem(item.Name, -1);
                    boxItem.Object = item;
                    tab.AddItem(boxItem);
                }
            }
        }

        private int _pastelCount = 0;
        private Color NextPastelColor() {
            _pastelCount = (_pastelCount + 240 / 8) % 160;
            return new HSLColor((double)_pastelCount, 240, 220);
        }

    }
}
