using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;

namespace NewOpenGLRenderer {

    class ScrollTexture : IDisposable {

        readonly Texture _tex;
        int count = 0;

        public int Width => _tex.Width;
        public int Height => _tex.Height;

        public ScrollTexture(int width, int height) {
            _tex = new Texture(width, height);
        }

        public void AddFrame(int[] frame) {
            GL.BindTexture(TextureTarget.Texture2D, _tex.Id);
            unsafe
            {
                fixed (int* p = frame)
                {
                    _tex.Draw(new Rectangle(count, 0, 1, _tex.Height), new IntPtr(p));
                    count = (count + 1) % _tex.Width;
                }
            }
        }

        public void Draw(RectangleF rc) {
            GL.BindTexture(TextureTarget.Texture2D, _tex.Id);

            GL.Begin(BeginMode.Quads);
            GL.Color4(Color.White);
            GL.TexCoord2(0.0f + count / (float)Width, 1f); GL.Vertex2(rc.Left, rc.Bottom);
            GL.TexCoord2(1.0f + count / (float)Width, 1f); GL.Vertex2(rc.Right + 1, rc.Bottom);
            GL.TexCoord2(1.0f + count / (float)Width, 0f); GL.Vertex2(rc.Right + 1, rc.Top);
            GL.TexCoord2(0.0f + count / (float)Width, 0f); GL.Vertex2(rc.Left, rc.Top);
            GL.End();
        }

        public void Dispose() {
            _tex.Dispose();
        }
    }

}
