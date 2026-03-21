using BepuUtilities;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Mathematics;
using OpenTK.Platform;
using OpenTK.Windowing.Desktop;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Vector2 = System.Numerics.Vector2;

namespace DemoUtilities
{
    public enum WindowMode
    {
        FullScreen,
        Windowed
    }

    /// <summary>
    /// Simple and not-very-general-purpose window management.
    /// </summary>
    public class Window : IDisposable
    {
        public NativeWindow window;

        private bool resized;

        private WindowMode windowMode;
        WindowMode WindowMode
        {
            get
            {
                return windowMode;
            }
            set
            {
                switch (value)
                {
                    case WindowMode.FullScreen:
                        if (windowMode != WindowMode.FullScreen)
                        {
                            windowMode = value;
                            window.WindowState = OpenTK.Windowing.Common.WindowState.Fullscreen;
                            window.WindowBorder = OpenTK.Windowing.Common.WindowBorder.Hidden;
                            window.Location = new Vector2i(0, 0);
                            var primaryBounds = window.CurrentMonitor.ClientArea;
                            window.Size = new Vector2i(primaryBounds.Size.X, primaryBounds.Size.Y);
                            resized = true;
                        }
                        break;
                    case WindowMode.Windowed:
                        if (windowMode != WindowMode.Windowed)
                        {
                            windowMode = value;
                            window.WindowState = OpenTK.Windowing.Common.WindowState.Normal;
                            window.WindowBorder = OpenTK.Windowing.Common.WindowBorder.Resizable;
                            resized = true;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Gets or sets the resolution of the window's body.
        /// </summary>
        public Int2 Resolution
        {
            get
            {
                return new Int2(window.ClientSize.X, window.ClientSize.Y);
            }
            set
            {
                window.ClientSize = new Vector2i(value.X, value.Y);
                resized = true;
            }
        }

        public unsafe IntPtr Handle { get { return (IntPtr)window.WindowPtr; } }

        /// <summary>
        /// Gets whether the window is currently focused.
        /// </summary>
        public bool Focused { get { return window.IsFocused; } }

        /// <summary>
        /// Constructs a new rendering-capable window.
        /// </summary>
        /// <param name="title">Title of the window.</param>
        /// <param name="resolution">Initial size in pixels of the window's drawable surface.</param>
        /// <param name="location">Initial location of the window's drawable surface.</param>
        /// <param name="windowMode">Initial window mode.</param>
        public Window(string title, Int2 resolution, Int2 location, WindowMode windowMode)
        {
            var settings = new NativeWindowSettings
            {
                Title = title,
                Size = new Vector2i(resolution.X, resolution.Y),
                Location = new Vector2i(location.X, location.Y),
                StartVisible = true,
                WindowBorder = OpenTK.Windowing.Common.WindowBorder.Fixed
            };

            window = new NativeWindow(settings);
            Resolution = resolution;
            window.Resize += args => resized = true;
            window.Closing += OnClosing;
            WindowMode = windowMode;
        }

        /// <summary>
        /// Constructs a new rendering-capable window.
        /// </summary>
        /// <param name="title">Title of the window.</param>
        /// <param name="resolution">Initial size in pixels of the window's drawable surface.</param>
        /// <param name="windowMode">Initial window mode.</param>
        public Window(string title, Int2 resolution, WindowMode windowMode)
            : this(
                title,
                resolution,
                new Int2(
                    (GetPrimaryMonitorSize().X - resolution.X) / 2,
                    (GetPrimaryMonitorSize().Y - resolution.Y) / 2),
                windowMode)
        {
        }

        private static Vector2i GetPrimaryMonitorSize()
        {
            var settings = NativeWindowSettings.Default;
            settings.StartVisible = false;
            using var tempWindow = new NativeWindow(settings);
            var area = tempWindow.CurrentMonitor.ClientArea;
            return area.Size;
        }

        public Vector2 GetNormalizedMousePosition(Int2 mousePosition)
        {
            return new Vector2((float)mousePosition.X / Resolution.X, (float)mousePosition.Y / Resolution.Y);
        }

        private void OnClosing(CancelEventArgs e)
        {
            //This will redundantly call window.Close, but that's fine.
            tryToClose = true;
            e.Cancel = true;
        }

        private bool windowUpdateLoopRunning;
        private bool tryToClose;

        /// <summary>
        /// Closes the window at the next available opportunity.
        /// </summary>
        public void Close()
        {
            if (windowUpdateLoopRunning)
                tryToClose = true;
            else
                window.Close();
        }

        /// <summary>
        /// Launches the update loop for the window. Processes events before every invocation of the update handler.
        /// </summary>
        /// <param name="updateHandler">Delegate to be invoked within the loop repeatedly.</param>
        public void Run(Action<float> updateHandler, Action<Int2> onResize)
        {
            long previousTime = Stopwatch.GetTimestamp();
            windowUpdateLoopRunning = true;

            while (true)
            {
                if (disposed)
                    break;

                if (resized)
                {
                    //Note that minimizing or resizing the window to invalid sizes don't result in actual resize attempts. Zero width rendering surfaces aren't allowed.
                    if (window.ClientSize.X > 0 && window.ClientSize.Y > 0)
                    {
                        onResize(new Int2(window.ClientSize.X, window.ClientSize.Y));
                    }
                    resized = false;
                }

                window.ProcessEvents(10);

                if (tryToClose || window.IsExiting)
                {
                    window.Close();
                    break;
                }

                long time = Stopwatch.GetTimestamp();
                var dt = (float)((time - previousTime) / (double)Stopwatch.Frequency);
                previousTime = time;

                if (window.WindowState != OpenTK.Windowing.Common.WindowState.Minimized)
                {
                    updateHandler(dt);
                }
                else
                {
                    //If the window is minimized, take a breather.
                    Thread.Sleep(1);
                }
            }

            windowUpdateLoopRunning = false;
        }

        private bool disposed;
        public void Dispose()
        {
            if (!disposed)
            {
                window.Dispose();
                disposed = true;
            }
        }
    }
}
