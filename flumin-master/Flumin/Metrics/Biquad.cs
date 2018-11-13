using System;

namespace SimpleADC.Metrics {

    public class Biquad {

        public enum BiquadType {
            Lowpass = 0,
            Highpass,
            Bandpass,
            Notch,
            Peak,
            Lowshelf,
            Highshelf
        };

        private BiquadType _type;
        private double _a0, _a1, _a2, _b1, _b2;
        private double _fc, _q, _peakGain;
        private double _z1, _z2;

        public Biquad(BiquadType type, double fc, double q, double peakGainDb) {
            _type = type;
            _fc = fc;
            _q = q;
            _peakGain = peakGainDb;
            _z1 = _z2 = 0.0;
            CalcBiquad();
        }

        public BiquadType Type {
            get { return _type; }
            set { _type = value; CalcBiquad(); }
        }

        public double Fc {
            get { return _fc; }
            set { _fc = value; CalcBiquad(); }
        }

        public double Q {
            get { return _q; }
            set { _q = value; CalcBiquad(); }
        }

        public double PeakGainDb {
            get { return _peakGain; }
            set { _peakGain = value; CalcBiquad(); }
        }

        public double Process(double f) {
            var fout = f *_a0 + _z1;
            _z1 = f *_a1 + _z2 - _b1 * fout;
            _z2 = f *_a2 - _b2 * fout;
            return fout;
        }

        private void CalcBiquad() {
            double norm;
            var v = Math.Pow(10, Math.Abs(_peakGain) / 20.0);
            var k = Math.Tan(Math.PI * _fc);

            _z1 = 0;
            _z2 = 0;

            switch (_type) {
                case BiquadType.Lowpass:
                    norm = 1 / (1 + k / _q + k* k);
                    _a0 = k* k * norm;
                    _a1 = 2 * _a0;
                    _a2 = _a0;
                    _b1 = 2 * (k* k - 1) * norm;
                    _b2 = (1 - k / _q + k* k) * norm;
                    break;
            
                case BiquadType.Highpass:
                    norm = 1 / (1 + k / _q + k* k);
                    _a0 = 1 * norm;
                    _a1 = -2 * _a0;
                    _a2 = _a0;
                    _b1 = 2 * (k* k - 1) * norm;
                    _b2 = (1 - k / _q + k* k) * norm;
                    break;
            
                case BiquadType.Bandpass:
                    norm = 1 / (1 + k /_q + k* k);
                    _a0 = k / _q* norm;
                    _a1 = 0;
                    _a2 = -_a0;
                    _b1 = 2 * (k* k - 1) * norm;
                    _b2 = (1 - k / _q + k* k) * norm;
                    break;
            
                case BiquadType.Notch:
                    norm = 1 / (1 + k / _q + k* k);
                    _a0 = (1 + k* k) * norm;
                    _a1 = 2 * (k* k - 1) * norm;
                    _a2 = _a0;
                    _b1 = _a1;
                    _b2 = (1 - k / _q + k* k) * norm;
                    break;
            
                case BiquadType.Peak:
                    if (_peakGain >= 0) {    // boost
                        norm = 1 / (1 + 1/_q* k + k* k);
                        _a0 = (1 + v/_q* k + k* k) * norm;
                        _a1 = 2 * (k* k - 1) * norm;
                        _a2 = (1 - v/_q* k + k* k) * norm;
                        _b1 = _a1;
                        _b2 = (1 - 1/_q* k + k* k) * norm;
                    } else {    // cut
                        norm = 1 / (1 + v/_q* k + k* k);
                        _a0 = (1 + 1/_q* k + k* k) * norm;
                        _a1 = 2 * (k* k - 1) * norm;
                        _a2 = (1 - 1/_q* k + k* k) * norm;
                        _b1 = _a1;
                        _b2 = (1 - v/_q* k + k* k) * norm;
                    }
                    break;

                case BiquadType.Lowshelf:
                    if (_peakGain >= 0) {    // boost
                        norm = 1 / (1 + Math.Sqrt(2) * k + k* k);
                        _a0 = (1 + Math.Sqrt(2*v) * k + v* k * k) * norm;
                        _a1 = 2 * (v* k * k - 1) * norm;
                        _a2 = (1 - Math.Sqrt(2*v) * k + v* k * k) * norm;
                        _b1 = 2 * (k* k - 1) * norm;
                        _b2 = (1 - Math.Sqrt(2) * k + k* k) * norm;
                    }
                    else {    // cut
                        norm = 1 / (1 + Math.Sqrt(2*v) * k + v* k * k);
                        _a0 = (1 + Math.Sqrt(2) * k + k* k) * norm;
                        _a1 = 2 * (k* k - 1) * norm;
                        _a2 = (1 - Math.Sqrt(2) * k + k* k) * norm;
                        _b1 = 2 * (v* k * k - 1) * norm;
                        _b2 = (1 - Math.Sqrt(2*v) * k + v* k * k) * norm;
                    }
                    break;

                case BiquadType.Highshelf:
                    if (_peakGain >= 0) {    // boost
                        norm = 1 / (1 + Math.Sqrt(2) * k + k* k);
                        _a0 = (v + Math.Sqrt(2*v) * k + k* k) * norm;
                        _a1 = 2 * (k* k - v) * norm;
                        _a2 = (v - Math.Sqrt(2*v) * k + k* k) * norm;
                        _b1 = 2 * (k* k - 1) * norm;
                        _b2 = (1 - Math.Sqrt(2) * k + k* k) * norm;
                    }
                    else {    // cut
                        norm = 1 / (v + Math.Sqrt(2*v) * k + k* k);
                        _a0 = (1 + Math.Sqrt(2) * k + k* k) * norm;
                        _a1 = 2 * (k* k - 1) * norm;
                        _a2 = (1 - Math.Sqrt(2) * k + k* k) * norm;
                        _b1 = 2 * (k* k - v) * norm;
                        _b2 = (v - Math.Sqrt(2*v) * k + k* k) * norm;
                    }
                    break;
            }

        }

    }

}