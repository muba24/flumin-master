using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments;

namespace DeviceLibrary {
    class NidaqCounterOutput : IDisposable {

        private NationalInstruments.DAQmx.Task _task;
        private NationalInstruments.DAQmx.COChannel _chanCo;
        private NationalInstruments.DAQmx.CounterSingleChannelWriter _coWriter;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source">For example "Dev1/ctr0"</param>
        /// <param name="frequency"></param>
        public NidaqCounterOutput(string source, double frequency) {
            var T = 1 / frequency;
            var t_low = T / 2;
            var t_high = T / 2;

            _task = new NationalInstruments.DAQmx.Task();
            _chanCo = _task.COChannels.CreatePulseChannelTime(
                source, 
                "", 
                NationalInstruments.DAQmx.COPulseTimeUnits.Seconds, 
                NationalInstruments.DAQmx.COPulseIdleState.Low, 
                0, 
                t_low, 
                t_high
            );

            _task.Timing.ConfigureImplicit(NationalInstruments.DAQmx.SampleQuantityMode.ContinuousSamples);
            
            try {
                _task.Control(NationalInstruments.DAQmx.TaskAction.Verify);
            } catch (NationalInstruments.DAQmx.DaqException e) {
                throw new NidaqException(e.Error);
            }
        }

        public void Run() {
            _task.Start();
        }

        public void Stop() {
            _task.Stop();
        }

        public void Dispose() {
            _task.Dispose();
        }
    }
}
