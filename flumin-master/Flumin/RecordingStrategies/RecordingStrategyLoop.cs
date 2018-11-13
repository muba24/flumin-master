using System;
using System.Collections.Generic;

namespace Flumin.RecordingStrategies {

    public class RecordingStrategyLoop : IRecordingStrategy {

        private readonly RecordingStrategyProperty _loopable;
        private readonly RecordingStrategyProperty _loopCountTotal;
        private int _loopCount;

        public RecordingStrategyLoop() : this(null, 0) { }

        public RecordingStrategyLoop(IRecordingStrategy loopable, int count) {
            _loopable = RecordingStrategyProperty.Make("Child", loopable);
            _loopCountTotal = RecordingStrategyProperty.Make("Loop Count", count);
            _loopCount = 0;
        }

        public void Reset() {
            LoopStrategy.Reset();
            _loopCount = 0;
        }

        public RecordSegmentInfo GetNextSegmentInfo() {
            RecordSegmentInfo result;
            do {
                result = LoopStrategy.GetNextSegmentInfo();
                if (result == null) _loopCount += 1;
            } while (result == null && _loopCount < _loopCountTotal.GetAs<int>());
            return result;
        }

        public IEnumerable<RecordingStrategyProperty> Properties => new[] { _loopCountTotal, _loopable };

        public TimeSpan TotalDuration {
            get {
                if (_loopable.GetAs<IRecordingStrategy>() != null) {
                    var ms = _loopCountTotal.GetAs<int>() * _loopable.GetAs<IRecordingStrategy>().TotalDuration.TotalMilliseconds;
                    return TimeSpan.FromMilliseconds(ms);
                }
                return TimeSpan.FromMilliseconds(0);
            }
        } 

        public int SegmentsTotal => _loopCountTotal.GetAs<int>() * LoopStrategy.SegmentsTotal;

        public int SegmentsRemaining => LoopStrategy.SegmentsRemaining + (_loopCountTotal.GetAs<int>() - _loopCount - 1) * LoopStrategy.SegmentsTotal;

        public string Name => "Loop";

        private IRecordingStrategy LoopStrategy => _loopable.GetAs<IRecordingStrategy>();
    }

}