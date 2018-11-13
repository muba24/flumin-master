using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2 {

    public class RingBuffer<T> : IDisposable where T : struct {

        readonly T[] _data;
        readonly GCHandle _dataPin;

        private bool _disposed;

        /// <summary>
        /// Total amount of elements the ring buffer can take
        /// </summary>
        public int Capacity => _data.Length;

        /// <summary>
        /// Free space left in the buffer
        /// </summary>
        public int Free => Capacity - Available;

        /// <summary>
        /// Number of elements available for reading
        /// </summary>
        public int Available { get; private set; }

        /// <summary>
        /// Current read position in the buffer
        /// </summary>
        public int ReadPosition { get; private set; }

        /// <summary>
        /// Current write position in the buffer
        /// </summary>
        public int WritePosition { get; private set; }
        
        /// <summary>
        /// Allow overwriting non-read data
        /// </summary>
        public bool Overflow { get; set; } = false;



        public RingBuffer(int elements) {
            if (elements < 0) throw new ArgumentOutOfRangeException();
            _data = new T[elements];
            _dataPin = GCHandle.Alloc(_data, GCHandleType.Pinned);
        }


        private void WriteFromPtr(IntPtr elements, int offsetSrc, int offsetDst, int count) {
            int typeSize = Marshal.SizeOf<T>();

            RingBufferApiHelper.CopyMemory(
                IntPtr.Add(_dataPin.AddrOfPinnedObject(), offsetDst * typeSize),
                IntPtr.Add(elements, offsetSrc * typeSize),
                count * typeSize
            );
        }


        /// <summary>
        /// Writes data to the ring buffer. Not type safe!
        /// </summary>
        /// <param name="elements">pointer to data to write to buffer. Must be of type <typeparamref name="T"/></param>
        /// <param name="offset">offset in the <paramref name="elements"/> array, counted in elements of type <typeparamref name="T"/></param>
        /// <param name="count">number of elements to take from <paramref name="elements"/> starting at position <paramref name="offset"/>, counted in elements of type <typeparamref name=">"/></param>
        /// <returns>number of elements actually written. If there is no space left, this will return 0.</returns>
        /// <exception cref="ArgumentOutOfRangeException">indexing error</exception>
        public int Write(IntPtr elements, int offset, int count) {
            if (elements == IntPtr.Zero) throw new ArgumentNullException();
            if (offset < 0) throw new ArgumentOutOfRangeException();
            if (count < 0) throw new ArgumentOutOfRangeException();
            lock (this) {
                var totalCount = Overflow ? count : Math.Min(count, Free);
                var firstWriteCount = Math.Min(totalCount, Capacity - WritePosition);
                WriteFromPtr(elements, offset, WritePosition, firstWriteCount);
                WriteFromPtr(elements, offset + firstWriteCount, 0, totalCount - firstWriteCount);

                if (Overflow) {
                    var extendedReadPosition = (ReadPosition < WritePosition) ? ReadPosition + Capacity : ReadPosition;
                    if (WritePosition + totalCount > extendedReadPosition) {
                        WritePosition = (WritePosition + totalCount) % Capacity;
                        ReadPosition = WritePosition;
                    } else {
                        WritePosition = (WritePosition + totalCount) % Capacity;
                    }
                } else {
                    WritePosition = (WritePosition + totalCount) % Capacity;
                }

                Available = Math.Min(Capacity, Available + totalCount);
                return totalCount;
            }
        }

        /// <summary>
        /// Writes data to the ring buffer
        /// </summary>
        /// <param name="elements">array of elements to write</param>
        /// <param name="offset">offset in the <paramref name="elements"/> array</param>
        /// <param name="count">number of elements to take from <paramref name="elements"/> starting at position <paramref name="offset"/></param>
        /// <returns>number of elements actually written. If there is no space left, this will return 0.</returns>
        /// <exception cref="ArgumentOutOfRangeException">indexing error</exception>
        /// <exception cref="OutOfMemoryException"><paramref name="count"/> too big for buffer</exception>
        public int Write(T[] elements, int offset, int count) {
            if (elements == null) throw new ArgumentNullException();
            if (offset < 0) throw new ArgumentOutOfRangeException();
            if (count < 0) throw new ArgumentOutOfRangeException();

            if (count > Capacity) throw new OutOfMemoryException();

            lock (this) {
                var totalCount = Overflow ? count : Math.Min(count, Free);
                var firstWriteCount = Math.Min(totalCount, Capacity - WritePosition);
                Array.Copy(elements, offset, _data, WritePosition, firstWriteCount);
                Array.Copy(elements, offset + firstWriteCount, _data, 0, totalCount - firstWriteCount);

                //if (Overflow && ReadPosition > WritePosition && WritePosition + totalCount > ReadPosition) {
                if (Overflow) {
                    var extendedReadPosition = (ReadPosition < WritePosition) ? ReadPosition + Capacity : ReadPosition;
                    if (WritePosition + totalCount > extendedReadPosition) {
                        WritePosition = (WritePosition + totalCount) % Capacity;
                        ReadPosition = WritePosition;
                    } else {
                        WritePosition = (WritePosition + totalCount) % Capacity;
                    }
                } else {
                    WritePosition = (WritePosition + totalCount) % Capacity;
                }

                Available = Math.Min(Capacity, Available + totalCount);
                return totalCount;
            }
        }


        /// <summary>
        /// Writes data to the ring buffer
        /// </summary>
        /// <param name="elements">array of elements to write</param>
        /// <param name="offset">offset in the <paramref name="elements"/> array</param>
        /// <param name="count">number of elements to take from <paramref name="elements"/> starting at position <paramref name="offset"/></param>
        /// <returns>number of elements actually written. If there is no space left, this will return 0.</returns>
        /// <exception cref="ArgumentOutOfRangeException">indexing error</exception>
        /// <exception cref="OutOfMemoryException"><paramref name="count"/> too big for buffer</exception>
        public int Write(T[,] elements, int offset, int count) {
            if (elements == null) throw new ArgumentNullException();
            if (offset < 0) throw new ArgumentOutOfRangeException();
            if (count < 0) throw new ArgumentOutOfRangeException();

            if (count > Capacity) throw new OutOfMemoryException();
            var size = Marshal.SizeOf(typeof(T));

            lock (this) {
                var totalCount = Overflow ? count : Math.Min(count, Free);
                var firstWriteCount = Math.Min(totalCount, Capacity - WritePosition);
                Buffer.BlockCopy(elements, offset * size, _data, WritePosition * size, firstWriteCount * size);
                Buffer.BlockCopy(elements, (offset + firstWriteCount) * size, _data, 0, (totalCount - firstWriteCount) * size);

                //if (Overflow && ReadPosition > WritePosition && WritePosition + totalCount > ReadPosition) {
                if (Overflow) {
                    var extendedReadPosition = (ReadPosition < WritePosition) ? ReadPosition + Capacity : ReadPosition;
                    if (WritePosition + totalCount > extendedReadPosition) {
                        WritePosition = (WritePosition + totalCount) % Capacity;
                        ReadPosition = WritePosition;
                    } else {
                        WritePosition = (WritePosition + totalCount) % Capacity;
                    }
                } else {
                    WritePosition = (WritePosition + totalCount) % Capacity;
                }

                Available = Math.Min(Capacity, Available + totalCount);
                return totalCount;
            }
        }

        /// <summary>
        /// Reads data from the ring buffer
        /// </summary>
        /// <param name="elements">array of elements to read to</param>
        /// <param name="offset">offset in the <paramref name="elements"/> array</param>
        /// <param name="count">number of elements to read to <paramref name="elements"/> starting at position <paramref name="offset"/></param>
        /// <returns>number of elements actually read. If there is no data in the buffer, this will return 0.</returns>
        /// <exception cref="ArgumentOutOfRangeException">indexing error</exception>
        /// <exception cref="RankException"><paramref name="elements"/> has wrong dimension</exception>
        public int Read(T[] elements, int offset, int count) {
            if (elements == null) throw new ArgumentNullException();
            if (offset < 0) throw new ArgumentOutOfRangeException();
            if (count < 0) throw new ArgumentOutOfRangeException();
            lock (this) {
                var totalCount = Math.Min(count, Available);
                var firstReadCount = Math.Min(totalCount, Capacity - ReadPosition);
                Array.Copy(_data, ReadPosition, elements, offset, firstReadCount);
                Array.Copy(_data, 0, elements, firstReadCount, totalCount - firstReadCount);
                ReadPosition = (ReadPosition + totalCount) % Capacity;
                Available -= totalCount;
                return totalCount;
            }
        }

        /// <summary>
        /// Reads data from the ring buffer without incrementing the read pointer
        /// </summary>
        /// <param name="elements">array of elements to read to</param>
        /// <param name="offset">offset in the <paramref name="elements"/> array</param>
        /// <param name="count">number of elements to read to <paramref name="elements"/> starting at position <paramref name="offset"/></param>
        /// <returns>number of elements actually read. If there is no data in the buffer, this will return 0.</returns>
        /// <exception cref="ArgumentOutOfRangeException">indexing error</exception>
        /// <exception cref="RankException"><paramref name="elements"/> has wrong dimension</exception>
        public int Peek(T[] elements, int offset, int count) {
            if (elements == null) throw new ArgumentNullException();
            if (offset < 0) throw new ArgumentOutOfRangeException();
            if (count < 0) throw new ArgumentOutOfRangeException();
            lock (this) {
                var totalCount = Math.Min(count, Available);
                var firstReadCount = Math.Min(totalCount, Capacity - ReadPosition);
                Array.Copy(_data, ReadPosition, elements, offset, firstReadCount);
                Array.Copy(_data, 0, elements, firstReadCount, totalCount - firstReadCount);
                return totalCount;
            }
        }

        public void Dispose() {
            if (!_disposed) {
                _dataPin.Free();
                _disposed = true;
            }
        }
    }

    static class RingBufferApiHelper {

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, int count);

    }

}
