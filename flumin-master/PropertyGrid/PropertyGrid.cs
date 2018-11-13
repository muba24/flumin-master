using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.ObjectModel;

namespace CustomPropertyGrid {
    public partial class PropertyGrid : UserControl {

        private ObservableCollection<PropertyGridRow> _rows = new ObservableCollection<PropertyGridRow>();
        private bool _frozen;

        public PropertyGrid() {
            InitializeComponent();
            _rows.CollectionChanged += rows_CollectionChanged;

            olvColumnTitle.AspectGetter = (x) => {
                var row = x as PropertyGridRow;
                if (row == null) return "null";
                return row.Title;
            };

            olvColumnValue.AspectGetter = (x) => {
                var row = x as PropertyGridRow;
                if (row == null) return "null";
                return row.ValueString;
            };

            olvColumnTitle.IsVisible = false;
            olvColumnValue.IsVisible = false;
        }

        public void Freeze() {
            objectListView.Freeze();
            _frozen = true;
        }

        public void Unfreeze() {
            objectListView.Unfreeze();
            _frozen = false;
            UpdateGrid();
        }

        public Collection<PropertyGridRow> Properties => _rows;

        public void UpdateRowEnabledState() {
            for (int i = 0; i < _rows.Count; i++) {
                var item = objectListView.GetItem(i);
                var row = (PropertyGridRow)item.RowObject;
                item.ForeColor = row.Editable ? System.Drawing.SystemColors.ControlText : SystemColors.GrayText;
            }
            objectListView.Refresh();
        }

        public void UpdateRow(PropertyGridRow row) {
            objectListView.RefreshObject(row);
        }

        private void rows_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (var item in e.NewItems.OfType<PropertyGridRow>()) {
                    item.Changed += Item_Changed;
                }
            }

            if (e.OldItems != null) {
                foreach (var item in e.OldItems.OfType<PropertyGridRow>()) {
                    item.Changed -= Item_Changed;
                }
            }

            objectListView.FinishCellEdit();
            UpdateGrid();
        }

        private void Item_Changed(object sender, PropertyGridRow.PropertyGridRowChangedEventArgs e) {
            UpdateRow((PropertyGridRow)sender);
        }

        private void UpdateGrid() {
            if (!_frozen) {
                objectListView.Items.Clear();
                objectListView.AddObjects(_rows);
                UpdateRowEnabledState();
            }
        }

        private void objectListView_CellEditStarting(object sender, BrightIdeasSoftware.CellEditEventArgs e) {
            var row = e.RowObject as PropertyGridRow;

            if (row == null || !row.Editable) {
                e.Cancel = true;
                return;
            }

            e.AutoDispose = false;
            e.Control = row.EditControl;
            e.Control.Bounds = e.CellBounds;

            row.EditStart();
        }

        private void objectListView_CellEditFinishing(object sender, BrightIdeasSoftware.CellEditEventArgs e) {
            var row = e.RowObject as PropertyGridRow;

            if (row == null) {
                e.Cancel = true;
                return;
            }

            row.EditEnd(e.Cancel);
        }

        private void objectListView_CellEditValidating(object sender, BrightIdeasSoftware.CellEditEventArgs e) {
            var row = e.RowObject as PropertyGridRow;

            if (row == null) {
                e.Cancel = true;
                return;
            }

            if (!row.Validate()) {
                e.Cancel = true;
            }
        }

        private void objectListView_KeyPress(object sender, KeyPressEventArgs e) {

        }

        private void objectListView_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
                var row = (PropertyGridRow)objectListView.SelectedItem.RowObject;
                if (row != null && row.Editable) {
                    objectListView.StartCellEdit(objectListView.SelectedItem, 1);
                }
                e.SuppressKeyPress = true;
            }
        }
    }
}
