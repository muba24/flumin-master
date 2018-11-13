using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewOpenGLRenderer {

    public class ShaderColorXY {

        private Shader _shader;

        public ShaderColorXY() {
            CompileShaders();
        }

        public int AttributeX => _shader.GetAttributePosition("x");
        public int AttributeY => _shader.GetAttributePosition("y");
        public int Program    => _shader.Program;

        public void SetTranslateMatrix(Matrix4 matrix) {
            GL.UniformMatrix4(_shader.GetUniformPosition("translate_matrix"), false, ref matrix);
        }

        public void SetShaderMatrix(Matrix4 matrix) {
            GL.UniformMatrix4(_shader.GetUniformPosition("mvp_matrix"), false, ref matrix);
        }

        public void SetShaderColor(Color color) {
            var m = 1 / 256f;
            var color_pos = _shader.GetUniformPosition("base_color");
            GL.Uniform4(color_pos, color.R * m, color.G * m, color.B * m, 1f);
        }

        private void CompileShaders() {
            var vertexSource   = System.IO.File.ReadAllText("shader_vertex.glsl");
            var fragmentSource = System.IO.File.ReadAllText("shader_fragment.glsl");

            _shader = new Shader(ref vertexSource, ref fragmentSource);
        }
    }

}
