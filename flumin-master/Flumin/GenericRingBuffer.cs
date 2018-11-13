using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flumin {

    public sealed class GenericRingBuffer<T> {
        private int _fHead;
        private int _fTail;
        private int _fSizeUntilCut;
        private T[] _fInternalBuffer;

        /// <summary>
        /// Gets the length of the byte queue
        /// </summary>
        public int Length { get; private set; }

        public bool FixedSize { get; set; } = false;

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
            int size = 1;
            int offset = 0;

            lock (this) {
                if ((Length + size) > _fInternalBuffer.Length) {
                    if (FixedSize) {
                        if (Capacity == 0) return;
                        var toSkip = Math.Min(Length, (Length + size) - _fInternalBuffer.Length);
                        Skip(toSkip);
                        if (size > _fInternalBuffer.Length) {
                            offset = size - _fInternalBuffer.Length;
                            size = _fInternalBuffer.Length;
                        }
                    } else {
                        SetCapacity((Length + size + 2047) & ~2047);
                        //throw new ArgumentOutOfRangeException("not big enough");
                    }
                }


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
                if ((Length + size) > _fInternalBuffer.Length) {
                    if (FixedSize) {
                        var toSkip = Math.Min(Length, (Length + size) - _fInternalBuffer.Length);
                        Skip(toSkip);
                        if (size > _fInternalBuffer.Length) {
                            offset = size - _fInternalBuffer.Length;
                            size = _fInternalBuffer.Length;
                        }
                    } else {
                        SetCapacity((Length + size + 2047) & ~2047);
                        //throw new ArgumentOutOfRangeException("not big enough");
                    }
                }

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

        public int Peek(T[] buffer, int offsetSrc, int offsetDst, int size) {
            lock (this) {
                if (size + offsetSrc > Length)
                    size = Length - offsetSrc;

                if (size <= 0)
                    return 0;

                var head = (_fHead + offsetSrc) % Length;

                if (head + offsetSrc < _fTail) {
                    Array.Copy(_fInternalBuffer, head, buffer, offsetDst, size);
                } else {
                    int rightLength = (_fInternalBuffer.Length - head);

                    if (rightLength >= size) {
                        Array.Copy(_fInternalBuffer, head, buffer, offsetDst, size);
                    } else {
                        Array.Copy(_fInternalBuffer, head, buffer, offsetDst, rightLength);
                        Array.Copy(_fInternalBuffer, 0, buffer, (offsetDst + rightLength), (size - rightLength));
                    }
                }

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
