using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using NurbsSharp.Core;
using System;
using System.Collections.Generic;

namespace NurbsSharp.Samples.Viewer
{
    internal class PointRenderer : IDisposable
    {
        private int _vao;
        private int _vbo;
        private int _pointCount;

        public PointRenderer(Vector3Double[]? points)
        {
            Initialize(points);
        }

        public PointRenderer(List<Vector3Double>? points)
        {
            if (points == null)
                Initialize((Vector3Double[]?)null);
            else
                Initialize(points.ToArray());
        }

        public void Initialize(Vector3Double[]? points)
        {
            _pointCount = (points != null) ? points.Length : 0;
            if (_pointCount == 0) return;

            float[] verts = ConvertVerticesToFloatArray(points);

            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindVertexArray(0);
        }

        public void Draw(ShaderProgram shader, Matrix4 mvp, float pointSize = 8.0f)
        {
            if (_pointCount == 0) return;
            shader.Use();
            shader.SetMatrix4("uMVP", ref mvp);

            GL.BindVertexArray(_vao);
            GL.PointSize(pointSize);
            GL.DrawArrays(PrimitiveType.Points, 0, _pointCount);
            GL.BindVertexArray(0);
        }

        private float[] ConvertVerticesToFloatArray(Vector3Double[]? vertices)
        {
            if (vertices == null || vertices.Length == 0)
                return Array.Empty<float>();

            float[] result = new float[vertices.Length * 3];
            for (int i = 0; i < vertices.Length; i++)
            {
                result[i * 3 + 0] = (float)vertices[i].X;
                result[i * 3 + 1] = (float)vertices[i].Y;
                result[i * 3 + 2] = (float)vertices[i].Z;
            }
            return result;
        }

        public void Dispose()
        {
            if (_vbo != 0)
                GL.DeleteBuffer(_vbo);
            if (_vao != 0)
                GL.DeleteVertexArray(_vao);
        }
    }
}
