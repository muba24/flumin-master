using System;
using System.Collections.Generic;

namespace Flumin.RecordingStrategies {

    public class RecordingStrategySingleSegment : IRecordingStrategy {

        protected readonly bool Recording;

        protected readonly RecordingStrategyProperty TotalSeconds;

        private readonly Queue<RecordSegmentInfo> _segments = new Queue<RecordSegmentInfo>();

        public RecordingStrategySingleSegment(int seconds, bool recording) {
            TotalSeconds = RecordingStrategyProperty.Make("TotalDuration", seconds);
            Recording = recording;
            Reset();
        }
        
        public void Reset() {
            _segments.Clear();
            _segments.Enqueue(new RecordSegmentInfo() {
                Index = 0,
                Length = TimeSpan.FromSeconds(TotalSeconds.GetAs<int>()),
                Recording = Recording
            });
        }

        public RecordSegmentInfo GetNextSegmentInfo() {
            try {
                return _segments.Dequeue();
            } catch (InvalidOperationException) {
                return null;
            }
        }

        public IEnumerable<RecordingStrategyProperty> Properties => new[] {TotalSeconds};

        public TimeSpan TotalDuration => TimeSpan.FromSeconds(TotalSeconds.GetAs<int>());

        public int SegmentsTotal => 1;
        public int SegmentsRemaining => _segments.Count;

        public string Name => "Segment";
    }

}