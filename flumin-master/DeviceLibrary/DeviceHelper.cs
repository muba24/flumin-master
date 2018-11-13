using System;
using System.Collections.Generic;using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;

namespace DeviceLibrary {

    static class NidaQmxHelper {
        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxGetSysDevNames(StringBuilder buffer, int bufsize);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxGetDevTerminals(string dev, StringBuilder buffer, int bufsize);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxGetDevDILines(string dev, StringBuilder buffer, int bufSize);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxGetDevDOLines(string dev, StringBuilder buffer, int bufSize);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxGetDevDIMaxRate(string dev, ref double rate);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxGetDevDOMaxRate(string dev, ref double rate);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxGetDevAIPhysicalChans(string dev, StringBuilder buffer, int bufsize);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxGetDevAOPhysicalChans(string dev, StringBuilder buffer, int bufsize);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxCreateTask(string task, int[] taskHandle);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxStopTask(int taskHandle);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxClearTask(int taskHandle);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxStartTask(int taskHandle);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxCreateDIChan(int taskHandle, string lines, string nameToAssignToChannel, int lineGrouping);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxCreateDOChan(int taskHandle, string lines, string nameToAssignToChannel, int lineGrouping);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxCreateAIVoltageChan(int taskHandle, string physicalChannel, string nameToAssignToChannel, int terminalConfig, double minVal, double maxVal, int units, string customScaleName);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxCreateAOVoltageChan(int taskHandle, string physicalChannel ,string nameToAssignToChannel, double minVal, double maxVal, int units, string customScaleName);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxCfgSampClkTiming(int taskHandle, string source, double rate, int activeEdge, int sampleMode, ulong sampsPerChan);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxRegisterEveryNSamplesEvent(int task, int everyNsamplesEventType, uint nSamples, uint options, DaQmxEveryNSamplesEventCallbackPtr callbackFunction, IntPtr callbackData);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxRegisterDoneEvent(int task, uint options, DaQmxDoneEventCallbackPtr callbackFunction, IntPtr callbackData);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxRegisterSignalEvent(int task, int signalId, uint options, DaQmxSignalEventCallbackPtr callbackFunction, IntPtr callbackData);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxReadAnalogF64(int taskHandle, int numSampsPerChan, double timeout, bool fillMode, IntPtr readArray, uint arraySizeInSamps, out int sampsPerChanRead, IntPtr reserved);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxWriteAnalogF64(int taskHandle, int numSampsPerChan, int autoStart, double timeout, bool dataLayout, IntPtr writeArray, out int sampsPerChanWritten, IntPtr reserved);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxGetDevAIMaxMultiChanRate(string device, out double data);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxCfgOutputBuffer(int taskHandle, uint numSampsPerChan);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxGetBufInputBufSize(int taskHandle, out uint samples);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxGetBufOutputBufSize(int taskHandle, out uint samples);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxSetWriteRegenMode(int taskHandle, int data);

        [DllImport("nicaiu.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int DAQmxGetErrorString(int errCode, StringBuilder errorString, int bufferSize);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int DaQmxEveryNSamplesEventCallbackPtr(int taskHandle, int everyNsamplesEventType, uint nSamples, IntPtr callbackData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int DaQmxDoneEventCallbackPtr(int taskHandle, int status, IntPtr callbackData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int DaQmxSignalEventCallbackPtr(int taskHandle, int signalId, IntPtr callbackData);

        public const int DAQmx_Val_AllowRegen = 10097;
        public const int DAQmx_Val_DoNotAllowRegen = 10158;

        public const int DaQmxValCfgDefault = -1;
        public const int DaQmxValVolts = 10348;

        public const int DaQmxValRising = 10280;
        public const int DaQmxValFalling = 10171;

        public const int DaQmxValFiniteSamps = 10178;
        public const int DaQmxValContSamps = 10123;
        public const int DaQmxValHwTimedSinglePoint = 12522;

        public const int DaQmxValAcquiredIntoBuffer = 1;
        public const int DaQmxValTransferredFromBuffer = 2;

        public const int DAQmx_Val_ChanPerLine = 0;
        public const int DAQmx_Val_ChanForAllLines = 1;

        public const bool DaQmxValGroupByChannel = false;
        public const bool DaQmxValGroupByScanNumber = true;

        public const int DaQmxValRse = 10083;
        public const int DaQmxValNrse = 10078;
        public const int DaQmxValDiff = 10106;
        public const int DaQmxValPseudoDiff = 12529;

        public enum TerminalCfg {
            NRSE = NidaQmxHelper.DaQmxValNrse,
            Diff = NidaQmxHelper.DaQmxValDiff,
            PseudoDiff = NidaQmxHelper.DaQmxValPseudoDiff,
            RSE = NidaQmxHelper.DaQmxValRse
        }

        public static string GetError(int code) {
            var buffer = new StringBuilder(1024);
            DAQmxGetErrorString(code, buffer, buffer.Capacity);
            return buffer.ToString();
        }
    }
}
