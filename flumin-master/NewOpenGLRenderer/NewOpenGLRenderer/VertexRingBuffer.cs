using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewOpenGLRenderer {
    class VertexRingBuffer {

        private readonly float[]     _vertexData;
        private readonly int[]       _indexData;
        private readonly int         _vboIdX;
        private readonly int         _vboIdY;
        private readonly int         _eboId;
        private int                  _posHead;
        private int                  _length;

        public int VboX             => _vboIdX;
        public int VboY             => _vboIdY;
        public int Ebo              => _eboId;
        public int HeadPosition     => _posHead;
        public int Capacity         => _vertexData.Length;


        public int Length {
            get { return _length; }
            set {
                _length = value;
                ResetPointer();
            }
        }


        public VertexRingBuffer(int capacity) {
            _vertexData = new float[capacity];
            _indexData = new int[capacity];

            for (var i = 0; i < _indexData.Length; i++) {
                _indexData[i] = i;
                _vertexData[i] = i;
            }

            _vboIdX = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboIdX);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(_vertexData.Length * sizeof(float)), _vertexData, BufferUsageHint.DynamicDraw);

            _vboIdY = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboIdY);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(_vertexData.Length * sizeof(float)), _vertexData, BufferUsageHint.DynamicDraw);

            _eboId = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _eboId);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(_indexData.Length * sizeof(uint)), _indexData, BufferUsageHint.DynamicDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            Length = capacity;
        }

        public void ResetPointer() {
            _posHead = 0;
        }

        public void SetVerticesX(float[] x) {
            if (x.Length < _indexData.Length) throw new IndexOutOfRangeException("X array's length must at least match this buffers capacity");

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboIdX);

            unsafe
            {
                fixed (float* px = x)
                {
                    GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(0 * sizeof(float)), (IntPtr)(x.Length * sizeof(float)), (IntPtr)px);
                }
            }
        }


        public void AddVertices(float[] y, int offset, int count) {
            if (count > Length) throw new IndexOutOfRangeException();
            if (count <= 0) return;

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboIdY);

            unsafe
            {
                fixed (float* py = y)
                {
                    if (_posHead + count < Length) {
                        // y passt komplett zwischen Head und Array-Ende
                        GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(_posHead * sizeof(float)), (IntPtr)(count * sizeof(float)), (IntPtr)(py + offset));
                        Buffer.BlockCopy(y, offset, _vertexData, _posHead, count);
                    } else {
                        int headToEnd = Length - _posHead;

                        // ersten Teil von y kopieren in den Bereich zwischen Head und Arrayende
                        GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(_posHead * sizeof(float)), (IntPtr)(headToEnd * sizeof(float)), (IntPtr)(py + offset));
                        Buffer.BlockCopy(y, offset, _vertexData, _posHead, headToEnd);

                        // zweiten Teil von y kopieren in den Bereich von Arrayanfang und Head
                        GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(0), (IntPtr)((count - headToEnd) * sizeof(float)), (IntPtr)(py + offset + headToEnd));
                        Buffer.BlockCopy(y, offset + headToEnd, _vertexData, 0, (count - headToEnd));
                    }
                }
            }

            _posHead = (_posHead + count) % Length;
        }

    }
}
