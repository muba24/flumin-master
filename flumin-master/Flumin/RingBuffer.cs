using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SimpleADC {
    
    // TODO: There are a number of ring buffers in this file.
    // The top one should become the only one in the future.
    // 
    // For this to happen buffer resize functionality should be removed
    // from nodes, so that they need to allocate space once and then only
    // use that space. If the space is full, there should be a an exception
    // ultimately stopping the graph.

    public class RingBuffer<T> : IEnumerable, IEnumerable<T>, ICloneable {

        private T[] _buffer;
        private int _usedCapacity;
        private int _length;
        private int _read;
        private int _write;

        public RingBuffer(int size) {
            _buffer       = new T[size];
            _usedCapacity = size;
            _read         = 0;
            _write        = 0;
        }

        public object Clone() {
            var rb                = new RingBuffer<T>(_buffer.Length);

            rb.IgnoreDataOverflow = IgnoreDataOverflow;
            rb._usedCapacity      = _usedCapacity;
            rb._read              = _read;
            rb._write             = _write;
            rb._length            = _length;

            Array.Copy(
                destinationArray: rb._buffer, 
                sourceArray:      _buffer, 
                length:           _buffer.Length
            );

            return rb;
        }

        public int Capacity => _usedCapacity;

        public int Length => _length;

        public int FreeLength => Capacity - Length;

        public bool IgnoreDataOverflow { get; set; }

        public void Resize(int newSize) {
            var newBuffer = new T[newSize];

            var elems = Length;
            var readWithOffset = (newSize < Capacity) ? _read + (Capacity - newSize) : _read;

            CopyArray(_buffer, readWithOffset % Capacity, newBuffer, 0, Capacity - readWithOffset);
            CopyArray(_buffer, 0, newBuffer, Capacity - readWithOffset, _write);

            _buffer = newBuffer;
            _read = 0;
            _write = Math.Min(newSize, elems) % Capacity;
            _length = Math.Min(newSize, elems);
        }

        public int Peek(T[] data, int offset, int size) {
            if (size > Length) {
                offset = size - Length;
                size = Length;
            }

            var availUpper = Capacity - _read;
            if (size <= availUpper) {
                CopyArray(_buffer, _read, data, offset, size);
            } else {
                CopyArray(_buffer, _read, data, offset, availUpper);
                CopyArray(_buffer, 0, data, offset + availUpper, size - availUpper);
            }

            return size;
        }

        public int Read(T[] data) {
            return Read(data, 0, data.Length);
        }

        public int Read(T[] data, int offset, int size) {
            if (size > Length) {
                offset = size - Length;
                size = Length;
            }

            var availUpper = Capacity - _read;
            if (size <= availUpper) {
                CopyArray(_buffer, _read, data, offset, size);
            } else {
                CopyArray(_buffer, _read, data, offset, availUpper);
                CopyArray(_buffer, 0, data, offset + availUpper, size - availUpper);
            }

            _read = (_read + size) % Capacity;
            _length = Math.Max(0, _length - size);

            return size;
        }

        public void Write(T elem) {
            if (!IgnoreDataOverflow && FreeLength < 1) {
                throw new OutOfMemoryException("Data too big to write to buffer");
            }

            if (IgnoreDataOverflow && FreeLength == 0) {
                _read = (_read + 1) % Capacity;
            }

            _buffer[_write] = elem;
            _write = (_write + 1) % Capacity;

            _length = Math.Min(Capacity, _length + 1);
        }

        public void Write(T[] data) {
            Write(data, 0, data.Length);
        }

        public void Write(T[] data, int offset, int size) {
            if (!IgnoreDataOverflow) {
                if (size > FreeLength) {
                    throw new OutOfMemoryException($"Data too big to write to buffer. Capacity {Capacity}, Buffer size {size}");
                }
            } else {
                if (size > Capacity) {
                    offset = size - Capacity;
                    size = Capacity;
                }
                if (size > FreeLength) {
                    _read = (_write + size) % Capacity;
                }
            }

            var availUpper = Capacity - _write;
            if (size <= availUpper) {
                CopyArray(data, offset, _buffer, _write, size);
            } else {
                CopyArray(data, offset, _buffer, _write, availUpper);
                CopyArray(data, offset + availUpper, _buffer, 0, size - availUpper);
            }
            _write = (_write + size) % Capacity;
            _length = Math.Min(Capacity, _length + size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyArray(T[] src, int srcOffset, T[] dst, int dstOffset, int elems) {
            Array.Copy(src, srcOffset, dst, dstOffset, elems);
        }

        public IEnumerator GetEnumerator() {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            for (int i = 0; i < Length; i++) {
                yield return _buffer[(_read + i) % Capacity];
            }
        }
    }



    public sealed class SignalRingBuffer {
        private int _fHead;
        private int _fTail;
        private int _fSizeUntilCut;
        private double[] _fInternalBuffer;
        private int _rate;
        private TimeStamp _timeBufEnd;

        public TimeStamp EndTime => _timeBufEnd;
        public TimeStamp BeginTime => _timeBufEnd.Sub(Length, _rate);

        /// <summary>
        /// Gets the length of the byte queue
        /// </summary>
        public int Length { get; private set; }

        public int Capacity => _fInternalBuffer.Length;

        public bool SizeFixed { get; set; } = true;

        public int Samplerate => _rate;

        /// <summary>
        /// Constructs a new instance of a byte queue.
        /// </summary>
        public SignalRingBuffer(int size, int rate) {
            _fInternalBuffer = new double[size];
            _rate = rate;
            Clear();
        }

        /// <summary>
        /// Clears the byte queue
        /// </summary>
        public void Clear() {
            _fHead = 0;
            _fTail = 0;
            Length = 0;
            _fSizeUntilCut = _fInternalBuffer.Length;
            _timeBufEnd = TimeStamp.Zero();
        }


        /// <summary>
        /// Extends the capacity of the bytequeue
        /// </summary>
        private void SetCapacity(int capacity) {
            var newBuffer = new double[capacity];

            if (Length > 0) {
                if (_fHead < _fTail) {
                    Buffer.BlockCopy(_fInternalBuffer, sizeof(double) * _fHead, newBuffer, 0, sizeof(double) * Length);
                } else {
                    Buffer.BlockCopy(_fInternalBuffer, sizeof(double) * _fHead, newBuffer, 0, sizeof(double) * (_fInternalBuffer.Length - _fHead));
                    Buffer.BlockCopy(_fInternalBuffer, 0, newBuffer, sizeof(double) * (_fInternalBuffer.Length - _fHead), sizeof(double) * _fTail);
                }
            }

            _fHead = 0;
            _fTail = Length;
            _fInternalBuffer = newBuffer;
        }

        internal SignalRingBuffer Clone() {
            var copy = new SignalRingBuffer(Capacity, _rate);
            Buffer.BlockCopy(_fInternalBuffer, 0, copy._fInternalBuffer, 0, sizeof(double) * Capacity);
            copy._fHead = _fHead;
            copy._fSizeUntilCut = _fSizeUntilCut;
            copy._fTail = _fTail;
            copy._timeBufEnd = new TimeStamp(_timeBufEnd);
            return copy;
        }


        /// <summary>
        /// Enqueues a buffer to the queue and inserts it to a correct position
        /// </summary>
        /// <param name="buffer">Buffer to enqueue</param>
        /// <param name="offset">The zero-based byte offset in the buffer</param>
        /// <param name="size">The number of bytes to enqueue</param>
        private void Enqueue(double[] buffer, int offset, int size) {
            if (size == 0)
                return;

            lock (this) {
                if ((Length + size) > _fInternalBuffer.Length) {
                    if (!SizeFixed) {
                        SetCapacity((Length + size + 2047) & ~2047);
                    } else if (SizeFixed) {
                        GlobalSettings.Instance.Errors.Add(new Error("Node: Buffer size too small. Lost data"));
                        var toSkip = Math.Min(Length, (Length + size) - _fInternalBuffer.Length);
                        Skip(toSkip);
                        if (size > _fInternalBuffer.Length) {
                            offset = size - _fInternalBuffer.Length;
                            size = _fInternalBuffer.Length;
                        }
                    }
                }
                //throw new ArgumentOutOfRangeException("not big enough");

                if (_fHead < _fTail) {
                    var rightLength = (_fInternalBuffer.Length - _fTail);

                    if (rightLength >= size) {
                        Buffer.BlockCopy(buffer, sizeof(double) * offset, _fInternalBuffer, sizeof(double) * _fTail, sizeof(double) * size);
                    } else {
                        Buffer.BlockCopy(buffer, sizeof(double) * offset, _fInternalBuffer, sizeof(double) * _fTail, sizeof(double) * rightLength);
                        Buffer.BlockCopy(buffer, sizeof(double) * (offset + rightLength), _fInternalBuffer, 0, sizeof(double) * (size - rightLength));
                    }
                } else {
                    Buffer.BlockCopy(buffer, sizeof(double) * offset, _fInternalBuffer, sizeof(double) * _fTail, sizeof(double) * size);
                }

                _fTail = (_fTail + size) % _fInternalBuffer.Length;
                Length += size;
                _fSizeUntilCut = _fInternalBuffer.Length - _fHead;
            }
        }

        public void Enqueue(TimeLocatedBuffer buffer) {
            lock (this) {
                if (buffer.Samplerate != _rate) throw new InvalidOperationException();
                Enqueue(buffer.GetSamples(), 0, buffer.WrittenSamples);
                _timeBufEnd = _timeBufEnd.Add(buffer.WrittenSamples, _rate);

                if (buffer.CurrentTime.Value != _timeBufEnd.Value) {
                    GlobalSettings.Instance.Errors.Add(
                        new Error($"Ring Buffer: Missing samples. Buffer Timestamp: { buffer.CurrentTime.Value } \t Ring Timestamp: { _timeBufEnd.Value}")
                    );
                    _timeBufEnd = new TimeStamp(buffer.CurrentTime);
                }
            }
        }

        /// <summary>
        /// Dequeues a buffer from the queue
        /// </summary>
        /// <param name="buffer">Buffer to enqueue</param>
        /// <param name="offset">The zero-based byte offset in the buffer</param>
        /// <param name="size">The number of bytes to dequeue</param>
        /// <returns>Number of bytes dequeued</returns>
        private int Dequeue(double[] buffer, int offset, int size) {
            lock (this) {
                if (size > Length)
                    size = Length;

                if (size == 0)
                    return 0;

                if (_fHead < _fTail) {
                    Buffer.BlockCopy(_fInternalBuffer, sizeof(double) * _fHead, buffer, sizeof(double) * offset, sizeof(double) * size);
                } else {
                    int rightLength = (_fInternalBuffer.Length - _fHead);

                    if (rightLength >= size) {
                        Buffer.BlockCopy(_fInternalBuffer, sizeof(double) * _fHead, buffer, sizeof(double) * offset, sizeof(double) * size);
                    } else {
                        Buffer.BlockCopy(_fInternalBuffer, sizeof(double) * _fHead, buffer, sizeof(double) * offset, sizeof(double) * rightLength);
                        Buffer.BlockCopy(_fInternalBuffer, 0, buffer, sizeof(double) * (offset + rightLength), sizeof(double) * (size - rightLength));
                    }
                }

                Skip(size);
                return size;
            }
        }

        public int Dequeue(TimeLocatedBuffer buffer, int length = -1) {
            lock (this) {
                if (length == -1) length = buffer.GetSamples().Length;
                if (buffer.Samplerate != _rate) throw new InvalidOperationException();

                var result = Dequeue(buffer.GetSamples(), 0, length);
                buffer.SetWritten(result);
                return result;
            }
        }

        public double[] PeekAllData() {
            var result = new double[Length];

            if (_fHead < _fTail) {
                Buffer.BlockCopy(_fInternalBuffer, sizeof(double) * _fHead, result, 0, sizeof(double) * Length);
            } else {
                int rightLength = (_fInternalBuffer.Length - _fHead);

                if (rightLength >= Length) {
                    Buffer.BlockCopy(_fInternalBuffer, sizeof(double) * _fHead, result, 0, sizeof(double) * Length);
                } else {
                    Buffer.BlockCopy(_fInternalBuffer, sizeof(double) * _fHead, result, 0, sizeof(double) * rightLength);
                    Buffer.BlockCopy(_fInternalBuffer, 0, result, sizeof(double) * (0 + rightLength), sizeof(double) * (Length - rightLength));
                }
            }

            return result;
        }

        public void Skip(int size) {
            size = Math.Min(Length, size);
            _fHead = (_fHead + size) % _fInternalBuffer.Length;
            Length -= size;

            if (Length <= 0) {
                _fHead = 0;
                _fTail = 0;
                Length = 0;
            }

            _fSizeUntilCut = _fInternalBuffer.Length - _fHead;
        }

        public object GetState() {
            return Clone();
        }

        public void LoadState(object state) {
            var buffer = (SignalRingBuffer)state;
            _fHead = buffer._fHead;
            _fTail = buffer._fTail;
            _rate = buffer._rate;
            _timeBufEnd = new TimeStamp(buffer._timeBufEnd);
            _fSizeUntilCut = buffer._fSizeUntilCut;

            if (_fInternalBuffer.Length != buffer._fInternalBuffer.Length) {
                _fInternalBuffer = new double[buffer._fInternalBuffer.Length];
            }

            Array.Copy(
                sourceArray: buffer._fInternalBuffer,
                destinationArray: _fInternalBuffer,
                length: buffer._fInternalBuffer.Length
            );
        }

        /// <summary>
        /// Peeks a byte with a relative index to the fHead
        /// Note: should be used for special cases only, as it is rather slow
        /// </summary>
        /// <param name="index">A relative index</param>
        /// <returns>The byte peeked</returns>
        public double this[int index] => index >= _fSizeUntilCut
                                         ? _fInternalBuffer[index - _fSizeUntilCut]
                                         : _fInternalBuffer[_fHead + index];
    }


    public sealed class GenericRingBuffer<T> {
        private int _fHead;
        private int _fTail;
        private int _fSizeUntilCut;
        private T[] _fInternalBuffer;

        /// <summary>
        /// Gets the length of the byte queue
        /// </summary>
        public int Length { get; private set; }

        public int Capacity => _fInternalBuffer.Length;

        /// <summary>
        /// Constructs a new instance of a byte queue.
        /// </summary>
        public GenericRingBuffer(int size = 2048) {
            _fInternalBuffer = new T[size];
        }

        /// <summary>
        /// Clears the byte queue
        /// </summary>
        public void Clear() {
            _fHead = 0;
            _fTail = 0;
            Length = 0;
            _fSizeUntilCut = _fInternalBuffer.Length;
        }

        /// <summary>
        /// Clears the byte queue
        /// </summary>
        public void Clear(int size) {
            lock (this) {
                if (size > Length)
                    size = Length;

                if (size == 0)
                    return;

                _fHead = (_fHead + size) % _fInternalBuffer.Length;
                Length -= size;

                if (Length == 0) {
                    _fHead = 0;
                    _fTail = 0;
                }

                _fSizeUntilCut = _fInternalBuffer.Length - _fHead;
            }
        }

        /// <summary>
        /// Extends the capacity of the bytequeue
        /// </summary>
        private void SetCapacity(int capacity) {
            var newBuffer = new T[capacity];

            if (Length > 0) {
                if (_fHead < _fTail) {
                    Array.Copy(_fInternalBuffer, _fHead, newBuffer, 0, Length);
                } else {
                    Array.Copy(_fInternalBuffer, _fHead, newBuffer, 0, (_fInternalBuffer.Length - _fHead));
                    Array.Copy(_fInternalBuffer, 0, newBuffer, (_fInternalBuffer.Length - _fHead), _fTail);
                }
            }

            _fHead = 0;
            _fTail = Length;
            _fInternalBuffer = newBuffer;
        }


        public void Enqueue(T sample) {
            const int size = 1;

            lock (this) {
                if ((Length + size) > _fInternalBuffer.Length)
                    SetCapacity((Length + size + 2047) & ~2047);
                //throw new ArgumentOutOfRangeException("not big enough");

                _fInternalBuffer[_fTail] = sample;

                _fTail = (_fTail + size) % _fInternalBuffer.Length;
                Length += size;
                _fSizeUntilCut = _fInternalBuffer.Length - _fHead;
            }
        }


        /// <summary>
        /// Enqueues a buffer to the queue and inserts it to a correct position
        /// </summary>
        /// <param name="buffer">Buffer to enqueue</param>
        /// <param name="offset">The zero-based byte offset in the buffer</param>
        /// <param name="size">The number of bytes to enqueue</param>
        public void Enqueue(T[] buffer, int offset, int size) {
            if (size == 0)
                return;

            lock (this) {
                if ((Length + size) > _fInternalBuffer.Length)
                    SetCapacity((Length + size + 2047) & ~2047);
                //throw new ArgumentOutOfRangeException("not big enough");

                if (_fHead < _fTail) {
                    var rightLength = (_fInternalBuffer.Length - _fTail);

                    if (rightLength >= size) {
                        Array.Copy(buffer, offset, _fInternalBuffer, _fTail, size);
                    } else {
                        Array.Copy(buffer, offset, _fInternalBuffer, _fTail, rightLength);
                        Array.Copy(buffer, (offset + rightLength), _fInternalBuffer, 0, (size - rightLength));
                    }
                } else {
                    Array.Copy(buffer, offset, _fInternalBuffer, _fTail, size);
                }

                _fTail = (_fTail + size) % _fInternalBuffer.Length;
                Length += size;
                _fSizeUntilCut = _fInternalBuffer.Length - _fHead;
            }
        }

        /// <summary>
        /// Dequeues a buffer from the queue
        /// </summary>
        /// <param name="buffer">Buffer to enqueue</param>
        /// <param name="offset">The zero-based byte offset in the buffer</param>
        /// <param name="size">The number of bytes to dequeue</param>
        /// <returns>Number of bytes dequeued</returns>
        public int Dequeue(T[] buffer, int offset, int size) {
            lock (this) {
                if (size > Length)
                    size = Length;

                if (size == 0)
                    return 0;

                if (_fHead < _fTail) {
                    Array.Copy(_fInternalBuffer, _fHead, buffer, offset, size);
                } else {
                    int rightLength = (_fInternalBuffer.Length - _fHead);

                    if (rightLength >= size) {
                        Array.Copy(_fInternalBuffer, _fHead, buffer, offset, size);
                    } else {
                        Array.Copy(_fInternalBuffer, _fHead, buffer, offset, rightLength);
                        Array.Copy(_fInternalBuffer, 0, buffer, (offset + rightLength), (size - rightLength));
                    }
                }

                Skip(size);
                return size;
            }
        }

        public void Skip(int size) {
            lock (this) {
                if (_fInternalBuffer.Length == 0) return;
                _fHead = (_fHead + size) % _fInternalBuffer.Length;
                Length -= size;

                if (Length == 0) {
                    _fHead = 0;
                    _fTail = 0;
                }

                _fSizeUntilCut = _fInternalBuffer.Length - _fHead;
            }
        }

        /// <summary>
        /// Peeks a byte with a relative index to the fHead
        /// Note: should be used for special cases only, as it is rather slow
        /// </summary>
        /// <param name="index">A relative index</param>
        /// <returns>The byte peeked</returns>
        public T this[int index] {
            get {
                lock (this) {                    
                    var cut = _fSizeUntilCut;
                    return index >= cut
                        ? _fInternalBuffer[index - cut]
                        : _fInternalBuffer[_fHead + index];
                }
            }
        }
    }

}
