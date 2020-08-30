﻿namespace SFBuilder
{
    /// <summary>
    /// Definitions of controls for game input
    /// </summary>
    public enum GameControl : byte
    {
        Camera_MoveBackward = 0,
        Camera_MoveForward = 1,
        Camera_MoveLeft = 2,
        Camera_MoveRight = 3,

        Camera_RotateLeft = 4,
        Camera_RotateRight = 5,
        Camera_TiltUp = 6,
        Camera_TiltDown = 7,

        Camera_ZoomIn = 8,
        Camera_ZoomOut = 9,

        Gameplay_CancelAndMenu = 10,
        Gameplay_Placement = 11,
        Gameplay_Undo = 12,

        Gamepad_CursorDown = 13,
        Gamepad_CursorLeft = 14,
        Gamepad_CursorRight = 15,
        Gamepad_CursorUp = 16,

        Gamepad_ZoomToggle = 17,

        Mouse_ToggleMovement = 18,
        Mouse_ToggleRotation = 19,
        Mouse_ToggleZoom = 20
    }

    /// <summary>
    /// Definitions of supported game input types
    /// </summary>
    public enum InputType : byte
    {
        Keyboard = 0,
        Mouse = 1,
        Gamepad = 2
    }

    /// <summary>
    /// Definitions of generic controls, i.e., those that are used for multiple sets of bindings (one for keyboard, one for gamepad)
    /// </summary>
    public enum GenericControl : byte
    {
        Camera_MoveBackward = 0,
        Camera_MoveForward = 1,
        Camera_MoveLeft = 2,
        Camera_MoveRight = 3,

        Camera_RotateLeft = 4,
        Camera_RotateRight = 5,
        Camera_TiltUp = 6,
        Camera_TiltDown = 7,

        Camera_ZoomIn = 8,
        Camera_ZoomOut = 9,

        Gameplay_CancelAndMenu = 10,
        Gameplay_Placement = 11,
        Gameplay_Undo = 12
    }

    /// <summary>
    /// Definitions of other controls, i.e., those that only have a single set of bindings
    /// </summary>
    public enum OtherControl : byte
    {
        Gamepad_CursorDown = 0,
        Gamepad_CursorLeft = 1,
        Gamepad_CursorRight = 2,
        Gamepad_CursorUp = 3,
        
        Gamepad_ZoomToggle = 4,

        Mouse_ToggleMovement = 5,
        Mouse_ToggleRotation = 6,
        Mouse_ToggleZoom = 7
    }
}