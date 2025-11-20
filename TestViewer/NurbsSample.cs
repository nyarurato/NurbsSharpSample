using NurbsSharp;
using NurbsSharp.Core;
using NurbsSharp.Geometry;
using NurbsSharp.Tesselation;

namespace NurbsSharp.Samples.Viewer;

/// <summary>
/// A sample NURBS surface generator.
/// </summary>
public static class NurbsSample
{
    /// <summary>
    /// Creates a NURBS surface of degree 3 in both U and V directions.
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
            ControlPoint[][] controlPoints = new ControlPoint[5][]; // 5x5 control points U x V
            controlPoints[0] = new ControlPoint[] {
            // x, y, z, weight
            new ControlPoint(0.0, 0.0, 0.0, 1),  //U0 V0
            new ControlPoint(1.0, 0.0, 0.0, 1),  //U0 V1
            new ControlPoint(2.0, 0.0, 0.0, 1),  //U0 V2
            new ControlPoint(3.0, 0.0, 0.0, 1),  //U0 V3
            new ControlPoint(4.0, 0.0, 0.0, 1)   //U0 V4
            };
            controlPoints[1] = new ControlPoint[] {
            new ControlPoint(0.0, 1.0, 0.5, 1),  //U1 V0
            new ControlPoint(1.0, 1.0, -1.5, 1), //U1 V1
            new ControlPoint(2.0, 1.0, 4.0, 1),  //U1 V2
            new ControlPoint(3.0, 1.0, -3.0, 1), //U1 V3
            new ControlPoint(4.0, 1.0, 0.5, 1)   //U1 V4
            };
            controlPoints[2] = new ControlPoint[] {
            new ControlPoint(0.0, 2.0, 1.5, 1),  //U2 V0
            new ControlPoint(1.0, 2.0, 2.5, 1),  //U2 V1
            new ControlPoint(2.0, 2.0, 3.5, 0.7),//U2 V2
            new ControlPoint(3.0, 2.0, 3.0, 1),  //U2 V3
            new ControlPoint(4.0, 2.0, 0.0, 1)   //U2 V4
            };
            controlPoints[3] = new ControlPoint[] {
            new ControlPoint(0.0, 3.0, 0.5, 1),  //U3 V0
            new ControlPoint(1.5, 3.0, -1.5, 1), //U3 V1
            new ControlPoint(2.5, 3.0, 2.0 ,1),  //U3 V2
            new ControlPoint(3.5, 3.0, -1.5, 1), //U3 V3
            new ControlPoint(4.5, 3.0, -1.0, 1)  //U3 V4
            };
            controlPoints[4] = new ControlPoint[] {
            new ControlPoint(0.0, 4.0, 0.5, 1),  //U4 V0
            new ControlPoint(1.0, 4.0, 0.5, 1),  //U4 V1
            new ControlPoint(2.0, 4.0, 0.0, 1),  //U4 V2
            new ControlPoint(3.0, 4.0, 0.0, 1),  //U4 V3
            new ControlPoint(4.0, 4.0, 0.0, 1)   //U4 V4
            };

        return new NurbsSurface(degreeU, degreeV, knotVectorU, knotVectorV, controlPoints);
    }

    /// <summary>
    /// Tessellates the sample NURBS surface into a mesh.
    /// </summary>
    /// <param name="numPointsU">Number of divisions in the U direction</param>
    /// <param name="numPointsV"></param>
    public static Mesh GetTessellatedMesh(int numPointsU = 30, int numPointsV = 30)
    {
        var surface = CreateDegree3Surface();// Create the sample NURBS surface
        return SurfaceTessellator.Tessellate(surface, numPointsU, numPointsV); // Generate a mesh with specified resolution
    }
}
