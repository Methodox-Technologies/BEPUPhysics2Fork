using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Runtime.CompilerServices;

namespace DemoUtilities
{
    using KeySet = QuickSet<Keys, KeyComparer>;
    using MouseButtonSet = QuickSet<MouseButton, MouseButtonComparer>;

    struct KeyComparer : IEqualityComparerRef<Keys>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ref Keys a, ref Keys b)
        {
            return a == b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Hash(ref Keys item)
        {
            return (int)item;
        }
    }

    struct MouseButtonComparer : IEqualityComparerRef<MouseButton>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ref MouseButton a, ref MouseButton b)
        {
            return a == b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Hash(ref MouseButton item)
        {
            return (int)item;
        }
    }

    public class Input : IDisposable
    {
        NativeWindow window;

        //You could use GetState-like stuff to avoid the need to explicitly grab these, but shrug. This keeps events localized to just the window, and we can do a little logic of our own.
        KeySet anyDownedKeys;
        KeySet downedKeys;
        KeySet previousDownedKeys;
        MouseButtonSet anyDownedButtons;
        MouseButtonSet downedButtons;
        MouseButtonSet previousDownedButtons;
        BufferPool pool;
        public QuickList<char> TypedCharacters;

        /// <summary>
        /// Forces the mouse to stay at the center of the screen by recentering it on every flush.
        /// In OpenTK 4, this is implemented via cursor grabbing instead of manual recentering.
        /// </summary>
        public bool MouseLocked
        {
            get; set;
        }

        Int2 WindowCenter { get { return new Int2(window.Size.X / 2, window.Size.Y / 2); } }

        /// <summary>
        /// Gets or sets the mouse position in window coordinates without changing the net mouse delta.
        /// </summary>
        public Int2 MousePosition
        {
            get
            {
                var p = window.MousePosition;
                return new Int2 { X = (int)p.X, Y = (int)p.Y };
            }
            set
            {
                //Note that changing the cursor position does not change the raw mouse x/y.
                window.MousePosition = new Vector2(value.X, value.Y);
            }
        }

        /// <summary>
        /// Gets the change in mouse position since the previous flush.
        /// </summary>
        public Int2 MouseDelta
        {
            get
            {
                return mouseDelta;
            }
        }

        /// <summary>
        /// Gets the amount of upward mouse wheel scrolling since the last flush regardless of how much downward scrolling occurred.
        /// </summary>
        public float ScrolledUp { get; private set; }

        /// <summary>
        /// Gets the amount of downward mouse wheel scrolling since the last flush regardless of how much upward scrolling occurred.
        /// </summary>
        public float ScrolledDown { get; private set; }

        /// <summary>
        /// Gets the mouse wheel scroll delta since the last flush.
        /// </summary>
        public float ScrollDelta { get { return ScrolledUp + ScrolledDown; } }

        public Input(Window window, BufferPool pool)
        {
            this.window = window.window;
            this.window.KeyDown += KeyDown;
            this.window.KeyUp += KeyUp;
            this.window.MouseDown += MouseDown;
            this.window.MouseUp += MouseUp;
            this.window.MouseWheel += MouseWheel;
            this.window.TextInput += TextInput; //KeyPress replaced by TextInput in OpenTK 4
            this.pool = pool;

            anyDownedButtons = new MouseButtonSet(8, pool);
            downedButtons = new MouseButtonSet(8, pool);
            previousDownedButtons = new MouseButtonSet(8, pool);

            anyDownedKeys = new KeySet(8, pool);
            downedKeys = new KeySet(8, pool);
            previousDownedKeys = new KeySet(8, pool);

            TypedCharacters = new QuickList<char>(32, pool);
        }

        private void TextInput(TextInputEventArgs e)
        {
            TypedCharacters.Add((char)e.Unicode, pool);
        }

        private void MouseWheel(MouseWheelEventArgs e)
        {
            if (e.OffsetY > 0)
                ScrolledUp += e.OffsetY;
            else
                ScrolledDown += e.OffsetY;
        }

        private void MouseDown(MouseButtonEventArgs e)
        {
            anyDownedButtons.Add(e.Button, pool);
            downedButtons.Add(e.Button, pool);
        }

        private void MouseUp(MouseButtonEventArgs e)
        {
            downedButtons.FastRemove(e.Button);
        }

        private void KeyDown(KeyboardKeyEventArgs e)
        {
            anyDownedKeys.Add(e.Key, pool);
            downedKeys.Add(e.Key, pool);
            //Unfortunately, backspace isn't reported by text input, so we do it manually.
            if (e.Key == Keys.Backspace)
                TypedCharacters.Add('\b', pool);
        }

        private void KeyUp(KeyboardKeyEventArgs e)
        {
            downedKeys.FastRemove(e.Key);
        }

        /// <summary>
        /// Gets whether a key is currently pressed according to the latest event processing call.
        /// </summary>
        public bool IsDown(Keys key)
        {
            return downedKeys.Contains(key);
        }

        /// <summary>
        /// Gets whether a key was down at the time of the previous flush.
        /// </summary>
        public bool WasDown(Keys key)
        {
            return previousDownedKeys.Contains(key);
        }

        /// <summary>
        /// Gets whether a down event occurred at any point between the previous flush and up to the last event process call for a key that was not down in the previous flush.
        /// </summary>
        public bool WasPushed(Keys key)
        {
            return !previousDownedKeys.Contains(key) && anyDownedKeys.Contains(key);
        }

        /// <summary>
        /// Gets whether a button is currently pressed according to the latest event processing call.
        /// </summary>
        public bool IsDown(MouseButton button)
        {
            return downedButtons.Contains(button);
        }

        /// <summary>
        /// Gets whether a button was down at the time of the previous flush.
        /// </summary>
        public bool WasDown(MouseButton mouseButton)
        {
            return previousDownedButtons.Contains(mouseButton);
        }

        /// <summary>
        /// Gets whether a down event occurred at any point between the previous flush and up to the last event process call for a button that was not down in the previous flush.
        /// </summary>
        public bool WasPushed(MouseButton button)
        {
            return !previousDownedButtons.Contains(button) && anyDownedButtons.Contains(button);
        }

        Int2 mouseDelta;
        Int2 previousRawMouse;

        public void Start()
        {
            var mouseState = window.MouseState;

            //Given a long enough time, this could theoretically hit overflow.
            //But that would require hours of effort with a high DPI mouse, and this is a demo application...
            var currentRawMouse = new Int2((int)mouseState.X, (int)mouseState.Y);

            mouseDelta.X = currentRawMouse.X - previousRawMouse.X;
            mouseDelta.Y = currentRawMouse.Y - previousRawMouse.Y;
            previousRawMouse = currentRawMouse;

            if (MouseLocked)
            {
                //This used to manually recenter the mouse every frame.
                //In OpenTK 4, we use cursor grabbing instead, which is the intended API.
                window.CursorState = CursorState.Grabbed;

                if (window.SupportsRawMouseInput)
                    window.RawMouseInput = true;
            }
            else
            {
                if (window.SupportsRawMouseInput)
                    window.RawMouseInput = false;

                window.CursorState = CursorState.Normal;
            }
        }

        public void End()
        {
            anyDownedKeys.Clear();
            anyDownedButtons.Clear();
            previousDownedKeys.Clear();
            previousDownedButtons.Clear();

            for (int i = 0; i < downedKeys.Count; ++i)
                previousDownedKeys.Add(downedKeys[i], pool);

            for (int i = 0; i < downedButtons.Count; ++i)
                previousDownedButtons.Add(downedButtons[i], pool);

            ScrolledDown = 0;
            ScrolledUp = 0;
            TypedCharacters.Count = 0;
        }

        /// <summary>
        /// Unhooks the input management from the window.
        /// </summary>
        public void Dispose()
        {
            window.KeyDown -= KeyDown;
            window.KeyUp -= KeyUp;
            window.MouseDown -= MouseDown;
            window.MouseUp -= MouseUp;
            window.MouseWheel -= MouseWheel;
            window.TextInput -= TextInput;

            anyDownedKeys.Dispose(pool);
            downedKeys.Dispose(pool);
            previousDownedKeys.Dispose(pool);
            anyDownedButtons.Dispose(pool);
            downedButtons.Dispose(pool);
            previousDownedButtons.Dispose(pool);
        }
    }
}
