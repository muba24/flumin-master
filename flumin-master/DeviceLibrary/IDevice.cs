using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceLibrary {

    public interface IDevice {

        event EventHandler<DeviceErrorArgs> OnError;

        string Name { get; }

        Guid UniqueId { get; }

        IEnumerable<IDevicePort> Ports { get; }

        int Id { get; }

        IEnumerable<IDevicePort> ListeningPorts { get; }

        bool Recording { get; }

        bool StartSampling();

        void StopSampling();
        
    }

    public class DeviceErrorArgs : EventArgs {
        public string Description { get; }
        public DateTime Date { get; }
        public DeviceErrorArgs(string description) {
            Description = description;
            Date = DateTime.Now;
        }
    }

}
