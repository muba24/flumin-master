using System;

namespace DeviceLibrary {
    interface IMetricInput {
        void DistributeData(IntPtr pData, int samples);
        void SetBufferSize(int samples);
    }
}