using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DeviceLibrary {

    class NILoop {

        public enum TASK_TYPE {
            TASK_TYPE_ANALOG_INPUT = 0,
            TASK_TYPE_DIGITAL_INPUT
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct HandleInfo {
            public int handle;
            public int type;
            public int samples_per_chan;
            public int buffer_size;

            [MarshalAs(UnmanagedType.SysInt)]
            public IntPtr mutex_buffers;

            public int result;
        }

        [DllImport("NILoop.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int start_polling(
            [MarshalAs(UnmanagedType.LPArray)] HandleInfo[] task_handle,
            int task_count
        );

        [DllImport("NILoop.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void stop_polling(
            int poll_handle
        );

        [DllImport("NILoop.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int read_buffer(
            int poll_handle,
            int task_handle,
            IntPtr ptr_data,
            int size
        );

    }

}
