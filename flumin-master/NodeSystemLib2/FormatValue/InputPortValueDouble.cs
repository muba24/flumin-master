﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FormatValue {

    public class InputPortValueDouble : InputPortValue<double> {

        public InputPortValueDouble(Node parent, string name) 
            : base(parent, name, PortDataTypes.TypeIdValueDouble)
        {
        }

    }

}
