using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace NewOpenGLRenderer {
    public partial class WaterfallPlot : UserControl {

        private GLControl   _glctrl;
        private bool        _loaded;
        private Shader      _shaderTexture;
        private Waterfall   _waterfall;
        private bool        _showBallon;
        private QuickFont.QFont _font;

        public int Samplerate { get; private set; }
        public int FFTSize { get; private set; }

        public float DbMin {
            get { return _waterfall.DbMin; }
            set { _waterfall.DbMin = value; }
        }

        public float DbMax {
            get { return _waterfall.DbMax; }
            set { _waterfall.DbMax = value; }
        }

        public WaterfallPlot() {
            InitializeComponent();
        }

        private void CreateGLControl() {
            _glctrl = new GLControl {
                Location = new Point(0, 0),
                Visible = true,
                Width = this.Width,
                Height = this.Height
            };

            _glctrl.Load += _glctrl_Load;
            _glctrl.MouseEnter += _glctrl_MouseEnter;
            _glctrl.MouseLeave += _glctrl_MouseLeave;
            _glctrl.MouseMove += _glctrl_MouseMove;

            Controls.Add(_glctrl);
        }

        private void _glctrl_MouseMove(object sender, MouseEventArgs e) {
            OnMouseMove(e);
        }

        private void _glctrl_MouseLeave(object sender, EventArgs e) {
            _showBallon = false;
            Invalidate();
        }

        private void _glctrl_MouseEnter(object sender, EventArgs e) {
            _showBallon = true;
            Invalidate();
        }

        private void _glctrl_Load(object sender, EventArgs e) {
            _glctrl.MakeCurrent();
            GL.Disable(EnableCap.Multisample);
            GL.Disable(EnableCap.LineSmooth);
            GL.Disable(EnableCap.AlphaTest);
            GL.Disable(EnableCap.Blend);
            GL.ClearColor(Color.Black);
            _loaded = true;
            CreateTextureShader();
            CreateFont();
            WaterfallPlot_Resize(null, null);
        }

        public void Init(int fftSize, int fftCount, int samplerate) {
            FFTSize = fftSize;
            Samplerate = samplerate;

            _glctrl.MakeCurrent();
            CreateTexture(fftCount + 1);
        }

        private void CreateFont() {
            _font = new QuickFont.QFont(SystemFonts.CaptionFont);
            _font.Options.Colour = new OpenTK.Graphics.Color4(Color.White);
        }

        private void CreateTextureShader() {
            var vertex       = System.IO.File.ReadAllText("shader_vertex_texture.glsl");
            var fragment     = System.IO.File.ReadAllText("shader_fragment_texture.glsl");
            _shaderTexture = new Shader(ref vertex, ref fragment);
        }

        private void CreateTexture(int width) {
            _waterfall?.Dispose();
            _waterfall = new Waterfall(FFTSize, width, Samplerate);
        }

        private void SetupViewport() {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Viewport(0, 0, _glctrl.Width, _glctrl.Height);
        }

        public void AddFrame(double[] data) {
            _waterfall.AddFrame(data, 0, data.Length);
        }
        
        private void RenderBallonInfo() {
            var mousePos = _glctrl.PointToClient(Cursor.Position);

            var texPos = new Point(mousePos.X * _waterfall.FFTCapacity / _glctrl.Width,
                                   mousePos.Y * _waterfall.FFTSize / _glctrl.Height);

            var dataPos = new Point(texPos.X, (_waterfall.FFTSize - texPos.Y - 1) / 2);

            var info = _waterfall.InfoFromPoint(dataPos);
            if (info.Frequency < 0 || info.Amplitude < 0) return;
            var msg = $"Frequency: {Math.Round(info.Frequency, 2)}\nAmplitude: {Math.Round(info.Amplitude, 3)}";

            QuickFont.QFont.Begin();
            var textSize = _font.Measure(msg);
            var showAt = new Point((int)Math.Min(_glctrl.Width - textSize.Width, mousePos.X),
                                   (int)Math.Min(_glctrl.Height - textSize.Height, mousePos.Y));

            GL.Begin(BeginMode.Quads);
            GL.Color4(0.0f, 0.1f, 0.6f, 0.5f); GL.Vertex2(showAt.X, showAt.Y);
            GL.Color4(0.0f, 0.1f, 0.6f, 0.5f); GL.Vertex2(showAt.X, showAt.Y + textSize.Height);
            GL.Color4(0.0f, 0.1f, 0.6f, 0.5f); GL.Vertex2(showAt.X + textSize.Width, showAt.Y + textSize.Height);
            GL.Color4(0.0f, 0.1f, 0.6f, 0.5f); GL.Vertex2(showAt.X + textSize.Width, showAt.Y);
            GL.End();

            _font.Print(msg, new Vector2(showAt.X, showAt.Y));
            QuickFont.QFont.End();
        }

        private void WaterfallPlot_Load(object sender, EventArgs e) {
            CreateGLControl();
        }

        private void WaterfallPlot_Paint(object sender, PaintEventArgs e) {
            if (!_loaded) return;

            if (_waterfall == null) return;
            if (IsDisposed) return;

            _glctrl.MakeCurrent();
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.Enable(EnableCap.Texture2D);
            GL.UseProgram(_shaderTexture.Program);
            _waterfall.DrawTo(new RectangleF(0, 0, _glctrl.Width, _glctrl.Height));
            GL.Disable(EnableCap.Texture2D);
            GL.UseProgram(0);

            if (_showBallon) {
                RenderBallonInfo();
            }

            _glctrl.SwapBuffers();
        }

        private void WaterfallPlot_Resize(object sender, EventArgs e) {
            if (!_loaded) return;
            if (IsDisposed) return;

            _glctrl.Width = this.ClientSize.Width;
            _glctrl.Height = this.ClientSize.Height;

            _glctrl.MakeCurrent();
            SetupViewport();

            Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, _glctrl.Width, _glctrl.Height, 0, -1, 1);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);

            QuickFont.QFont.InvalidateViewport();

            Invalidate();
        }
    }
}
