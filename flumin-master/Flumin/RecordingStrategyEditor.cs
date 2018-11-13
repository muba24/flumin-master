using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using BrightIdeasSoftware;
using Flumin.RecordingStrategies;

namespace Flumin {
    public partial class RecordingStrategyEditor : Form {

        private readonly Dictionary<string, Type> _strategyTypes = new Dictionary<string, Type> {
            { "Shot", typeof(RecordingStrategyOneShot) },
            { "Pause", typeof(RecordingStrategyPause) },
            { "Loop", typeof(RecordingStrategyLoop) },
            { "Queue", typeof(RecordingStrategyQueue) }
        };

        private readonly List<IRecordingStrategy> _roots = new List<IRecordingStrategy>();

        private void FilleStrategyTypeMenu() {
            var menuStrategies = new ToolStripMenuItem("Strategies");
            foreach (var metricInfo in _strategyTypes) {
                var menu = menuStrategies.DropDownItems.Add(metricInfo.Key, null, metricToolStripMenuItem_Click);
                menu.Tag = metricInfo.Value;
            }
            contextMenuStripAdd.Items.Add(menuStrategies);
        }

        private void UpdateTotalDuration() {
            var ms = _roots.Sum(strategy => strategy.TotalDuration.TotalMilliseconds);
            labelLength.Text = TimeSpan.FromMilliseconds(ms).ToString();
        }

        private void metricToolStripMenuItem_Click(object sender, EventArgs e) {
            var strategyType = ((ToolStripItem)sender).Tag as Type;
            if (strategyType != null) {
                var strategy = (IRecordingStrategy) Activator.CreateInstance(strategyType);
                if (treeListView1.SelectedItem?.RowObject is RecordingStrategyProperty) {
                    var property = (RecordingStrategyProperty) treeListView1.SelectedItem.RowObject;

                    if (property.GetValueType() == typeof (List<IRecordingStrategy>)) {
                        property.GetAs<List<IRecordingStrategy>>().Add(strategy);
                    }
                    else if (property.GetValueType() == typeof (IRecordingStrategy)) {
                        property.Set(strategy);
                    }

                    treeListView1.RebuildAll(true);
                } else {
                    _roots.Add(strategy);
                    treeListView1.Roots = _roots;
                }
                UpdateTotalDuration();
            }
        }

        public RecordingStrategyEditor() {
            InitializeComponent();
            FilleStrategyTypeMenu();
            treeListView1.VirtualMode = true;
            treeListView1.OwnerDraw = true;
            treeListView1.FullRowSelect = true;
            treeListView1.CellEditActivation = ObjectListView.CellEditActivateMode.DoubleClick;

            treeListView1.CanExpandGetter = x => {
                if (x is IRecordingStrategy) {
                    if (((IRecordingStrategy) x).Properties.Any()) return true;
                }

                if (x is RecordingStrategyProperty) {
                    var property = (RecordingStrategyProperty) x;

                    if (property.GetValueType() == typeof (List<IRecordingStrategy>)) {
                        return true;
                    }
                    if (property.GetValueType() == typeof (IRecordingStrategy)) {
                        return true;
                    }
                }

                return false;
            };

            treeListView1.ChildrenGetter = x => {
                if (x is IRecordingStrategy) {
                    var strategy = ((IRecordingStrategy) x);
                    return strategy.Properties;
                }

                if (x is RecordingStrategyProperty) {
                    var property = (RecordingStrategyProperty)x;

                    if (property.GetValueType() == typeof (List<IRecordingStrategy>)) {
                        return property.GetAs<List<IRecordingStrategy>>();
                    }
                    if (property.GetValueType() == typeof (IRecordingStrategy)) {
                        var obj = property.GetAs<IRecordingStrategy>();
                        if (obj != null) {
                            return new object[] { property.GetAs<IRecordingStrategy>() };
                        }
                        return new object[0];
                    }
                }

                return new object[0];
            };

            olvColumn2.AspectGetter = x => {
                if (!(x is RecordingStrategyProperty)) return null;
                var property = (RecordingStrategyProperty)x;
                return property.GetValueType() != typeof (List<IRecordingStrategy>) ? property.Get() : null;
            };

            olvColumn2.IsEditable = true;
            treeListView1.CellEditStarting += TreeListView1_CellEditStarting;
            treeListView1.CellEditFinishing += TreeListView1_CellEditFinishing;
        }

        private void TreeListView1_CellEditFinishing(object sender, CellEditEventArgs e) {
            var property = (RecordingStrategyProperty)e.RowObject;

            if (property.GetValueType() == typeof (int)) {
                property.Set(int.Parse(((TextBox) e.Control).Text));
            }

            UpdateTotalDuration();
            e.Cancel = true;
        }

        private void TreeListView1_CellEditStarting(object sender, CellEditEventArgs e) {
            if (!(e.RowObject is RecordingStrategyProperty)) {
                e.Cancel = true;
                return;
            }

            var property = (RecordingStrategyProperty) e.RowObject;
            Control editCtrl = null;

            if (property.GetValueType() == typeof (int)) {
                editCtrl = new TextBox {Bounds = e.CellBounds, Text=property.Get().ToString()};
            }
            
            e.Control = editCtrl;
            e.Cancel = e.Control == null;
        }

        private void buttonAdd_MouseUp(object sender, MouseEventArgs e) {
            contextMenuStripAdd.Show(buttonAdd, e.Location);
        }
    }
}
