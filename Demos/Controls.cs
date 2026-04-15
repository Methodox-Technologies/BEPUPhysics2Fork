using DemoUtilities;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Demos;


/// <summary>
/// Caches strings for enum values to avoid enum boxing.
/// </summary>
static class ControlStrings
{
    static Dictionary<Keys, string> keys;
    static Dictionary<MouseButton, string> mouseButtons;
    static Dictionary<MouseWheelAction, string> mouseWheel;

    public static string GetName(Keys key)
    {
        return keys[key];
    }
    public static string GetName(MouseButton button)
    {
        return mouseButtons[button];
    }
    public static string GetName(MouseWheelAction wheelAction)
    {
        return mouseWheel[wheelAction];
    }

    static ControlStrings()
    {
        keys = new Dictionary<Keys, string>();
        var keyNames = Enum.GetNames(typeof(Keys));
        Keys[] keyValues = (Keys[])Enum.GetValues(typeof(Keys));
        for (int i = 0; i < keyNames.Length; ++i)
        {
            keys.TryAdd(keyValues[i], keyNames[i]);
        }
        mouseButtons = new Dictionary<MouseButton, string>();
        var mouseButtonNames = Enum.GetNames(typeof(MouseButton));
        MouseButton[] mouseButtonValues = (MouseButton[])Enum.GetValues(typeof(MouseButton));
        for (int i = 0; i < mouseButtonNames.Length; ++i)
        {
            mouseButtons.TryAdd(mouseButtonValues[i], mouseButtonNames[i]);
        }
        mouseWheel = new Dictionary<MouseWheelAction, string>();
        var wheelNames = Enum.GetNames(typeof(MouseWheelAction));
        MouseWheelAction[] wheelValues = (MouseWheelAction[])Enum.GetValues(typeof(MouseWheelAction));
        for (int i = 0; i < wheelNames.Length; ++i)
        {
            mouseWheel.TryAdd(wheelValues[i], wheelNames[i]);
        }
    }
}


public enum HoldableControlType
{
    None,
    Key,
    MouseButton,
}
/// <summary>
/// A control binding which can be held for multiple frames.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct HoldableBind
{
    [FieldOffset(0)]
    public Keys Key;
    [FieldOffset(0)]
    public MouseButton Button;
    [FieldOffset(4)]
    public HoldableControlType Type;

    [FieldOffset(8)]
    public Keys AlternativeKey;
    [FieldOffset(8)]
    public MouseButton AlternativeButton;
    [FieldOffset(12)]
    public HoldableControlType AlternativeType;

    public HoldableBind(Keys key)
        : this()
    {
        Key = key;
        Type = HoldableControlType.Key;
    }
    public HoldableBind(Keys key, Keys alternativeKey)
        : this()
    {
        Key = key;
        Type = HoldableControlType.Key;
        AlternativeKey = alternativeKey;
        AlternativeType = HoldableControlType.Key;
    }
    public HoldableBind(Keys key, MouseButton alternativeButton)
        : this()
    {
        Key = key;
        Type = HoldableControlType.Key;
        AlternativeButton = alternativeButton;
        AlternativeType = HoldableControlType.MouseButton;
    }
    public HoldableBind(MouseButton button)
        : this()
    {
        Button = button;
        Type = HoldableControlType.MouseButton;
    }
    public HoldableBind(MouseButton button, Keys alternativeKey)
        : this()
    {
        Button = button;
        Type = HoldableControlType.MouseButton;
        AlternativeKey = alternativeKey;
        AlternativeType = HoldableControlType.Key;
    }
    public HoldableBind(MouseButton button, MouseButton alternativeButton)
        : this()
    {
        Button = button;
        Type = HoldableControlType.MouseButton;
        AlternativeButton = alternativeButton;
        AlternativeType = HoldableControlType.MouseButton;
    }

    public static implicit operator HoldableBind(Keys key)
    {
        return new HoldableBind(key);
    }
    public static implicit operator HoldableBind((Keys, Keys) binds)
    {
        return new HoldableBind(binds.Item1, binds.Item2);
    }
    public static implicit operator HoldableBind((Keys, MouseButton) binds)
    {
        return new HoldableBind(binds.Item1, binds.Item2);
    }
    public static implicit operator HoldableBind(MouseButton button)
    {
        return new HoldableBind(button);
    }
    public static implicit operator HoldableBind((MouseButton, Keys) binds)
    {
        return new HoldableBind(binds.Item1, binds.Item2);
    }
    public static implicit operator HoldableBind((MouseButton, MouseButton) binds)
    {
        return new HoldableBind(binds.Item1, binds.Item2);
    }
    public bool IsDown(Input input)
    {
        if (Type == HoldableControlType.Key && input.IsDown(Key))
            return true;
        if (Type == HoldableControlType.MouseButton && input.IsDown(Button))
            return true;
        if (AlternativeType == HoldableControlType.Key && input.IsDown(AlternativeKey))
            return true;
        if (AlternativeType == HoldableControlType.MouseButton && input.IsDown(AlternativeButton))
            return true;
        return false;
    }

    public bool WasPushed(Input input)
    {
        if (Type == HoldableControlType.Key && input.WasPushed(Key))
            return true;
        if (Type == HoldableControlType.MouseButton && input.WasPushed(Button))
            return true;
        if (AlternativeType == HoldableControlType.Key && input.WasPushed(AlternativeKey))
            return true;
        if (AlternativeType == HoldableControlType.MouseButton && input.WasPushed(AlternativeButton))
            return true;
        return false;
    }

    public TextBuilder AppendString(TextBuilder text)
    {
        if (Type == HoldableControlType.Key)
            text.Append(ControlStrings.GetName(Key));
        else if (Type == HoldableControlType.MouseButton)
            text.Append(ControlStrings.GetName(Button));
        if (AlternativeType != HoldableControlType.None)
        {
            if (Type != HoldableControlType.None)
                text.Append(" or ");
            if (AlternativeType == HoldableControlType.Key)
                text.Append(ControlStrings.GetName(AlternativeKey));
            else if (AlternativeType == HoldableControlType.MouseButton)
                text.Append(ControlStrings.GetName(AlternativeButton));
        }
        return text;
    }

}

public enum InstantControlType
{
    None,
    Key,
    MouseButton,
    MouseWheel,
}
public enum MouseWheelAction
{
    ScrollUp,
    ScrollDown
}
/// <summary>
/// A control binding that supports any form of instant action, but may or may not support being held.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct InstantBind
{
    [FieldOffset(0)]
    public Keys Key;
    [FieldOffset(0)]
    public MouseButton Button;
    [FieldOffset(0)]
    public MouseWheelAction Wheel;
    [FieldOffset(4)]
    public InstantControlType Type;

    [FieldOffset(8)]
    public Keys AlternativeKey;
    [FieldOffset(8)]
    public MouseButton AlternativeButton;
    [FieldOffset(8)]
    public MouseWheelAction AlternativeWheel;
    [FieldOffset(12)]
    public InstantControlType AlternativeType;

    public InstantBind(Keys key)
        : this()
    {
        Key = key;
        Type = InstantControlType.Key;
    }
    public InstantBind(Keys key, Keys alternativeKey)
        : this()
    {
        Key = key;
        Type = InstantControlType.Key;
        AlternativeKey = alternativeKey;
        AlternativeType = InstantControlType.Key;
    }
    public InstantBind(Keys key, MouseButton button)
        : this()
    {
        Key = key;
        Type = InstantControlType.Key;
        AlternativeButton = button;
        AlternativeType = InstantControlType.MouseButton;
    }
    public InstantBind(Keys key, MouseWheelAction wheelAction)
        : this()
    {
        Key = key;
        Type = InstantControlType.Key;
        AlternativeWheel = wheelAction;
        AlternativeType = InstantControlType.MouseWheel;
    }

    public InstantBind(MouseButton button)
        : this()
    {
        Button = button;
        Type = InstantControlType.MouseButton;
    }
    public InstantBind(MouseButton button, Keys alternativeKey)
        : this()
    {
        Button = button;
        Type = InstantControlType.MouseButton;
        AlternativeKey = alternativeKey;
        AlternativeType = InstantControlType.Key;
    }
    public InstantBind(MouseButton button, MouseButton alternativeButton)
        : this()
    {
        Button = button;
        Type = InstantControlType.MouseButton;
        AlternativeButton = alternativeButton;
        AlternativeType = InstantControlType.MouseButton;
    }
    public InstantBind(MouseButton button, MouseWheelAction alternativeWheel)
        : this()
    {
        Button = button;
        Type = InstantControlType.MouseButton;
        AlternativeWheel = alternativeWheel;
        AlternativeType = InstantControlType.MouseWheel;
    }

    public InstantBind(MouseWheelAction wheel)
        : this()
    {
        Wheel = wheel;
        Type = InstantControlType.MouseWheel;
    }
    public InstantBind(MouseWheelAction wheel, Keys alternativeKey)
        : this()
    {
        Wheel = wheel;
        Type = InstantControlType.MouseWheel;
        AlternativeKey = alternativeKey;
        AlternativeType = InstantControlType.Key;
    }
    public InstantBind(MouseWheelAction wheel, MouseButton alternativeButton)
        : this()
    {
        Wheel = wheel;
        Type = InstantControlType.MouseWheel;
        AlternativeButton = alternativeButton;
        AlternativeType = InstantControlType.MouseButton;
    }
    public InstantBind(MouseWheelAction wheel, MouseWheelAction alternativeWheel)
        : this()
    {
        Wheel = wheel;
        Type = InstantControlType.MouseWheel;
        AlternativeWheel = alternativeWheel;
        AlternativeType = InstantControlType.MouseWheel;
    }

    public static implicit operator InstantBind(Keys key)
    {
        return new InstantBind(key);
    }
    public static implicit operator InstantBind((Keys, Keys) binds)
    {
        return new InstantBind(binds.Item1, binds.Item2);
    }
    public static implicit operator InstantBind((Keys, MouseButton) binds)
    {
        return new InstantBind(binds.Item1, binds.Item2);
    }
    public static implicit operator InstantBind((Keys, MouseWheelAction) binds)
    {
        return new InstantBind(binds.Item1, binds.Item2);
    }

    public static implicit operator InstantBind(MouseButton button)
    {
        return new InstantBind(button);
    }
    public static implicit operator InstantBind((MouseButton, Keys) binds)
    {
        return new InstantBind(binds.Item1, binds.Item2);
    }
    public static implicit operator InstantBind((MouseButton, MouseButton) binds)
    {
        return new InstantBind(binds.Item1, binds.Item2);
    }
    public static implicit operator InstantBind((MouseButton, MouseWheelAction) binds)
    {
        return new InstantBind(binds.Item1, binds.Item2);
    }

    public static implicit operator InstantBind(MouseWheelAction wheel)
    {
        return new InstantBind(wheel);
    }
    public static implicit operator InstantBind((MouseWheelAction, Keys) binds)
    {
        return new InstantBind(binds.Item1, binds.Item2);
    }
    public static implicit operator InstantBind((MouseWheelAction, MouseButton) binds)
    {
        return new InstantBind(binds.Item1, binds.Item2);
    }
    public static implicit operator InstantBind((MouseWheelAction, MouseWheelAction) binds)
    {
        return new InstantBind(binds.Item1, binds.Item2);
    }

    public bool WasTriggered(Input input)
    {
        switch (Type)
        {
            case InstantControlType.Key:
                if (input.WasPushed(Key)) return true;
                break;
            case InstantControlType.MouseButton:
                if (input.WasPushed(Button)) return true;
                break;
            case InstantControlType.MouseWheel:
                if (Wheel == MouseWheelAction.ScrollUp ? input.ScrolledUp > 0 : input.ScrolledDown < 0) return true;
                break;
        }
        switch (AlternativeType)
        {
            case InstantControlType.Key:
                return input.WasPushed(AlternativeKey);
            case InstantControlType.MouseButton:
                return input.WasPushed(AlternativeButton);
            case InstantControlType.MouseWheel:
                return AlternativeWheel == MouseWheelAction.ScrollUp ? input.ScrolledUp > 0 : input.ScrolledDown < 0;
        }
        return false;
    }

    public TextBuilder AppendString(TextBuilder text)
    {
        if (Type == InstantControlType.None && AlternativeType == InstantControlType.None)
            text.Append("(no bind)");
        switch (Type)
        {
            case InstantControlType.Key:
                text.Append(ControlStrings.GetName(Key));
                break;
            case InstantControlType.MouseButton:
                text.Append(ControlStrings.GetName(Button));
                break;
            case InstantControlType.MouseWheel:
                text.Append(ControlStrings.GetName(Wheel));
                break;
        }
        if (AlternativeType != InstantControlType.None)
        {
            if (Type != InstantControlType.None)
            {
                text.Append(" or ");
            }
            switch (AlternativeType)
            {
                case InstantControlType.Key:
                    text.Append(ControlStrings.GetName(AlternativeKey));
                    break;
                case InstantControlType.MouseButton:
                    text.Append(ControlStrings.GetName(AlternativeButton));
                    break;
                case InstantControlType.MouseWheel:
                    text.Append(ControlStrings.GetName(AlternativeWheel));
                    break;
            }
        }
        return text;
    }
}

public struct Controls
{
    public HoldableBind MoveForward;
    public HoldableBind MoveBackward;
    public HoldableBind MoveLeft;
    public HoldableBind MoveRight;
    public HoldableBind MoveUp;
    public HoldableBind MoveDown;
    public InstantBind MoveSlower;
    public InstantBind MoveFaster;
    public HoldableBind Grab;
    public HoldableBind GrabRotate;
    public float MouseSensitivity;
    public float CameraSlowMoveSpeed;
    public float CameraMoveSpeed;
    public float CameraFastMoveSpeed;

    public HoldableBind SlowTimesteps;
    public InstantBind LockMouse;
    public InstantBind Exit;
    public InstantBind ShowConstraints;
    public InstantBind ShowContacts;
    public InstantBind ShowBoundingBoxes;
    public InstantBind ChangeTimingDisplayMode;
    public InstantBind ChangeDemo;
    public InstantBind ShowControls;

    public static Controls Default
    {
        get
        {
            return new Controls
            {
                MoveForward = Keys.W,
                MoveBackward = Keys.S,
                MoveLeft = Keys.A,
                MoveRight = Keys.D,
                MoveDown = Keys.LeftControl,
                MoveUp = Keys.LeftShift,
                MoveSlower = (MouseWheelAction.ScrollDown, Keys.Y),
                MoveFaster = (MouseWheelAction.ScrollUp, Keys.U),
                Grab = MouseButton.Right,
                GrabRotate = Keys.Q,
                MouseSensitivity = 1.5e-3f,
                CameraSlowMoveSpeed = 0.5f,
                CameraMoveSpeed = 5,
                CameraFastMoveSpeed = 50,
                SlowTimesteps = (MouseButton.Middle, Keys.O),

                LockMouse = Keys.Tab,
                Exit = Keys.Escape,
                ShowConstraints = Keys.J,
                ShowContacts = Keys.K,
                ShowBoundingBoxes = Keys.L,
                ChangeTimingDisplayMode = Keys.F2,
                ChangeDemo = (Keys.GraveAccent, Keys.F3),
                ShowControls = Keys.F1,
            };
        }

    }
}