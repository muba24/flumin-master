using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CustomPropertyGrid;
using NodeSystemLib2.Generic.NodeAttributes;

namespace Flumin {

    /*
     * 
     * PropertyGrid rows for node attributes
     * 
     * Supports several types of node attributes.
     * All PropertyGridRows must specify a control to appropriately edit an attribute.
     * 
     */

    /// <summary>
    /// Factory for creating a PropertyGrid row from a node attribute
    /// </summary>
    class PropertyRowNodeAttribute {

        private PropertyRowNodeAttribute() {}

        /// <summary>
        /// Create a grid row for a node attribute
        /// </summary>
        /// <param name="attr">Attribute to make viewable/editable in property grid</param>
        /// <returns>PropertyGrid row</returns>
        /// <exception cref="NotImplementedException">node attribute type not yet implemented</exception>
        public static CustomPropertyGrid.PropertyGridRow FromAttribute(NodeAttribute attr) {
            if (attr is AttributeValueDouble) return new PropertyRowNodeAttrDouble((AttributeValueDouble)attr);
            if (attr is AttributeValueInt) return new PropertyRowNodeAttrInt((AttributeValueInt)attr);
            if (attr is AttributeValueString) return new PropertyRowNodeAttrString((AttributeValueString)attr);
            if (attr is AttributeValueFile) return new PropertyRowNodeAttrFile((AttributeValueFile)attr);
            if (IsSubclassOfRawGeneric(typeof(AttributeValueEnum<>), attr.GetType())) return new PropertyRowNodeAttrEnum(attr);

            throw new NotImplementedException();
        }

        // http://stackoverflow.com/questions/457676/check-if-a-class-is-derived-from-a-generic-class
        static bool IsSubclassOfRawGeneric(Type generic, Type toCheck) {
            while (toCheck != null && toCheck != typeof(object)) {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur) {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

    }

    // ------------------------------------------------------------------------

    public class PropertyRowNodeAttrFile : CustomPropertyGrid.PropertyGridRow {

        private readonly AttributeValueFile _attr;
        private TableLayoutPanel _layout;
        private TextBox _textBoxPath;
        private Button _buttonSelect;

        public override event EventHandler<PropertyGridRowChangedEventArgs> Changed;

        public PropertyRowNodeAttrFile(AttributeValueFile attr) {
            _attr = attr;
            _attr.Changed += (s, e) => Changed?.Invoke(this, new PropertyGridRowChangedEventArgs());
            CreateLayout();
        }

        private void CreateLayout() {
            _textBoxPath = new TextBox {Dock = DockStyle.Fill, Padding = Padding.Empty, Margin = Padding.Empty};
            _buttonSelect = new Button {Dock = DockStyle.Fill, Padding = Padding.Empty, Margin = Padding.Empty, Text = "..."};

            _layout = new TableLayoutPanel();
            _layout.Padding = Padding.Empty;
            _layout.Margin = Padding.Empty;
            _layout.GrowStyle = TableLayoutPanelGrowStyle.AddColumns;

            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            _layout.Controls.Add(_textBoxPath, 0, 0);
            _layout.Controls.Add(_buttonSelect, 1, 0);
            
            _buttonSelect.Click += _buttonSelect_Click;
        }

        private void _buttonSelect_Click(object sender, EventArgs e) {
            var dlg = new OpenFileDialog {
                Filter = "All files (*.*)|*.*",
                Multiselect = false
            };

            var result = dlg.ShowDialog(GlobalSettings.Instance.ActiveEditor);
            if (result == DialogResult.OK) {
                _textBoxPath.Text = dlg.FileName;
            }
        }

        public override Control EditControl => _layout;

        public override bool Editable => _attr.Editable;

        public override string Title => _attr.Name;

        public override string ValueString => _attr.TypedGet() + $" {_attr.Unit}";

        public override void EditEnd(bool cancel) {
            if (!cancel && Validate()) {
                _attr.Set(_textBoxPath.Text);
            }
        }

        public override void EditStart() {
            _textBoxPath.Text = _attr.TypedGet();
        }

        public override bool Validate() {
            return true;
        }
    }

    // ------------------------------------------------------------------------

    public class PropertyRowNodeAttrDouble : CustomPropertyGrid.PropertyGridRow {

        private readonly PropertyRowContainer<TextBox> _box;
        private readonly AttributeValueDouble _attr;

        public override event EventHandler<PropertyGridRowChangedEventArgs> Changed;

        public PropertyRowNodeAttrDouble(AttributeValueDouble attr) {
            _box = new PropertyRowContainer<TextBox>(new TextBox(), attr.Unit);
            _attr = attr;
            _attr.Changed += _attr_Changed;
        }

        private void _attr_Changed(object sender, AttributeChangedEventArgs e) {
            Changed?.Invoke(this, new PropertyGridRowChangedEventArgs());
        }

        public override Control EditControl => _box;

        public override bool Editable => _attr.Editable;

        public override string Title => _attr.Name;

        public override string ValueString => _attr.TypedGet().ToString(System.Globalization.CultureInfo.InvariantCulture) + $" {_attr.Unit}";

        public override void EditEnd(bool cancel) {
            if (!cancel && Validate()) {
                _attr.Set(double.Parse(_box.UserControl.Text, System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        public override void EditStart() {
            _box.UserControl.Text = _attr.TypedGet().ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public override bool Validate() {
            double result;
            return double.TryParse(_box.UserControl.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result);
        }
    }

    // ------------------------------------------------------------------------

    public class PropertyRowNodeAttrInt : CustomPropertyGrid.PropertyGridRow {

        private readonly PropertyRowContainer<TextBox> _box;
        private readonly AttributeValueInt _attr;

        public override event EventHandler<PropertyGridRowChangedEventArgs> Changed;

        public PropertyRowNodeAttrInt(AttributeValueInt attr) {
            _box = new PropertyRowContainer<TextBox>(new TextBox(), attr.Unit);
            _attr = attr;
            _attr.Changed += (s, e) => Changed?.Invoke(this, new PropertyGridRowChangedEventArgs());
        }

        public override Control EditControl => _box;

        public override string Title => _attr.Name;

        public override string ValueString => _attr.TypedGet() + $" {_attr.Unit}";

        public override bool Editable => _attr.Editable;

        public override void EditEnd(bool cancel) {
            if (!cancel && Validate()) {
                _attr.Set(int.Parse(_box.UserControl.Text));
            }
        }

        public override void EditStart() {
            _box.UserControl.Text = _attr.TypedGet().ToString();
        }

        public override bool Validate() {
            int result;
            return int.TryParse(_box.UserControl.Text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out result);
        }
    }

    // ------------------------------------------------------------------------

    public class PropertyRowNodeAttrString : CustomPropertyGrid.PropertyGridRow {

        private readonly TextBox _box = new TextBox();
        private readonly AttributeValueString _attr;

        public override event EventHandler<PropertyGridRowChangedEventArgs> Changed;

        public PropertyRowNodeAttrString(AttributeValueString attr) {
            _attr = attr;
            _attr.Changed += (s, e) => Changed?.Invoke(this, new PropertyGridRowChangedEventArgs());
        }

        public override Control EditControl => _box;

        public override bool Editable => _attr.Editable;

        public override string Title => _attr.Name;

        public override string ValueString => _attr.TypedGet() + $" {_attr.Unit}";

        public override void EditEnd(bool cancel) {
            if (!cancel && Validate()) {
                _attr.Set(_box.Text);
            }
        }

        public override void EditStart() {
            _box.Text = _attr.TypedGet();
        }

        public override bool Validate() {
            return true;
        }
    }

    // ------------------------------------------------------------------------

    public class PropertyRowNodeAttrEnum : CustomPropertyGrid.PropertyGridRow {

        private readonly PropertyRowContainer<ComboBox> _box;
        private readonly Type _enumType;
        private readonly NodeAttribute _attr;

        public override event EventHandler<PropertyGridRowChangedEventArgs> Changed;

        public override string Title => _attr.Name;

        public override Control EditControl => _box;

        public override string ValueString => _box.UserControl.SelectedItem?.ToString() ?? "";

        public override bool Editable => _attr.Editable;

        public PropertyRowNodeAttrEnum(NodeAttribute attr) {
            if (!(attr.GetValueType().IsEnum)) throw new InvalidOperationException("passed attribute does not contain enum");
            _enumType = attr.GetValueType();
            _attr = attr;
            _attr.Changed += (s, e) => Changed?.Invoke(this, new PropertyGridRowChangedEventArgs());

            _box = new PropertyRowContainer<ComboBox>(new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList }, attr.Unit);
            FillComboBox();
            Select(_attr.Get());
        }

        private void FillComboBox() {
            var values = Enum.GetValues(_enumType);

            foreach (var val in values) {
                _box.UserControl.Items.Add(val);
            }
        }

        private void Select(object sel) {
            _box.UserControl.SelectedItem = sel;
        }

        public override bool Validate() {
            // nothing to validate as all possible choices are valid ones (ComboBox in DropDownList mode)
            return true;
        }

        public override void EditStart() {
            Select(_attr.Get());
        }

        public override void EditEnd(bool cancel) {
            if (!cancel && Validate()) {
                try {
                    _attr.Set(_box.UserControl.SelectedItem);
                } catch (Exception) {}
            }
        }
    }

}
