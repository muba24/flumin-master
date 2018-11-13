using System;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

namespace NewOpenGLRenderer {

    class Texture : IDisposable {

        public int Id { get; }
        public int Width { get; }
        public int Height { get; }

        public Texture(int width, int height) {
            Width = width;
            Height = height;
            Id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, Id);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            var rep = new [] {(int)TextureWrapMode.Repeat};
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, rep);
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, rep);

            GL.TexImage2D(TextureTarget.Texture2D, 0,
                            PixelInternalFormat.Rgba8,
                            width, height, 0,
                            PixelFormat.Bgra,
                            PixelType.UnsignedByte,
                            IntPtr.Zero);  

            CheckError();
        }

        public void Draw(Rectangle where, IntPtr pixels) {
            GL.BindTexture(TextureTarget.Texture2D, Id);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0,
                             where.X, where.Y, where.Width, where.Height,
                             PixelFormat.Bgra, PixelType.UnsignedByte,
                             pixels);
            CheckError();
        }

        public void Dispose() {
            GL.DeleteTexture(Id);
        }



        private static void CheckError() {
            ErrorCode ec = GL.GetError();
            if (ec != 0) {
                throw new System.Exception(ec.ToString());
            }
        }
    }

}
