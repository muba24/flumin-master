using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceLibrary {
    class DataOutputBuffer {

        private double[] _channelData;
        private int[] _written;

        public DataOutputBuffer(int channels, int samplesPerChannel) {
            _channelData = new double[channels * samplesPerChannel];
            _written = new int[channels];
            SamplesPerChannel = samplesPerChannel;
            ChannelCount = channels;
        }

        public int SamplesPerChannel { get; }

        public int ChannelCount { get; }

        public double[] Data => _channelData;

        public bool Full => _written.All(c => c == SamplesPerChannel);

        public void Reset() {
            for (int i = 0; i < ChannelCount; i++) {
                _written[i] = 0;
            }
        }

        public int GetSamplesWritten(int channel) {
            return _written[channel];
        }

        private int StartIndexForChannel(int channel) {
            return channel * SamplesPerChannel;
        }

        public int Write(double[] data, int offset, int samples, int channel) {
            var alreadyWritten      = GetSamplesWritten(channel);
            var spaceLeftInChannel  = SamplesPerChannel - alreadyWritten;
            var actuallyToBeWritten = Math.Min(spaceLeftInChannel, samples);

            Array.Copy(data, offset, _channelData, StartIndexForChannel(channel) + alreadyWritten, actuallyToBeWritten);
            _written[channel] += actuallyToBeWritten;

            return actuallyToBeWritten;
        }

    }
}
