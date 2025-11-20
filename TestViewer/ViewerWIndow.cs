using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using NurbsSharp.Geometry;
using NurbsSharp.Core;
namespace NurbsSharp.Samples.Viewer;

/// <summary>
/// OpenTK window to display a NURBS surface with gradient coloring and wireframe overlay
/// </summary>
public class ViewerWindow : GameWindow
{
    // ===== Constants =====
    private const int MESH_RESOLUTION_U = 30;
    private const int MESH_RESOLUTION_V = 30;
    private const float ROTATION_SPEED = 0.5f; // radians per second
    private const float WIREFRAME_LINE_WIDTH = 1.5f;
    
    // ===== OpenGL Resources =====
    private int _vertexArrayObject;
    private int _vertexBufferObject;
    private int _elementBufferObject;
    private int _gradientShaderProgram;
    private int _wireframeShaderProgram;
    private int _indexCount;
    
    // ===== Transformation Matrices =====
    private Matrix4 _modelMatrix;
    private Matrix4 _viewMatrix;
    private Matrix4 _projectionMatrix;
    private float _rotationAngle = 0.0f;

    public ViewerWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
    }

    // ===== Initialization =====
    
    protected override void OnLoad()
    {
        base.OnLoad();
        
        InitializeOpenGLSettings();
        InitializeShaders();
        InitializeMeshData();
        InitializeCamera();
    }

    private void InitializeOpenGLSettings()
    {
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f); // Black background
        GL.Enable(EnableCap.DepthTest);
    }

    private void InitializeShaders()
    {
        _gradientShaderProgram = CreateGradientShaderProgram();
        _wireframeShaderProgram = CreateWireframeShaderProgram();
    }

    private void InitializeMeshData()
    {
        // Get tessellated mesh from NURBS surface
        // THIS IS A SAMPLE DATA: See NurbsSample.cs for details
        var mesh = NurbsSample.GetTessellatedMesh(MESH_RESOLUTION_U, MESH_RESOLUTION_V);
        _indexCount = mesh.Indexes.Length;

        // Convert Vector3Double to float array for OpenGL
        float[] vertices = ConvertVerticesToFloatArray(mesh.Vertices);
        
        // Create and bind VAO
        _vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArrayObject);
        
        // Upload vertex data to GPU
        _vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
        
        // Upload index data to GPU
        _elementBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, mesh.Indexes.Length * sizeof(int), mesh.Indexes, BufferUsageHint.StaticDraw);
        
        // Configure vertex attributes (position only)
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
    }

    private float[] ConvertVerticesToFloatArray(Vector3Double[] vertices)
    {
        float[] result = new float[vertices.Length * 3];
        for (int i = 0; i < vertices.Length; i++)
        {
            result[i * 3 + 0] = (float)vertices[i].X;
            result[i * 3 + 1] = (float)vertices[i].Y;
            result[i * 3 + 2] = (float)vertices[i].Z;
        }
        return result;
    }

    private void InitializeCamera()
    {
        _modelMatrix = Matrix4.Identity;
        
        // Position camera to view the surface
        _viewMatrix = Matrix4.LookAt(
            new Vector3(6.0f, 6.0f, 10.0f),  // Camera position
            new Vector3(2.0f, 2.0f, 0.0f),    // Look at point (surface center)
            Vector3.UnitZ                      // Up direction
        );
        
        // Setup perspective projection
        _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45.0f),
            (float)Size.X / Size.Y,
            0.1f,   // Near plane
            100.0f  // Far plane
        );
    }

    // ===== Rendering =====
    
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        UpdateModelMatrix();
        Matrix4 mvp = _modelMatrix * _viewMatrix * _projectionMatrix;
        
        RenderSolidSurface(mvp);
        RenderWireframeOverlay(mvp);
        
        SwapBuffers();
    }

    private void UpdateModelMatrix()
    {
        // Rotate around the surface center (approximately at 2, 2, 0)
        Vector3 surfaceCenter = new Vector3(2.0f, 2.0f, 0.0f);
        _modelMatrix = Matrix4.CreateTranslation(-surfaceCenter) *
                       Matrix4.CreateRotationZ(_rotationAngle) *
                       Matrix4.CreateTranslation(surfaceCenter);
    }

    private void RenderSolidSurface(Matrix4 mvp)
    {
        GL.UseProgram(_gradientShaderProgram);
        
        int mvpLocation = GL.GetUniformLocation(_gradientShaderProgram, "uMVP");
        GL.UniformMatrix4(mvpLocation, false, ref mvp);
        
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
        GL.BindVertexArray(_vertexArrayObject);
        GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
    }

    private void RenderWireframeOverlay(Matrix4 mvp)
    {
        GL.UseProgram(_wireframeShaderProgram);
        
        int mvpLocation = GL.GetUniformLocation(_wireframeShaderProgram, "uMVP");
        GL.UniformMatrix4(mvpLocation, false, ref mvp);
        
        // Enable polygon offset to prevent z-fighting
        GL.Enable(EnableCap.PolygonOffsetLine);
        GL.PolygonOffset(-1.0f, -1.0f);
        
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        GL.LineWidth(WIREFRAME_LINE_WIDTH);
        
        GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
        
        GL.Disable(EnableCap.PolygonOffsetLine);
    }

    // ===== Update Loop =====
    
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        
        // Update rotation angle
        _rotationAngle += (float)args.Time * ROTATION_SPEED;
        
        // Exit on ESC key
        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }
    }

    // ===== Window Events =====
    
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        
        GL.Viewport(0, 0, Size.X, Size.Y);
        
        // Update projection matrix for new aspect ratio
        _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45.0f),
            (float)Size.X / Size.Y,
            0.1f,
            100.0f
        );
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        
        // Clean up OpenGL resources
        GL.DeleteBuffer(_vertexBufferObject);
        GL.DeleteBuffer(_elementBufferObject);
        GL.DeleteVertexArray(_vertexArrayObject);
        GL.DeleteProgram(_gradientShaderProgram);
        GL.DeleteProgram(_wireframeShaderProgram);
    }

    // ===== Shader Creation =====
    
    private int CreateGradientShaderProgram()
    {
        // Vertex shader: pass Z height to fragment shader for gradient calculation
        string vertexShaderSource = 
            "#version 330 core\n" +
            "layout (location = 0) in vec3 aPosition;\n" +
            "out float vHeight;\n" +
            "uniform mat4 uMVP;\n" +
            "void main()\n" +
            "{\n" +
            "    vHeight = aPosition.z;\n" +
            "    gl_Position = uMVP * vec4(aPosition, 1.0);\n" +
            "}\n";
        
        // Fragment shader: create blue-to-red gradient based on Z height (-2 to 2 range)
        string fragmentShaderSource = 
            "#version 330 core\n" +
            "in float vHeight;\n" +
            "out vec4 FragColor;\n" +
            "void main()\n" +
            "{\n" +
            "    float t = clamp((vHeight + 2.0) / 4.0, 0.0, 1.0);\n" +
            "    vec3 color1 = vec3(0.0, 0.0, 1.0);  // Blue (low)\n" +
            "    vec3 color2 = vec3(1.0, 0.0, 0.0);  // Red (high)\n" +
            "    vec3 finalColor = mix(color1, color2, t);\n" +
            "    FragColor = vec4(finalColor, 1.0);\n" +
            "}\n";
        
        return CompileShaderProgram(vertexShaderSource, fragmentShaderSource);
    }

    private int CreateWireframeShaderProgram()
    {
        // Simple vertex shader
        string vertexShaderSource = 
            "#version 330 core\n" +
            "layout (location = 0) in vec3 aPosition;\n" +
            "uniform mat4 uMVP;\n" +
            "void main()\n" +
            "{\n" +
            "    gl_Position = uMVP * vec4(aPosition, 1.0);\n" +
            "}\n";
        
        // Fragment shader: solid gray color for wireframe
        string fragmentShaderSource = 
            "#version 330 core\n" +
            "out vec4 FragColor;\n" +
            "void main()\n" +
            "{\n" +
            "    FragColor = vec4(0.7, 0.7, 0.7, 1.0); // Gray\n" +
            "}\n";
        
        return CompileShaderProgram(vertexShaderSource, fragmentShaderSource);
    }

    private int CompileShaderProgram(string vertexSource, string fragmentSource)
    {
        // Compile vertex shader
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexSource);
        GL.CompileShader(vertexShader);
        CheckShaderCompilation(vertexShader, "Vertex");
        
        // Compile fragment shader
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentSource);
        GL.CompileShader(fragmentShader);
        CheckShaderCompilation(fragmentShader, "Fragment");
        
        // Link shader program
        int program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);
        CheckProgramLinking(program);
        
        // Clean up individual shaders (no longer needed after linking)
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        
        return program;
    }

    // ===== Error Checking =====
    
    private void CheckShaderCompilation(int shader, string type)
    {
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetShaderInfoLog(shader);
            throw new Exception($"{type} Shader compilation failed: {infoLog}");
        }
    }

    private void CheckProgramLinking(int program)
    {
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetProgramInfoLog(program);
            throw new Exception($"Shader program linking failed: {infoLog}");
        }
    }
}