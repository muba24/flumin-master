namespace Flumin.RecordingStrategies {
    public class RecordingStrategyPause : RecordingStrategySingleSegment {

        public RecordingStrategyPause() : base(0, false) { }

        public RecordingStrategyPause(int seconds) : base(seconds, recording: false) {}

        public new string Name => "Pause";

    }
}