using System;
using System.Collections.Generic;
using System.Linq;

namespace Flumin.RecordingStrategies {

    public class RecordingStrategyQueue : IRecordingStrategy {

        private readonly RecordingStrategyProperty _strategyProperty;
        private int _strategyIndex;

        public RecordingStrategyQueue() {
            _strategyProperty = RecordingStrategyProperty.Make("Children", new List<IRecordingStrategy>());
        }

        public void Reset() {
            foreach (var strategy in Strategies) strategy.Reset();
        }

        public RecordSegmentInfo GetNextSegmentInfo() {
            RecordSegmentInfo result;
            do {
                result = Strategies[_strategyIndex].GetNextSegmentInfo();
                if (result == null) _strategyIndex++;
            } while (result == null && _strategyIndex < Strategies.Count);

            return result;
        }

        public IEnumerable<RecordingStrategyProperty> Properties => new [] { _strategyProperty };

        public TimeSpan TotalDuration => TimeSpan.FromMilliseconds(_strategyProperty.GetAs<List<IRecordingStrategy>>().Sum(strategy => strategy.TotalDuration.TotalMilliseconds));

        public int SegmentsTotal => Strategies.Sum(strategy => strategy.SegmentsTotal);
        public int SegmentsRemaining => Strategies.Sum(strategy => strategy.SegmentsRemaining);

        public string Name => "Queue";

        public List<IRecordingStrategy> Strategies => _strategyProperty.GetAs<List<IRecordingStrategy>>();

    }

}