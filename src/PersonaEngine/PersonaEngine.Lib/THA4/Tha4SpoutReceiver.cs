using System;
using System.Numerics;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Options;

using PersonaEngine.Lib.Configuration;
using PersonaEngine.Lib.UI;
using PersonaEngine.Lib.UI.Common;
using PersonaEngine.Lib.UI.RouletteWheel;

using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

using Spout.Interop;

namespace PersonaEngine.Lib.THA4;

public class Tha4SpoutReceiver : IRenderComponent
{
    private readonly IOptionsMonitor<Tha4Options> _options;

    private SpoutReceiver? _receiver;
    private GL _gl;
    private Shader? _shader;
    private Texture? _texture;
    private BufferObject<VertexPositionTexture>? _vertexBuffer;
    private VertexArrayObject? _vao;

    public Tha4SpoutReceiver(IOptionsMonitor<Tha4Options> options) { _options = options; }

    public bool UseSpout => true;

    public string SpoutTarget => "Live2D";

    public int Priority => 100;

    public void Initialize(GL gl, IView view, IInputContext input)
    {
        _gl = gl;

        var cfg = _options.CurrentValue;

        _receiver = new SpoutReceiver();
        _receiver.SetReceiverName(cfg.SenderName);

        _texture = new Texture(gl, cfg.Width, cfg.Height, IntPtr.Zero);
        _texture.SetMinFilter(TextureMinFilter.Linear);
        _texture.SetMagFilter(TextureMagFilter.Linear);

        _vertexBuffer = new BufferObject<VertexPositionTexture>(gl, 4, BufferTargetARB.ArrayBuffer, false);
        _vao = new VertexArrayObject(gl, Marshal.SizeOf<VertexPositionTexture>());
        _vao.Bind();
        _vertexBuffer.Bind();

        var vert = @"#version 330
layout(location = 0) in vec3 a_position;
layout(location = 1) in vec2 a_texCoords;
out vec2 v_tex;
void main()
{
    v_tex = a_texCoords;
    gl_Position = vec4(a_position, 1.0);
}";

        var frag = @"#version 330
in vec2 v_tex;
uniform sampler2D TextureSampler;
layout(location = 0) out vec4 outColor;
void main()
{
    outColor = texture(TextureSampler, v_tex);
}";

        _shader = new Shader(gl, vert, frag);

        var loc = _shader.GetAttribLocation("a_position");
        _vao.VertexAttribPointer(loc, 3, VertexAttribPointerType.Float, false, 0);
        loc = _shader.GetAttribLocation("a_texCoords");
        _vao.VertexAttribPointer(loc, 2, VertexAttribPointerType.Float, false, 12);

        var vertices = new[]
        {
            new VertexPositionTexture(new Vector3(-1f, -1f, 0f), new Vector2(0f, 1f)),
            new VertexPositionTexture(new Vector3(1f, -1f, 0f), new Vector2(1f, 1f)),
            new VertexPositionTexture(new Vector3(-1f, 1f, 0f), new Vector2(0f, 0f)),
            new VertexPositionTexture(new Vector3(1f, 1f, 0f), new Vector2(1f, 0f))
        };
        _vertexBuffer.SetData(vertices, 0, vertices.Length);
    }

    public void Update(float deltaTime) { }

    public void Render(float deltaTime)
    {
        if ( _receiver == null || _texture == null || _shader == null || _vao == null )
        {
            return;
        }

        _receiver.ReceiveTexture(_texture.GlTexture, (uint)GLEnum.Texture2D, true, 0);

        _shader.Use();
        _shader.SetUniform("TextureSampler", 0);
        _texture.Bind();
        _vao.Bind();
        _gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
    }

    public void Resize() { }

    public void Dispose()
    {
        _receiver?.Dispose();
        _texture?.Dispose();
        _vertexBuffer?.Dispose();
        _vao?.Dispose();
        _shader?.Dispose();
    }
}
