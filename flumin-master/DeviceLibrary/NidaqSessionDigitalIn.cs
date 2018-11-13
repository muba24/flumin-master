using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DeviceLibrary {

    class NidaqSessionDigitalIn : INidaqSessionTask {

        public NidaqSessionDigitalIn(NidaqSingleton.Device device, NidaqSession parent) {
            Parent = parent;
            Device = device;
        }

        public enum ClockSource {
            Intern, Extern
        }

        private readonly List<MetricDigitalInput> _nodes = new List<MetricDigitalInput>();

        private NidaqCounterOutput _counter;

        public ClockSource Source { get; set; }
        public string ClockPath { get; set; }
        public int ClockRate { get; set; }
        public int TaskHandle { get; private set; }
        public int SamplesPerChannel { get; private set; }

        public NidaqSession Parent { get; }
        public NidaqSingleton.Device Device { get; }
        public SessionTaskState State { get; private set; }

        public IReadOnlyList<INidaqMetric> Nodes => _nodes;

        public void CreateTask(IEnumerable<INidaqMetric> nodes) {
            if (TaskHandle != 0) throw new InvalidOperationException("Task already created. First destroy the old task or task already created");

            if (!nodes.All(n => n is MetricDigitalInput)) {
                throw new InvalidCastException("all passed nodes must be of type MetricDigitalInput");
            }

            if (!nodes.All(n => n.Channel.Device == Device)) {
                throw new InvalidOperationException("not all passed nodes are connected to device " + Device.Name);
            }

            // 1. create task
            if (Source == ClockSource.Intern) {
                _counter = new NidaqCounterOutput(Device.Name + "/ctr0", ClockRate);
            }

            var taskHandle = new int[1];
            var result = NidaQmxHelper.DAQmxCreateTask(null, taskHandle);
            if (result < 0) throw new NidaqException(result);
            TaskHandle = taskHandle[0];

            // 2. create channels
            foreach (var input in nodes.OfType<MetricDigitalInput>()) {
                result = NidaQmxHelper.DAQmxCreateDIChan(
                    lines: input.Channel.Path,
                    taskHandle: TaskHandle,
                    lineGrouping: NidaQmxHelper.DAQmx_Val_ChanForAllLines,
                    nameToAssignToChannel: null
                );
                if (result < 0) CleanupAndThrow(result);
                _nodes.Add(input);
            }

            // 3. configure clock
            SamplesPerChannel = ClockRate / 10;
            result = NidaQmxHelper.DAQmxCfgSampClkTiming(
                activeEdge: NidaQmxHelper.DaQmxValRising,
                sampleMode: NidaQmxHelper.DaQmxValContSamps,
                sampsPerChan: (ulong)SamplesPerChannel,
                taskHandle: TaskHandle,
                source: (this.Source == ClockSource.Intern) ? ("/" + Device.Name + "/Ctr0InternalOutput") : ClockPath,
                rate: ClockRate
            );
            if (result < 0) CleanupAndThrow(result);

            State = SessionTaskState.Stopped;
        }

        public void DestroyTask() {
            if (TaskHandle == 0) throw new InvalidOperationException("Task not yet created. First create a task");

            NidaQmxHelper.DAQmxClearTask(TaskHandle);
            TaskHandle = 0;
            SamplesPerChannel = 0;
            _nodes.Clear();
            if (_counter != null) _counter.Dispose();
            State = SessionTaskState.None;
        }

        public void Start() {
            if (TaskHandle == 0) throw new InvalidOperationException("Task not yet created. First create a task");

            try {
                _counter.Run();
            } catch (NationalInstruments.DAQmx.DaqException e) {
                CleanupAndThrow(e.Error);
            }

            var result = NidaQmxHelper.DAQmxStartTask(TaskHandle);
            if (result < 0) CleanupAndThrow(result);

            State = SessionTaskState.Running;
        }

        public void Stop() {
            if (TaskHandle == 0) throw new InvalidOperationException("Task not yet created. First create a task");

            var result = NidaQmxHelper.DAQmxStopTask(TaskHandle);
            if (result < 0) CleanupAndThrow(result);

            try {
                _counter.Stop();
            } catch (NationalInstruments.DAQmx.DaqException e) {
                CleanupAndThrow(e.Error);
            }

            State = SessionTaskState.Stopped;
        }

        private void CleanupAndThrow(int code) {
            NidaQmxHelper.DAQmxClearTask(TaskHandle);
            TaskHandle = 0;
            SamplesPerChannel = 0;
            _nodes.Clear();
            State = SessionTaskState.None;
            if (_counter != null) _counter.Dispose();
            throw new NidaqException(code);
        }

        public void Serialize(XmlWriter writer) {
            writer.WriteStartElement("task");
            writer.WriteAttributeString("type", "di");
            writer.WriteAttributeString("dev", Device.Name);
            writer.WriteAttributeString("rate", ClockRate.ToString());
            writer.WriteAttributeString("clksrc", ClockPath);
            writer.WriteEndElement();
        }

        public void LoadFactorySettings() {
            if (!NidaqSingleton.Instance.FactorySettings.ContainsKey(Parent.SessionGraph)) return;
            var factorySettings = NidaqSingleton.Instance.FactorySettings[Parent.SessionGraph];

            var taskSettings = factorySettings.SelectNodes("tasks/task");
            for (int i = 0; i < taskSettings.Count; i++) {
                var taskType = taskSettings[i].Attributes.GetNamedItem("type")?.Value ?? "";
                if (taskType == "di") {
                    ClockRate = int.Parse(taskSettings[i].Attributes.GetNamedItem("rate")?.Value ?? "0");
                    ClockPath = taskSettings[i].Attributes.GetNamedItem("clksrc")?.Value ?? "";
                    break;
                }
            }
        }
    }

}
