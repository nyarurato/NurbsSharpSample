using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using NurbsSharp.Core;
using System;

namespace NurbsSharp.Samples.Viewer
{
    internal class MeshRenderer : IDisposable
    {
        private int _vao;
        private int _vbo;
        private int _ebo;
        private int _indexCount;

        public MeshRenderer(Vector3Double[] vertices, int[] indexes)
        {
            Initialize(vertices, indexes);
        }

        public void Initialize(Vector3Double[] vertices, int[] indexes)
        {
            _indexCount = (indexes != null) ? indexes.Length : 0;

            float[] verts = ConvertVerticesToFloatArray(vertices);

            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);

            if (indexes != null && indexes.Length > 0)
            {
                _ebo = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
                GL.BufferData(BufferTarget.ElementArrayBuffer, indexes.Length * sizeof(int), indexes, BufferUsageHint.StaticDraw);
            }

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Unbind VAO to reduce accidental state changes
            GL.BindVertexArray(0);
        }

        public void DrawSolid(ShaderProgram shader, Matrix4 mvp)
        {
            if (_indexCount == 0) return;
            shader.Use();
            shader.SetMatrix4("uMVP", ref mvp);

            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public void DrawWireframe(ShaderProgram shader, Matrix4 mvp, float lineWidth)
        {
            if (_indexCount == 0) return;
            shader.Use();
            shader.SetMatrix4("uMVP", ref mvp);

            GL.Enable(EnableCap.PolygonOffsetLine);
            GL.PolygonOffset(-1.0f, -1.0f);

            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
            GL.BindVertexArray(_vao);
            GL.LineWidth(lineWidth);
            GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            GL.Disable(EnableCap.PolygonOffsetLine);
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

        public void Dispose()
        {
            if (_vbo != 0)
                GL.DeleteBuffer(_vbo);
            if (_ebo != 0)
                GL.DeleteBuffer(_ebo);
            if (_vao != 0)
                GL.DeleteVertexArray(_vao);
        }
    }
}
