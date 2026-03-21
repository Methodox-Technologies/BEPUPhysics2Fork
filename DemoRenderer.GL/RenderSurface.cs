using BepuUtilities;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;

namespace DemoRenderer
{
    /// <summary>
    /// The swap chain, drawing surfaces, and the device contexts associated with a window.
    /// </summary>
    public class RenderSurface : Disposable
    {
        private readonly NativeWindow window;

        /// <summary>
        /// Gets the current resolution of the render surface. To change the resolution, use Resize.
        /// </summary>
        public Int2 Resolution { get; private set; }

        /// <summary>
        /// Constructs a new swap surface.
        /// </summary> 
        /// <param name="window">Window to build a swap chain and drawing surface for.</param>
        /// <param name="resolution">Resolution of the rendering surface.</param>
        /// <param name="enableDeviceDebugLayer">Whether to use the debug layer for this window's graphics device.</param>
        public RenderSurface(NativeWindow window, Int2 resolution, bool fullScreen = false, bool enableDeviceDebugLayer = false)
        {
            this.window = window;

            // In OpenTK 4, the OpenGL context is created with the NativeWindow.
            // If needed, ensure the window was created with the desired API/profile/version in NativeWindowSettings.
            window.MakeCurrent();

            if (enableDeviceDebugLayer)
            {
                GL.Enable(EnableCap.DebugOutput);
                GL.Enable(EnableCap.DebugOutputSynchronous);
                GL.DebugMessageCallback(DebugCallback, IntPtr.Zero);
            }

            Resolution = resolution;
            window.WindowState = fullScreen ? WindowState.Fullscreen : WindowState.Normal;
            GL.Viewport(0, 0, Resolution.X, Resolution.Y);
        }

        private static void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            Console.Error.WriteLine($"{source}, {type}, {id}, {severity}, {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(message)}");
            if (type == DebugType.DebugTypeError)
                throw new Exception();
        }

        public void Resize(Int2 resolution, bool fullScreen)
        {
            Resolution = resolution;
            window.MakeCurrent();
            window.WindowState = fullScreen ? WindowState.Fullscreen : WindowState.Normal;
            window.ClientSize = new OpenTK.Mathematics.Vector2i(Resolution.X, Resolution.Y);
            GL.Viewport(0, 0, Resolution.X, Resolution.Y);
        }

        public void Present() => window.Context.SwapBuffers();

        protected override void DoDispose()
        {
            // The window owns the context in OpenTK 4, so don't dispose the context separately here.
            // Dispose the window elsewhere if this class does not own it.
        }
    }
}
