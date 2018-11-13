using System;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

namespace NewOpenGLRenderer {

    public class VertexFloatBuffer : IDisposable {
        
        public int VertexCount { get { return vertex_data.Length / AttributeCount; } }
        public int TriangleCount { get { return index_data.Length / 3; } }
        public int AttributeCount { get; }
        public int Stride { get; }

        public BufferUsageHint UsageHint { get; set; }
        public BeginMode DrawMode { get; set; }

        public bool IsLoaded { get; private set; }

        public int VBO { get { return id_vbo; } }
        public int EBO { get { return id_ebo; } }

        private int id_vbo;
        private int id_ebo;

        private int vertex_position;
        private int index_position;

        protected float[] vertex_data;
        protected uint[] index_data;

        public int Length => vertex_position / 2;

        public VertexFloatBuffer() : this(1024) { }

        public VertexFloatBuffer(int limit) {
            Stride = 8;
            AttributeCount = Stride / sizeof(float);

            UsageHint = BufferUsageHint.StreamDraw;
            DrawMode = BeginMode.Triangles;

            vertex_data = new float[limit * AttributeCount];
            index_data = new uint[limit];
        }

        public void Clear() {
            vertex_position = 0;
            index_position = 0;
        }

        public void Set(float[] vertices, uint[] indices) {
            Clear();
            vertex_data = vertices;
            index_data = indices;
        }

        /// <summary>
        /// Load vertex buffer into a VBO in OpenGL
        /// :: store in memory
        /// </summary>
        public void Load() {
            if (IsLoaded) return;

            //VBO
            GL.GenBuffers(1, out id_vbo);
            GL.BindBuffer(BufferTarget.ArrayBuffer, id_vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertex_position * sizeof(float)), vertex_data, UsageHint);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.GenBuffers(1, out id_ebo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, id_ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(index_position * sizeof(uint)), index_data, UsageHint);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            IsLoaded = true;
        }

        /// <summary>
        /// Reload the buffer data without destroying the buffers pointer to OpenGL
        /// </summary>
        public void Reload() {
            if (!IsLoaded) {
                Load();
                return;
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, id_vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertex_position * sizeof(float)), vertex_data, UsageHint);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, id_ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(index_position * sizeof(uint)), index_data, UsageHint);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        /// <summary>
        /// Unload vertex buffer from OpenGL
        /// :: release memory
        /// </summary>
        public void Unload() {
            if (!IsLoaded) return;

            GL.BindBuffer(BufferTarget.ArrayBuffer, id_vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertex_position * sizeof(float)), IntPtr.Zero, UsageHint);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, id_ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(index_position * sizeof(uint)), IntPtr.Zero, UsageHint);

            GL.DeleteBuffers(1, ref id_vbo);
            GL.DeleteBuffers(1, ref id_ebo);

            IsLoaded = false;
        }

        public void BindAndDraw(ShaderColorXY shader, int step = 1) {
            if (!IsLoaded) return;

            int strideAdd = Stride * step;

            GL.BindBuffer(BufferTarget.ArrayBuffer, id_vbo);
            GL.EnableVertexAttribArray(shader.AttributeX);
            GL.EnableVertexAttribArray(shader.AttributeY);
            GL.VertexAttribPointer(shader.AttributeX, 1, VertexAttribPointerType.Float, false, strideAdd, 0);
            GL.VertexAttribPointer(shader.AttributeY, 1, VertexAttribPointerType.Float, false, strideAdd, 4);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, id_ebo);
            GL.DrawElements(DrawMode, index_position, DrawElementsType.UnsignedInt, 0);

            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
            GL.DisableVertexAttribArray(2);
            GL.DisableVertexAttribArray(3);
        }

        public void Dispose() {
            Unload();
            Clear();
            vertex_data = null;
            index_data = null;
        }

        /// <summary>
        /// Add indices in order of vertices length,
        /// this is if you dont want to set indices and just index from vertex-index
        /// </summary>
        public void IndexFromLength() {
            int count = vertex_position / AttributeCount;
            index_position = 0;
            for (uint i = 0; i < count; i++) {
                index_data[index_position++] = i;
            }
        }

        public void AddIndex(uint indexA, uint indexB, uint indexC) {
            index_data[index_position++] = indexA;
            index_data[index_position++] = indexB;
            index_data[index_position++] = indexC;
        }

        public void AddIndices(uint[] indices) {
            for (int i = 0; i < indices.Length; i++) {
                index_data[index_position++] = indices[i];
            }
        }

        public void PopVertex() {
            vertex_position -= 2;
        }

        public void AddVertex(PointF p) {
            AddVertex(p.X, p.Y);
        }

        public void AddVertex(float x, float y) {
            vertex_data[vertex_position++] = x;
            vertex_data[vertex_position++] = y;
        }

        public PointF this [int index] {
            get {
                return new PointF(vertex_data[index * 2 + 0], 
                                  vertex_data[index * 2 + 1]);
            }
        }
        
    }

}