using System.Collections.Concurrent;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace NurbsSharp.Samples.Viewer;

/// <summary>
/// Displays the results produced by <see cref="NurbsExamples"/>.
/// NURBS construction and query code intentionally lives in that one class.
/// </summary>
public sealed class ViewerWindow : GameWindow
{
    private enum SurfaceDrawMode
    {
        All,
        SolidOnly,
        WireframeOnly
    }

    private enum ExampleKind
    {
        CurveSurface,
        CurveCurve,
        SurfacePlane,
        SurfaceSurface,
        RayMesh
    }

    private readonly CameraController _camera = new();
    private readonly ConcurrentQueue<Action> _mainThreadActions = new();

    private ShaderProgram? _gradientShader;
    private ShaderProgram? _wireframeShader;
    private ShaderProgram? _curveShader;
    private ShaderProgram? _intersectionCurveShader;
    private ShaderProgram? _fastIntersectionCurveShader;
    private ShaderProgram? _intersectionPointShader;
    private ShaderProgram? _inputPointShader;
    private ShaderProgram? _planeShader;
    private ShaderProgram? _rayShader;

    private MeshRenderer? _meshRenderer;
    private MeshRenderer? _secondMeshRenderer;
    private MeshRenderer? _planeRenderer;
    private CurveRenderer? _curveRenderer;
    private CurveRenderer? _secondCurveRenderer;
    private PointRenderer? _inputPointRenderer;
    private PointRenderer? _intersectionPointRenderer;
    private RayRenderer? _rayRenderer;
    private readonly List<CurveRenderer> _intersectionCurveRenderers = new();
    private readonly List<CurveRenderer> _fastIntersectionCurveRenderers = new();

    private SurfacePlaneExample? _surfacePlaneCache;
    private SurfaceSurfaceExample? _surfaceSurfaceCache;
    private bool _surfacePlaneComputing;
    private bool _surfaceSurfaceComputing;
    private bool _isUnloading;
    private ExampleKind _requestedExample = ExampleKind.CurveSurface;

    private SurfaceDrawMode _drawMode = SurfaceDrawMode.All;
    private bool _showCurves = true;
    private bool _showIntersections = true;
    private bool _showInputPoints = true;
    private bool _showRays = true;
    private float _wireframeLineWidth = 1.5f;

    public ViewerWindow(
        GameWindowSettings gameWindowSettings,
        NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        GL.Enable(EnableCap.DepthTest);

        InitializeShaders();
        _camera.Initialize(Size.X, Size.Y);
        PrintKeyHelp();
        LoadCurveSurfaceExample();
    }

    private void InitializeShaders()
    {
        const string gradientVertexShader =
            "#version 330 core\n" +
            "layout(location=0) in vec3 aPosition;\n" +
            "out float vHeight;\n" +
            "uniform mat4 uMVP;\n" +
            "void main(){ vHeight=aPosition.z; gl_Position=uMVP*vec4(aPosition,1.0); }\n";
        const string gradientFragmentShader =
            "#version 330 core\n" +
            "in float vHeight;\n" +
            "out vec4 FragColor;\n" +
            "void main(){ float t=clamp((vHeight+2.0)/4.0,0.0,1.0);" +
            "FragColor=vec4(mix(vec3(0.0,0.0,1.0),vec3(1.0,0.0,0.0),t),1.0); }\n";

        _gradientShader = new ShaderProgram(gradientVertexShader, gradientFragmentShader);
        _wireframeShader = ShaderProgram.CreateSimpleColorProgram("0.7, 0.7, 0.7");
        _curveShader = ShaderProgram.CreateSimpleColorProgram("0.0, 1.0, 0.0");
        _intersectionCurveShader = ShaderProgram.CreateSimpleColorProgram("1.0, 0.0, 1.0");
        _fastIntersectionCurveShader = ShaderProgram.CreateSimpleColorProgram("0.0, 1.0, 1.0");
        _intersectionPointShader = ShaderProgram.CreateSimpleColorProgram("1.0, 0.1, 0.1");
        _inputPointShader = ShaderProgram.CreateSimpleColorProgram("1.0, 0.8, 0.0");
        _planeShader = ShaderProgram.CreateSimpleColorProgramWithAlpha("0.3, 0.6, 0.9, 0.4");
        _rayShader = ShaderProgram.CreateSimpleColorProgram("1.0, 1.0, 0.0");
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        while (_mainThreadActions.TryDequeue(out var action))
            action();

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        Matrix4 mvp = _camera.GetMVP();

        RenderPlane(mvp);
        RenderMeshes(mvp);
        RenderCurves(mvp);
        RenderPoints(mvp);
        RenderRays(mvp);

        SwapBuffers();
    }

    private void RenderPlane(Matrix4 mvp)
    {
        if (_planeRenderer is null || _planeShader is null)
            return;

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.DepthMask(false);
        _planeRenderer.DrawSolid(_planeShader, mvp);
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
    }

    private void RenderMeshes(Matrix4 mvp)
    {
        if (_drawMode != SurfaceDrawMode.WireframeOnly && _gradientShader is not null)
        {
            _meshRenderer?.DrawSolid(_gradientShader, mvp);
            _secondMeshRenderer?.DrawSolid(_gradientShader, mvp);
        }

        if (_drawMode != SurfaceDrawMode.SolidOnly && _wireframeShader is not null)
        {
            _meshRenderer?.DrawWireframe(_wireframeShader, mvp, _wireframeLineWidth);
            _secondMeshRenderer?.DrawWireframe(_wireframeShader, mvp, _wireframeLineWidth);
        }
    }

    private void RenderCurves(Matrix4 mvp)
    {
        if (_showCurves && _curveShader is not null)
        {
            _curveRenderer?.Draw(_curveShader, mvp, 3.0f);
            _secondCurveRenderer?.Draw(_curveShader, mvp, 3.0f);
        }

        if (!_showIntersections)
            return;

        if (_fastIntersectionCurveShader is not null)
        {
            foreach (var curve in _fastIntersectionCurveRenderers)
                curve.Draw(_fastIntersectionCurveShader, mvp, 2.0f, 0.02f);
        }

        if (_intersectionCurveShader is not null)
        {
            foreach (var curve in _intersectionCurveRenderers)
                curve.Draw(_intersectionCurveShader, mvp, 3.5f, 0.05f);
        }
    }

    private void RenderPoints(Matrix4 mvp)
    {
        if (_showInputPoints && _inputPointShader is not null)
            _inputPointRenderer?.Draw(_inputPointShader, mvp, 8.0f);

        if (_showIntersections && _intersectionPointShader is not null)
            _intersectionPointRenderer?.Draw(_intersectionPointShader, mvp, 11.0f);
    }

    private void RenderRays(Matrix4 mvp)
    {
        if (_showRays && _rayShader is not null)
            _rayRenderer?.Draw(_rayShader, mvp, 2.5f);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        _camera.Update((float)args.Time);
        HandleKeys();

        if (KeyboardState.IsKeyDown(Keys.Escape))
            Close();
    }

    private void HandleKeys()
    {
        bool shift = KeyboardState.IsKeyDown(Keys.LeftShift) ||
                     KeyboardState.IsKeyDown(Keys.RightShift);

        if (shift)
        {
            if (KeyboardState.IsKeyPressed(Keys.D3)) StartSurfacePlaneExample();
            if (KeyboardState.IsKeyPressed(Keys.D4)) StartSurfaceSurfaceExample();
            if (KeyboardState.IsKeyPressed(Keys.D5)) LoadCurveCurveExample();
            if (KeyboardState.IsKeyPressed(Keys.D6)) ToggleInputPoints();
            if (KeyboardState.IsKeyPressed(Keys.D8)) LoadRayMeshExample();
            return;
        }

        if (KeyboardState.IsKeyPressed(Keys.D0)) ResetViewer();
        if (KeyboardState.IsKeyPressed(Keys.D1)) SetDrawMode(SurfaceDrawMode.All);
        if (KeyboardState.IsKeyPressed(Keys.D2)) SetDrawMode(SurfaceDrawMode.SolidOnly);
        if (KeyboardState.IsKeyPressed(Keys.D3)) SetDrawMode(SurfaceDrawMode.WireframeOnly);
        if (KeyboardState.IsKeyPressed(Keys.D4)) ToggleRotation();
        if (KeyboardState.IsKeyPressed(Keys.D5)) ToggleIntersections();
        if (KeyboardState.IsKeyPressed(Keys.D6)) ToggleCurves();
        if (KeyboardState.IsKeyPressed(Keys.D7)) ChangeWireframeWidth(0.5f);
        if (KeyboardState.IsKeyPressed(Keys.D8)) ChangeWireframeWidth(-0.5f);
        if (KeyboardState.IsKeyPressed(Keys.D9)) PrintStatus();
    }

    private void ResetViewer()
    {
        _drawMode = SurfaceDrawMode.All;
        _showCurves = true;
        _showIntersections = true;
        _showInputPoints = true;
        _showRays = true;
        _wireframeLineWidth = 1.5f;
        _camera.Initialize(Size.X, Size.Y);
        LoadCurveSurfaceExample();
    }

    private void LoadCurveSurfaceExample()
    {
        _requestedExample = ExampleKind.CurveSurface;
        Apply(NurbsExamples.CreateCurveSurfaceExample());
        Console.WriteLine("Example: Curve-Surface intersection");
    }

    private void LoadCurveCurveExample()
    {
        _requestedExample = ExampleKind.CurveCurve;
        Apply(NurbsExamples.CreateCurveCurveExample());
        Console.WriteLine("Example: Curve-Curve intersection");
    }

    private void LoadRayMeshExample()
    {
        _requestedExample = ExampleKind.RayMesh;
        Apply(NurbsExamples.CreateRayMeshExample());
        Console.WriteLine("Example: Ray-Mesh intersection");
    }

    private void StartSurfacePlaneExample()
    {
        _requestedExample = ExampleKind.SurfacePlane;
        if (_surfacePlaneCache is not null)
        {
            Apply(_surfacePlaneCache);
            return;
        }

        if (_surfacePlaneComputing)
        {
            Console.WriteLine("Surface-Plane intersection is already being computed.");
            return;
        }

        _surfacePlaneComputing = true;
        Console.WriteLine("Computing Surface-Plane intersections...");
        _ = ComputeSurfacePlaneExampleAsync();
    }

    private async Task ComputeSurfacePlaneExampleAsync()
    {
        try
        {
            var example = await Task.Run(NurbsExamples.CreateSurfacePlaneExample).ConfigureAwait(false);
            Post(() =>
            {
                _surfacePlaneComputing = false;
                _surfacePlaneCache = example;
                if (_requestedExample == ExampleKind.SurfacePlane)
                    Apply(example);
                Console.WriteLine(
                    $"Surface-Plane intersections: fast={example.FastIntersections.Length}, " +
                    $"robust={example.RobustIntersections.Length}");
            });
        }
        catch (Exception exception)
        {
            Post(() =>
            {
                _surfacePlaneComputing = false;
                Console.Error.WriteLine($"Surface-Plane intersection failed: {exception.Message}");
            });
        }
    }

    private void StartSurfaceSurfaceExample()
    {
        _requestedExample = ExampleKind.SurfaceSurface;
        if (_surfaceSurfaceCache is not null)
        {
            Apply(_surfaceSurfaceCache);
            return;
        }

        if (_surfaceSurfaceComputing)
        {
            Console.WriteLine("Surface-Surface intersection is already being computed.");
            return;
        }

        _surfaceSurfaceComputing = true;
        Console.WriteLine("Computing Surface-Surface intersections...");
        _ = ComputeSurfaceSurfaceExampleAsync();
    }

    private async Task ComputeSurfaceSurfaceExampleAsync()
    {
        try
        {
            var example = await Task.Run(NurbsExamples.CreateSurfaceSurfaceExample).ConfigureAwait(false);
            Post(() =>
            {
                _surfaceSurfaceComputing = false;
                _surfaceSurfaceCache = example;
                if (_requestedExample == ExampleKind.SurfaceSurface)
                    Apply(example);
                Console.WriteLine($"Surface-Surface intersections: {example.Intersections.Length}");
            });
        }
        catch (Exception exception)
        {
            Post(() =>
            {
                _surfaceSurfaceComputing = false;
                Console.Error.WriteLine($"Surface-Surface intersection failed: {exception.Message}");
            });
        }
    }

    private void Apply(CurveSurfaceExample example)
    {
        ClearScene();
        _meshRenderer = CreateMeshRenderer(example.Surface);
        _curveRenderer = CreateCurveRenderer(example.Curve);
        _intersectionPointRenderer = CreatePointRenderer(example.Intersections);
        SetSceneCenter(example.Surface.Center);
    }

    private void Apply(CurveCurveExample example)
    {
        ClearScene();
        _curveRenderer = CreateCurveRenderer(example.Curve1);
        _secondCurveRenderer = CreateCurveRenderer(example.Curve2);
        _inputPointRenderer = CreatePointRenderer(example.InterpolationPoints);
        _intersectionPointRenderer = CreatePointRenderer(example.Intersections);
        SetSceneCenter(example.Center);
    }

    private void Apply(SurfacePlaneExample example)
    {
        ClearScene();
        _meshRenderer = CreateMeshRenderer(example.Surface);
        _planeRenderer = CreateMeshRenderer(example.Plane);
        AddCurves(example.FastIntersections, _fastIntersectionCurveRenderers);
        AddCurves(example.RobustIntersections, _intersectionCurveRenderers);
        SetSceneCenter(example.Surface.Center);
    }

    private void Apply(SurfaceSurfaceExample example)
    {
        ClearScene();
        _meshRenderer = CreateMeshRenderer(example.Surface1);
        _secondMeshRenderer = CreateMeshRenderer(example.Surface2);
        AddCurves(example.Intersections, _intersectionCurveRenderers);
        SetSceneCenter(example.Surface1.Center);
    }

    private void Apply(RayMeshExample example)
    {
        ClearScene();
        _meshRenderer = CreateMeshRenderer(example.Surface);
        if (example.RaySegments.Length > 0)
            _rayRenderer = new RayRenderer(example.RaySegments);
        _intersectionPointRenderer = CreatePointRenderer(example.Intersections);
        SetSceneCenter(example.Surface.Center);
        Console.WriteLine($"Ray-Mesh hits: {example.Intersections.Length}");
    }

    private static MeshRenderer? CreateMeshRenderer(MeshData mesh)
    {
        return mesh.Vertices.Length == 0 || mesh.Indices.Length == 0
            ? null
            : new MeshRenderer(mesh.Vertices, mesh.Indices);
    }

    private static CurveRenderer? CreateCurveRenderer(NurbsSharp.Core.Vector3Double[] points)
    {
        return points.Length == 0 ? null : new CurveRenderer(points);
    }

    private static PointRenderer? CreatePointRenderer(NurbsSharp.Core.Vector3Double[] points)
    {
        return points.Length == 0 ? null : new PointRenderer(points);
    }

    private static void AddCurves(
        IEnumerable<NurbsSharp.Core.Vector3Double[]> curves,
        ICollection<CurveRenderer> renderers)
    {
        foreach (var points in curves)
        {
            if (points.Length > 0)
                renderers.Add(new CurveRenderer(points));
        }
    }

    private void SetSceneCenter(NurbsSharp.Core.Vector3Double center)
    {
        _camera.SetCenter(center);
        _showCurves = true;
        _showIntersections = true;
        _showInputPoints = true;
        _showRays = true;
    }

    private void SetDrawMode(SurfaceDrawMode mode)
    {
        _drawMode = mode;
        Console.WriteLine($"Draw mode: {_drawMode}");
    }

    private void ToggleRotation()
    {
        _camera.RotationEnabled = !_camera.RotationEnabled;
        Console.WriteLine($"Rotation: {_camera.RotationEnabled}");
    }

    private void ToggleIntersections()
    {
        _showIntersections = !_showIntersections;
        Console.WriteLine($"Intersections: {_showIntersections}");
    }

    private void ToggleCurves()
    {
        _showCurves = !_showCurves;
        Console.WriteLine($"Curves: {_showCurves}");
    }

    private void ToggleInputPoints()
    {
        _showInputPoints = !_showInputPoints;
        Console.WriteLine($"Interpolation points: {_showInputPoints}");
    }

    private void ChangeWireframeWidth(float amount)
    {
        _wireframeLineWidth = Math.Clamp(_wireframeLineWidth + amount, 0.5f, 10.0f);
        Console.WriteLine($"Wireframe width: {_wireframeLineWidth}");
    }

    private void PrintStatus()
    {
        Console.WriteLine(
            $"Example={_requestedExample}, DrawMode={_drawMode}, " +
            $"Rotation={_camera.RotationEnabled}, Curves={_showCurves}, " +
            $"Intersections={_showIntersections}, WireframeWidth={_wireframeLineWidth}");
    }

    private static void PrintKeyHelp()
    {
        Console.WriteLine("NURBS Sharp sample keys:");
        Console.WriteLine("  0       Curve-Surface example / reset");
        Console.WriteLine("  1/2/3   All / solid / wireframe");
        Console.WriteLine("  4       Toggle rotation");
        Console.WriteLine("  5       Toggle intersections");
        Console.WriteLine("  6       Toggle curves");
        Console.WriteLine("  7/8     Increase / decrease wireframe width");
        Console.WriteLine("  9       Print status");
        Console.WriteLine("  Shift+3 Surface-Plane example");
        Console.WriteLine("  Shift+4 Surface-Surface example");
        Console.WriteLine("  Shift+5 Curve-Curve example");
        Console.WriteLine("  Shift+6 Toggle interpolation points");
        Console.WriteLine("  Shift+8 Ray-Mesh example");
    }

    private void Post(Action action)
    {
        if (!_isUnloading)
            _mainThreadActions.Enqueue(action);
    }

    private void ClearScene()
    {
        _meshRenderer?.Dispose();
        _meshRenderer = null;
        _secondMeshRenderer?.Dispose();
        _secondMeshRenderer = null;
        _planeRenderer?.Dispose();
        _planeRenderer = null;
        _curveRenderer?.Dispose();
        _curveRenderer = null;
        _secondCurveRenderer?.Dispose();
        _secondCurveRenderer = null;
        _inputPointRenderer?.Dispose();
        _inputPointRenderer = null;
        _intersectionPointRenderer?.Dispose();
        _intersectionPointRenderer = null;
        _rayRenderer?.Dispose();
        _rayRenderer = null;

        DisposeCurves(_intersectionCurveRenderers);
        DisposeCurves(_fastIntersectionCurveRenderers);
    }

    private static void DisposeCurves(ICollection<CurveRenderer> renderers)
    {
        foreach (var renderer in renderers)
            renderer.Dispose();
        renderers.Clear();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, Size.X, Size.Y);
        _camera.Resize(Size.X, Size.Y);
    }

    protected override void OnUnload()
    {
        _isUnloading = true;
        while (_mainThreadActions.TryDequeue(out _))
        {
        }

        ClearScene();
        _gradientShader?.Dispose();
        _wireframeShader?.Dispose();
        _curveShader?.Dispose();
        _intersectionCurveShader?.Dispose();
        _fastIntersectionCurveShader?.Dispose();
        _intersectionPointShader?.Dispose();
        _inputPointShader?.Dispose();
        _planeShader?.Dispose();
        _rayShader?.Dispose();

        base.OnUnload();
    }
}
