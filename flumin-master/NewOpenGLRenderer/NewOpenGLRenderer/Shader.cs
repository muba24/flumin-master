using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewOpenGLRenderer {

    public class Shader : IDisposable {
        public string VertexSource { get; private set; }
        public string FragmentSource { get; private set; }

        public int VertexID { get; private set; }
        public int FragmentID { get; private set; }

        public int Program { get; private set; }

        private readonly Dictionary<string, int> Attributes = new Dictionary<string, int>();
        private readonly Dictionary<string, int> Uniforms = new Dictionary<string, int>();

        public int GetUniformPosition(string name) {
            if (!Uniforms.ContainsKey(name)) {
                var position = GL.GetUniformLocation(Program, name);
                Uniforms.Add(name, position);
            }
            return Uniforms[name];
        }

        public int GetAttributePosition(string name) {
            if (!Attributes.ContainsKey(name)) {
                var position = GL.GetAttribLocation(Program, name);
                if (position < 0) throw new IndexOutOfRangeException();
                Attributes.Add(name, position);
            }
            return Attributes[name];
        }

        public Shader(ref string vs, ref string fs) {
            VertexSource = vs;
            FragmentSource = fs;

            Build();
        }

        private void Build() {
            int statusCode;
            string info;

            VertexID = GL.CreateShader(ShaderType.VertexShader);
            FragmentID = GL.CreateShader(ShaderType.FragmentShader);

            // Compile vertex shader
            GL.ShaderSource(VertexID, VertexSource);
            GL.CompileShader(VertexID);
            GL.GetShaderInfoLog(VertexID, out info);
            GL.GetShader(VertexID, ShaderParameter.CompileStatus, out statusCode);

            if (statusCode != 1)
                throw new ApplicationException(info);

            // Compile fragment shader
            GL.ShaderSource(FragmentID, FragmentSource);
            GL.CompileShader(FragmentID);
            GL.GetShaderInfoLog(FragmentID, out info);
            GL.GetShader(FragmentID, ShaderParameter.CompileStatus, out statusCode);

            if (statusCode != 1)
                throw new ApplicationException(info);

            Program = GL.CreateProgram();
            GL.AttachShader(Program, FragmentID);
            GL.AttachShader(Program, VertexID);
            GL.LinkProgram(Program);
        }

        public void Dispose() {
            if (Program != 0)
                GL.DeleteProgram(Program);
            if (FragmentID != 0)
                GL.DeleteShader(FragmentID);
            if (VertexID != 0)
                GL.DeleteShader(VertexID);
        }
    }

}
