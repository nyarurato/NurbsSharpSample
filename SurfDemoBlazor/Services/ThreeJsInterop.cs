using Microsoft.JSInterop;
using System.Text.Json.Serialization;

namespace SurfDemoBlazor.Services;

public class ThreeJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    private bool _disposed;

    public ThreeJsInterop(IJSRuntime jsRuntime)
    {
        _moduleTask = new Lazy<Task<IJSObjectReference>>(() => 
            jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./js/threejs-viewer.js").AsTask());
    }

    public async Task<bool> InitThreeJS(string containerId)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<bool>("initThreeJS", containerId);
    }

    public async Task DisplayPoints(Point3D[] points)
    {
        Console.WriteLine($"DisplayPoints called with {points.Length} points");
        for (int i = 0; i < Math.Min(3, points.Length); i++)
        {
            Console.WriteLine($"Point {i}: X={points[i].X}, Y={points[i].Y}, Z={points[i].Z}");
        }
        
        // Convert to anonymous objects to ensure correct JSON serialization
        var jsPoints = points.Select(p => new { x = p.X, y = p.Y, z = p.Z }).ToArray();
        
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("displayPoints", (object)jsPoints);
    }

    public async Task DisplayMesh(MeshData meshData)
    {
        Console.WriteLine($"DisplayMesh called with {meshData.Vertices.Length} vertices and {meshData.Faces.Length} faces");
        
        // Convert to anonymous objects to ensure correct JSON serialization
        var jsMeshData = new
        {
            vertices = meshData.Vertices.Select(v => new { x = v.X, y = v.Y, z = v.Z }).ToArray(),
            faces = meshData.Faces.Select(f => new { a = f.A, b = f.B, c = f.C }).ToArray()
        };
        
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("displayMesh", jsMeshData);
    }

    public async Task ClearScene()
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("clearScene");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("dispose");
            await module.DisposeAsync();
        }
    }
}

public class Point3D
{
    [JsonPropertyName("x")]
    public double X { get; set; }
    
    [JsonPropertyName("y")]
    public double Y { get; set; }
    
    [JsonPropertyName("z")]
    public double Z { get; set; }

    public Point3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

public class MeshData
{
    public Point3D[] Vertices { get; set; }
    public Face[] Faces { get; set; }

    public MeshData(Point3D[] vertices, Face[] faces)
    {
        Vertices = vertices;
        Faces = faces;
    }
}

public class Face
{
    [JsonPropertyName("a")]
    public int A { get; set; }
    
    [JsonPropertyName("b")]
    public int B { get; set; }
    
    [JsonPropertyName("c")]
    public int C { get; set; }

    public Face(int a, int b, int c)
    {
        A = a;
        B = b;
        C = c;
    }
}
