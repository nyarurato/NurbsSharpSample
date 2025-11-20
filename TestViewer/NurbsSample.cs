using NurbsSharp;
using NurbsSharp.Core;
using NurbsSharp.Geometry;
using NurbsSharp.Tesselation;

namespace NurbsSharp.Samples.Viewer;

/// <summary>
/// NURBS曲面のサンプルデータを提供するクラス
/// </summary>
public static class NurbsSample
{
    /// <summary>
    /// degree 3のNURBS曲面を作成（NurbsSurfaceTestCから）
    /// </summary>
    public static NurbsSurface CreateDegree3Surface()
    {
        // NURBS surface (degree 3 in both directions)
        int degreeU = 3;
            int degreeV = 3;
            double[] knotsU = { 0, 0, 0, 0, 0.5,1, 1, 1, 1 };
            double[] knotsV = { 0, 0, 0, 0, 0.5,1, 1, 1, 1 };
            KnotVector knotVectorU = new KnotVector(knotsU, degreeU);
            KnotVector knotVectorV = new KnotVector(knotsV, degreeV);
            ControlPoint[][] controlPoints = new ControlPoint[5][];
            controlPoints[0] = new ControlPoint[] {
            new ControlPoint(0.0, 0.0, 0.0, 1),
            new ControlPoint(1.0, 0.0, 0.0, 1),
            new ControlPoint(2.0, 0.0, 0.0, 1),
            new ControlPoint(3.0, 0.0, 0.0, 1),
            new ControlPoint(4.0, 0.0, 0.0, 1)
            };
            controlPoints[1] = new ControlPoint[] {
            new ControlPoint(0.0, 1.0, 0.5, 1),
            new ControlPoint(1.0, 1.0, -1.5, 1),
            new ControlPoint(2.0, 1.0, 4.0, 1),
            new ControlPoint(3.0, 1.0, -3.0, 1),
            new ControlPoint(4.0, 1.0, 0.5, 1)
            };
            controlPoints[2] = new ControlPoint[] {
            new ControlPoint(0.0, 2.0, 1.5, 1),
            new ControlPoint(1.0, 2.0, 2.5, 1),
            new ControlPoint(2.0, 2.0, 3.5, 0.7),
            new ControlPoint(3.0, 2.0, 3.0, 1),
            new ControlPoint(4.0, 2.0, 0.0, 1)
            };
            controlPoints[3] = new ControlPoint[] {
            new ControlPoint(0.0, 3.0, 0.5, 1),
            new ControlPoint(1.5, 3.0, -1.5, 1),
            new ControlPoint(2.5, 3.0, 2.0 ,1),
            new ControlPoint(3.5, 3.0, -1.5, 1),
            new ControlPoint(4.5, 3.0, -1.0, 1)
            };
            controlPoints[4] = new ControlPoint[] {
            new ControlPoint(0.0, 4.0, 0.5, 1),
            new ControlPoint(1.0, 4.0, 0.5, 1),
            new ControlPoint(2.0, 4.0, 0.0, 1),
            new ControlPoint(3.0, 4.0, 0.0, 1),
            new ControlPoint(4.0, 4.0, 0.0, 1) 
            };

        return new NurbsSurface(degreeU, degreeV, knotVectorU, knotVectorV, controlPoints);
    }

    /// <summary>
    /// テッセレーションされたメッシュを取得
    /// </summary>
    /// <param name="numPointsU">U方向の分割数</param>
    /// <param name="numPointsV">V方向の分割数</param>
    public static Mesh GetTessellatedMesh(int numPointsU = 30, int numPointsV = 30)
    {
        var surface = CreateDegree3Surface();
        return SurfaceTessellator.Tessellate(surface, numPointsU, numPointsV);
    }
}
