using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceLibrary {

    public interface IDeviceFactory2 {
        List<IDevice> CreateDevices();
        string  Name { get; }
    }


    public class DeviceFactory2 {

        private List<IDeviceFactory2> _factories = new List<IDeviceFactory2>();

        private static DeviceFactory2 _inst = null;

        public static DeviceFactory2 Instance
        {
            get
            {
                if (_inst == null) _inst = new DeviceFactory2();
                return _inst;
            }
        }

        private DeviceFactory2() {
            var a = typeof(IDeviceFactory2).Assembly;
            var t = typeof(IDeviceFactory2);

            var list = a.GetTypes().Where(type => type != t && t.IsAssignableFrom(type)).ToList();

            foreach (var factoryType in list) {
                _factories.Add((IDeviceFactory2)Activator.CreateInstance(factoryType, new IDGenerator()));
            }
        }

        public IReadOnlyList<IDeviceFactory2> Factories => _factories;

    }

    public interface IIDGenerator {
        int GetID();
    }

    class IDGenerator : IIDGenerator {

        private int _idCounter = 0;

        public int GetID() {
            return _idCounter++;
        }

    }

}
