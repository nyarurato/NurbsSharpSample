using NurbsSharp.Core;
using NurbsSharp.Generation;
using NurbsSharp.Generation.Interpolation;
using NurbsSharp.Geometry;
using NurbsSharp.Intersection;
using NurbsSharp.Tesselation;

namespace NurbsSharp.Samples.Viewer;

/// <summary>
/// NURBS Sharp usage examples used by the OpenTK viewer.
/// Start here when you want to learn how to construct and query NURBS geometry.
/// </summary>
public static class NurbsExamples
{
    /// <summary>
    /// Creates a degree-3 NURBS surface, a curve that crosses it, and their intersections.
    /// </summary>
    public static CurveSurfaceExample CreateCurveSurfaceExample()
    {
        var surface = CreateDegree3Surface();
        var curve = CreateCrossingCurve();

        var mesh = ToMeshData(SurfaceTessellator.Tessellate(surface, 30, 30));
        var curvePoints = CurveTessellator.Tessellate(curve, 100).ToArray();
        var intersections = CurveSurfaceIntersector.Intersect(curve, surface)
            .Select(result => result.CurvePoint)
            .ToArray();

        return new CurveSurfaceExample(mesh, curvePoints, intersections);
    }

    /// <summary>
    /// Creates two NURBS curves and finds their intersection points.
    /// The second curve is created by global interpolation.
    /// </summary>
    public static CurveCurveExample CreateCurveCurveExample()
    {
        var curve1 = CreateCrossingCurve();
        var interpolationPoints = CreateInterpolationPoints(curve1);
        var curve2 = GlobalInterpolator.InterpolateCurve(interpolationPoints, degree: 2);

        var curve1Points = CurveTessellator.Tessellate(curve1, 150).ToArray();
        var curve2Points = CurveTessellator.Tessellate(curve2, 150).ToArray();
        var intersections = CurveCurveIntersector.Intersect(curve1, curve2)
            .Select(result => result.Point1)
            .ToArray();
        var center = BoundingBox.FromPoints(curve1Points.Concat(curve2Points)).Center;

        return new CurveCurveExample(
            curve1Points,
            curve2Points,
            interpolationPoints,
            intersections,
            center);
    }

    /// <summary>
    /// Intersects a NURBS surface with the YZ plane at X=2.
    /// Both the fast and robust algorithms are returned for visual comparison.
    /// </summary>
    public static SurfacePlaneExample CreateSurfacePlaneExample()
    {
        var surface = CreateDegree3Surface();
        var plane = Plane.YZ.Translate(new Vector3Double(2.0, 0.0, 0.0));
        var planeSize = Math.Max(surface.BoundingBox.Size.Y, surface.BoundingBox.Size.Z) * 1.5;

        var fastCurves = SurfacePlaneIntersector.IntersectFast(
                surface,
                plane,
                tolerance: 1e-6,
                numIsoCurves: 50)
            .Select(curve => CurveTessellator.Tessellate(curve, 60).ToArray())
            .ToArray();

        var robustCurves = SurfacePlaneIntersector.IntersectRobust(
                surface,
                plane,
                tolerance: 1e-6,
                maxIterations: 20,
                stepSize: 0.02)
            .Select(curve => CurveTessellator.Tessellate(curve, 100).ToArray())
            .ToArray();

        return new SurfacePlaneExample(
            ToMeshData(SurfaceTessellator.Tessellate(surface, 30, 30)),
            CreatePlaneMesh(plane, planeSize),
            fastCurves,
            robustCurves);
    }

    /// <summary>
    /// Intersects the sample NURBS surface with a planar NURBS surface.
    /// </summary>
    public static SurfaceSurfaceExample CreateSurfaceSurfaceExample()
    {
        var surface1 = CreateDegree3Surface();
        var surface2 = PrimitiveFactory.CreateFace(
            new Vector3Double(0.6, 0.0, 1.0),
            new Vector3Double(3.6, 0.0, 1.0),
            new Vector3Double(0.6, 4.0, 1.0),
            new Vector3Double(3.6, 4.0, 1.0));

        var intersectionCurves = SurfaceSurfaceIntersector.IntersectCurves(
                surface1,
                surface2,
                tolerance: 1e-6,
                isoDivisions: 10,
                interpolate: true,
                degree: 2)
            .Select(curve => CurveTessellator.Tessellate(curve, 100).ToArray())
            .ToArray();

        return new SurfaceSurfaceExample(
            ToMeshData(SurfaceTessellator.Tessellate(surface1, 30, 30)),
            ToMeshData(SurfaceTessellator.Tessellate(surface2, 8, 8)),
            intersectionCurves);
    }

    /// <summary>
    /// Shoots deterministic rays from a sphere toward the sample surface and finds mesh hits.
    /// </summary>
    public static RayMeshExample CreateRayMeshExample(int rayCount = 10, double radius = 8.0)
    {
        var mesh = SurfaceTessellator.Tessellate(CreateDegree3Surface(), 30, 30);
        var center = mesh.BoundingBox.Center;
        var random = new Random(42);
        var rays = new Ray[rayCount];
        var raySegments = new Vector3Double[rayCount * 2];

        for (int i = 0; i < rayCount; i++)
        {
            double theta = random.NextDouble() * 2.0 * Math.PI;
            double phi = Math.Acos(2.0 * random.NextDouble() - 1.0);
            var radial = new Vector3Double(
                Math.Sin(phi) * Math.Cos(theta),
                Math.Sin(phi) * Math.Sin(theta),
                Math.Cos(phi));
            var origin = center + radial * radius;
            var direction = center - origin;

            rays[i] = new Ray(origin, direction, normalize: true);
            raySegments[i * 2] = origin;
            raySegments[i * 2 + 1] = rays[i].PointAt(radius * 1.5);
        }

        var bvh = BVHBuilder.Build(mesh);
        var hitPoints = new List<Vector3Double>();
        foreach (var ray in rays)
        {
            if (RayMeshIntersector.Intersects(ray, mesh, out var hit, bvh))
                hitPoints.Add(hit.Point);
        }

        return new RayMeshExample(ToMeshData(mesh), raySegments, hitPoints.ToArray());
    }

    /// <summary>
    /// Creates the degree-3 surface shared by the examples.
    /// </summary>
    public static NurbsSurface CreateDegree3Surface()
    {
        const int degreeU = 3;
        const int degreeV = 3;
        double[] knotsU = { 0, 0, 0, 0, 0.5, 1, 1, 1, 1 };
        double[] knotsV = { 0, 0, 0, 0, 0.5, 1, 1, 1, 1 };

        ControlPoint[][] controlPoints =
        {
            new ControlPoint[]
            {
                new(0.0, 0.0, 0.0, 1), new(1.0, 0.0, 0.0, 1),
                new(2.0, 0.0, 0.0, 1), new(3.0, 0.0, 0.0, 1),
                new(4.0, 0.0, 0.0, 1)
            },
            new ControlPoint[]
            {
                new(0.0, 1.0, 0.5, 1), new(1.0, 1.0, -1.5, 1),
                new(2.0, 1.0, 4.0, 1), new(3.0, 1.0, -3.0, 1),
                new(4.0, 1.0, 0.5, 1)
            },
            new ControlPoint[]
            {
                new(0.0, 2.0, 1.5, 1), new(1.0, 2.0, 2.5, 1),
                new(2.0, 2.0, 3.5, 0.7), new(3.0, 2.0, 3.0, 1),
                new(4.0, 2.0, 0.0, 1)
            },
            new ControlPoint[]
            {
                new(0.0, 3.0, 0.5, 1), new(1.5, 3.0, -1.5, 1),
                new(2.5, 3.0, 2.0, 1), new(3.5, 3.0, -1.5, 1),
                new(4.5, 3.0, -1.0, 1)
            },
            new ControlPoint[]
            {
                new(0.0, 4.0, 0.5, 1), new(1.0, 4.0, 0.5, 1),
                new(2.0, 4.0, 0.0, 1), new(3.0, 4.0, 0.0, 1),
                new(4.0, 4.0, 0.0, 1)
            }
        };

        return new NurbsSurface(
            degreeU,
            degreeV,
            new KnotVector(knotsU, degreeU),
            new KnotVector(knotsV, degreeV),
            controlPoints);
    }

    private static NurbsCurve CreateCrossingCurve()
    {
        const int degree = 3;
        double[] knots = { 0, 0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1 };
        ControlPoint[] controlPoints =
        {
            new(-1.0, 0.5, 2.0, 1.0),
            new(0.5, 1.0, 3.0, 1.0),
            new(1.5, 1.5, 1.5, 1.0),
            new(2.5, 2.5, -1.5, 1.0),
            new(3.0, 2.8, -1.0, 1.0),
            new(3.5, 3.2, -0.5, 1.0),
            new(5.0, 3.5, 1.5, 1.0)
        };

        return new NurbsCurve(degree, new KnotVector(knots, degree), controlPoints);
    }

    private static Vector3Double[] CreateInterpolationPoints(NurbsCurve curveToIntersect)
    {
        var intersectionPoint = curveToIntersect.GetPos(0.5);
        return
        [
            intersectionPoint + new Vector3Double(0.0, -2.0, 2.0),
            intersectionPoint + new Vector3Double(0.0, -1.0, 1.0),
            intersectionPoint,
            intersectionPoint + new Vector3Double(0.0, 1.0, -1.0),
            intersectionPoint + new Vector3Double(0.0, 2.0, -2.0)
        ];
    }

    private static MeshData CreatePlaneMesh(Plane plane, double size)
    {
        var center = plane.Normal * -plane.Distance;
        var up = Math.Abs(plane.Normal.Y) < 0.95
            ? new Vector3Double(0, 1, 0)
            : new Vector3Double(1, 0, 0);
        var u = Vector3Double.Cross(up, plane.Normal).normalized;
        var v = Vector3Double.Cross(plane.Normal, u).normalized;
        double half = size * 0.5;

        Vector3Double[] vertices =
        {
            center - u * half - v * half,
            center + u * half - v * half,
            center + u * half + v * half,
            center - u * half + v * half
        };

        return new MeshData(vertices, new[] { 0, 1, 2, 2, 3, 0 }, center);
    }

    private static MeshData ToMeshData(Mesh mesh)
    {
        return new MeshData(mesh.Vertices, mesh.Indexes, mesh.BoundingBox.Center);
    }
}

public sealed record MeshData(Vector3Double[] Vertices, int[] Indices, Vector3Double Center);

public sealed record CurveSurfaceExample(
    MeshData Surface,
    Vector3Double[] Curve,
    Vector3Double[] Intersections);

public sealed record CurveCurveExample(
    Vector3Double[] Curve1,
    Vector3Double[] Curve2,
    Vector3Double[] InterpolationPoints,
    Vector3Double[] Intersections,
    Vector3Double Center);

public sealed record SurfacePlaneExample(
    MeshData Surface,
    MeshData Plane,
    Vector3Double[][] FastIntersections,
    Vector3Double[][] RobustIntersections);

public sealed record SurfaceSurfaceExample(
    MeshData Surface1,
    MeshData Surface2,
    Vector3Double[][] Intersections);

public sealed record RayMeshExample(
    MeshData Surface,
    Vector3Double[] RaySegments,
    Vector3Double[] Intersections);
