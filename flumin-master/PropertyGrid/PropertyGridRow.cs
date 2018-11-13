using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomPropertyGrid {

    public abstract class PropertyGridRow {

        public class PropertyGridRowChangedEventArgs : EventArgs {
            
        }

        public abstract event EventHandler<PropertyGridRowChangedEventArgs> Changed;

        public abstract string Title { get; }
        public abstract Control EditControl { get; }
        public abstract string ValueString { get; }

        public abstract bool Editable { get; }

        public abstract bool Validate();
        public abstract void EditStart();
        public abstract void EditEnd(bool cancel);

    }

}
