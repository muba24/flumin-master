using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DeviceLibrary {

    class NidaqSessionAnalogIn : INidaqSessionTask {

        public NidaqSessionAnalogIn(NidaqSingleton.Device dev, NidaqSession parent) {
            Parent = parent;
            Device = dev;
        }

        private readonly List<MetricAnalogInput> _nodes = new List<MetricAnalogInput>();

        public int ClockRate { get; set; }
        public int TaskHandle { get; private set; }
        public int SamplesPerChannel { get; private set; }

        public NidaqSession Parent { get; }
        public NidaqSingleton.Device Device { get; }
        public IReadOnlyList<INidaqMetric> Nodes => _nodes;

        public SessionTaskState State {get;private set;}

        /// <summary>
        /// Creates a new DAQmx task and adds channels to it
        /// </summary>
        /// <param name="nodes">Graph nodes of type MetricAnalogInput</param>
        /// <exception cref="InvalidCastException">At least one element in <paramref name="nodes"/> not of type MetricAnalogInput</exception>
        /// <exception cref="InvalidOperationException">At least one element in <paramref name="nodes"/> not connected to the instance's specified device or task already created</exception>
        /// <exception cref="NidaqException">Task or channel could not be created</exception>
        public void CreateTask(IEnumerable<INidaqMetric> nodes) {
            if (TaskHandle != 0) throw new InvalidOperationException("Task already created. First destroy the old task");

            if (!nodes.All(n => n is MetricAnalogInput)) {
                throw new InvalidCastException("all passed nodes must be of type MetricAnalogInput");
            }

            if (!nodes.All(n => n.Channel.Device == Device)) {
                throw new InvalidOperationException("not all passed nodes are connected to device " + Device.Name);
            }

            foreach (var node in nodes.OfType<MetricAnalogInput>()) {
                node.Samplerate = ClockRate;
            }

            // 1. create task
            var taskHandle = new int[1];
            var result = NidaQmxHelper.DAQmxCreateTask(null, taskHandle);
            if (result < 0) throw new NidaqException(result);
            TaskHandle = taskHandle[0];

            // 2. create channels
            foreach (var input in nodes.OfType<MetricAnalogInput>()) {
                result = NidaQmxHelper.DAQmxCreateAIVoltageChan(
                    maxVal:                 input.VMax,
                    minVal:                 input.VMin,
                    units:                  NidaQmxHelper.DaQmxValVolts,
                    terminalConfig:         (int)input.TerminalConfig,
                    physicalChannel:        input.Channel.Path,
                    taskHandle:             TaskHandle,
                    nameToAssignToChannel:  null,
                    customScaleName:        null
                );
                if (result < 0) CleanupAndThrow(result);
                _nodes.Add(input);
            }

            // 3. configure clock
            SamplesPerChannel = ClockRate / 5;
            result = NidaQmxHelper.DAQmxCfgSampClkTiming(
                activeEdge:                 NidaQmxHelper.DaQmxValRising,
                sampleMode:                 NidaQmxHelper.DaQmxValContSamps,
                sampsPerChan:               (ulong)SamplesPerChannel,
                taskHandle:                 TaskHandle,
                source:                     "",
                rate:                       ClockRate
            );
            if (result < 0) CleanupAndThrow(result);

            State = SessionTaskState.Stopped;
        }

        public void DestroyTask() {
            if (TaskHandle != 0) {//throw new InvalidOperationException("Task not yet created. First create a task");
                NidaQmxHelper.DAQmxClearTask(TaskHandle);
                TaskHandle = 0;
            }

            SamplesPerChannel = 0;
            _nodes.Clear();
            State = SessionTaskState.None;
        }

        public void Start() {
            if (TaskHandle == 0) throw new InvalidOperationException("Task not yet created. First create a task");

            var result = NidaQmxHelper.DAQmxStartTask(TaskHandle);
            if (result < 0) CleanupAndThrow(result);

            State = SessionTaskState.Running;
        }

        public void Stop() {
            if (TaskHandle == 0) throw new InvalidOperationException("Task not yet created. First create a task");

            var result = NidaQmxHelper.DAQmxStopTask(TaskHandle);
            if (result < 0) CleanupAndThrow(result, false);

            State = SessionTaskState.Stopped;
        }

        private void CleanupAndThrow(int code, bool doThrow = true) {
            NidaQmxHelper.DAQmxClearTask(TaskHandle);
            TaskHandle = 0;
            SamplesPerChannel = 0;
            _nodes.Clear();
            State = SessionTaskState.None;

            if (doThrow) {
                throw new NidaqException(code);
            } else {
                Parent.SessionGraph.Context.Notify(
                    new NodeSystemLib2.Generic.GraphNotification(
                        NodeSystemLib2.Generic.GraphNotification.NotificationType.Error, 
                        NidaQmxHelper.GetError(code)
                    )
                );
            }
        }

        public void LoadFactorySettings() {
            if (!NidaqSingleton.Instance.FactorySettings.ContainsKey(Parent.SessionGraph)) return;
            var factorySettings = NidaqSingleton.Instance.FactorySettings[Parent.SessionGraph];

            var taskSettings = factorySettings.SelectNodes("tasks/task");
            for (int i = 0; i < taskSettings.Count; i++) {
                var taskType = taskSettings[i].Attributes.GetNamedItem("type")?.Value ?? "";
                if (taskType == "ai") {
                    ClockRate = int.Parse(taskSettings[i].Attributes.GetNamedItem("rate")?.Value ?? "0");
                    break;
                }
            }
        }

        public void Serialize(XmlWriter writer) {
            writer.WriteStartElement("task");
            writer.WriteAttributeString("type", "ai");
            writer.WriteAttributeString("dev", Device.Name);
            writer.WriteAttributeString("rate", ClockRate.ToString());
            writer.WriteEndElement();
        }
    }

}
