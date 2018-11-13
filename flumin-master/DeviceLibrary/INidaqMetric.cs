using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceLibrary {
    interface INidaqMetric {

        NidaqSingleton.Device Device { get; }
        NidaqSingleton.Channel Channel { get; }
        NidaqSession Session { get; }
        NidaqSingleton.Channel.ChannelType Type { get; }

    }
}
