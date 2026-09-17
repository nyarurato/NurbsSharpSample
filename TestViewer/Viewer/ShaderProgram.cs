using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace NurbsSharp.Samples.Viewer
{
    internal class ShaderProgram : IDisposable
    {
        public int ProgramId { get; private set; }

        public ShaderProgram(string vertexSource, string fragmentSource)
        {
            ProgramId = CompileShaderProgram(vertexSource, fragmentSource);
        }

        public void Use()
        {
            GL.UseProgram(ProgramId);
        }

        public void SetMatrix4(string name, ref Matrix4 matrix)
        {
            int loc = GL.GetUniformLocation(ProgramId, name);
            if (loc >= 0)
                GL.UniformMatrix4(loc, false, ref matrix);
        }

        public void SetFloat(string name, float val)
        {
            int loc = GL.GetUniformLocation(ProgramId, name);
            if (loc >= 0)
                GL.Uniform1(loc, val);
        }

        public void Dispose()
        {
            if (ProgramId != 0)
            {
                GL.DeleteProgram(ProgramId);
                ProgramId = 0;
            }
        }

        // Public helper to create a basic pass-through GLSL program that takes a position and MVP uniform
        public static ShaderProgram CreateSimpleColorProgram(string fragmentColor)
        {
            string vs = "#version 330 core\nlayout(location=0) in vec3 aPosition;\nuniform mat4 uMVP;uniform float uZOffset;void main(){ vec3 p = aPosition; p.z += uZOffset; gl_Position = uMVP * vec4(p, 1.0); }\n";
            string fs = $"#version 330 core\nout vec4 FragColor; void main(){{ FragColor = vec4({fragmentColor}, 1.0); }}\n";
            return new ShaderProgram(vs, fs);
        }

        // Public helper to create a color program that accepts RGBA values (including alpha)
        public static ShaderProgram CreateSimpleColorProgramWithAlpha(string fragmentColorWithAlpha)
        {
            string vs = "#version 330 core\nlayout(location=0) in vec3 aPosition;\nuniform mat4 uMVP;void main(){ gl_Position = uMVP * vec4(aPosition, 1.0); }\n";
            string fs = $"#version 330 core\nout vec4 FragColor; void main(){{ FragColor = vec4({fragmentColorWithAlpha}); }}\n";
            return new ShaderProgram(vs, fs);
        }

        private static int CompileShaderProgram(string vertexSource, string fragmentSource)
        {
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexSource);
            GL.CompileShader(vertexShader);
            CheckShaderCompilation(vertexShader, "Vertex");

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentSource);
            GL.CompileShader(fragmentShader);
            CheckShaderCompilation(fragmentShader, "Fragment");

            int program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);
            CheckProgramLinking(program);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            return program;
        }

        private static void CheckShaderCompilation(int shader, string type)
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"{type} Shader compilation failed: {infoLog}");
            }
        }

        private static void CheckProgramLinking(int program)
        {
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(program);
                throw new Exception($"Shader program linking failed: {infoLog}");
            }
        }
    }
}
