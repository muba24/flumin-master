using System;
using System.Collections.Generic;

namespace Flumin.RecordingStrategies {

    public interface IRecordingStrategy {

        void Reset();

        RecordSegmentInfo GetNextSegmentInfo();

        IEnumerable<RecordingStrategyProperty> Properties { get; }

        TimeSpan TotalDuration { get; }

        int SegmentsTotal { get; }

        int SegmentsRemaining { get; }

        string Name { get; }

    }
}