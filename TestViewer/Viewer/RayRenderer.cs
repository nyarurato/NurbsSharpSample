using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using NurbsSharp.Core;
using System;

namespace NurbsSharp.Samples.Viewer
{
    /// <summary>
    /// Renders rays as lines in 3D space.
    /// </summary>
    internal class RayRenderer : IDisposable
    {
        private int _vao;
        private int _vbo;
        private int _vertexCount;

        public RayRenderer(Vector3Double[] lineVertices)
        {
            Initialize(lineVertices);
        }

        public void Initialize(Vector3Double[] lineVertices)
        {
            float[] vertices = new float[lineVertices.Length * 3];
            for (int i = 0; i < lineVertices.Length; i++)
            {
                vertices[i * 3] = (float)lineVertices[i].X;
                vertices[i * 3 + 1] = (float)lineVertices[i].Y;
                vertices[i * 3 + 2] = (float)lineVertices[i].Z;
            }

            _vertexCount = lineVertices.Length;
            if (_vertexCount == 0)
                return;

            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindVertexArray(0);
        }

        public void Draw(ShaderProgram shader, Matrix4 mvp, float lineWidth = 2.0f)
        {
            if (_vertexCount == 0) return;

            shader.Use();
            shader.SetMatrix4("uMVP", ref mvp);

            GL.BindVertexArray(_vao);
            GL.LineWidth(lineWidth);
            GL.DrawArrays(PrimitiveType.Lines, 0, _vertexCount);
            GL.BindVertexArray(0);
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
