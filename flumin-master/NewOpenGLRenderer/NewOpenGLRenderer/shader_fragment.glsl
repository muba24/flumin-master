#version 400

varying vec4 color_result;

void main(void)
{
    gl_FragColor = color_result;
}