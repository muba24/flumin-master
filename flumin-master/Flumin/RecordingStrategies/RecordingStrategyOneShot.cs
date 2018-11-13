namespace Flumin.RecordingStrategies {

    public class RecordingStrategyOneShot : RecordingStrategySingleSegment {

        public RecordingStrategyOneShot() : base(0, recording: true) { }

        public RecordingStrategyOneShot(int seconds) : base(seconds, recording: true) { }

        public new string Name => "Shot";

    }

}