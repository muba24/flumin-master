#version 400

in float x;
in float y;

uniform mat4 mvp_matrix;
uniform mat4 translate_matrix;
uniform vec4 base_color;

varying vec4 color_result;

void main(void)
{
    color_result = base_color;
    gl_Position = mvp_matrix * translate_matrix * vec4(x, y, 0.0, 1.0);
}
