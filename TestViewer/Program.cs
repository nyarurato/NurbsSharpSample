using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;


namespace NurbsSharp.Samples.OpenTK
{
    internal static class Program
    {
        private static void Main()
        {
            GameWindowSettings gameWindowSettings = GameWindowSettings.Default;
            NativeWindowSettings nativeWindowSettings = NativeWindowSettings.Default;
            nativeWindowSettings.ClientSize = (800, 600);
            nativeWindowSettings.Title = "NurbsSharp OpenTK Viewer Sample";
            using var window = new Viewer.ViewerWindow(gameWindowSettings, nativeWindowSettings);
            window.Run();
        }
    }
}