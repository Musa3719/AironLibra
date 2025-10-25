using UnityEngine;
using UnityEngine.InputSystem;

public static class M_Input
{
    public static bool IsLastInputFromGamepadForAim()
    {
        if (Gamepad.current == null) return false;
        if (Mouse.current == null) return true;

        if (Gamepad.current.rightStick.value != Vector2.zero)
            Options._Instance._IsLastInputFromGamepadForAim = true;
        if (Mouse.current.delta.value != Vector2.zero)
            Options._Instance._IsLastInputFromGamepadForAim = false;

        return Options._Instance._IsLastInputFromGamepadForAim;
    }

    public static float GetCameraZoomInput()
    {
        float value = 0f;
        if (Gamepad.current != null)
            value += Gamepad.current.dpad.value.y / 10f;
        if (Keyboard.current != null)
            value += Input.mouseScrollDelta.y;
        return value;
    }
    public static float GetCameraRotateInput()
    {
        float value = 0f;
        if (Gamepad.current != null)
            value += Gamepad.current.dpad.value.x;
        if (Keyboard.current != null)
            value += Input.mousePositionDelta.normalized.x;
        return value;
    }


    public static Vector3 GetMousePosition()
    {
        return Input.mousePosition;
    }
    public static Vector2 GetGamepadLookVector()
    {
        if (Gamepad.current == null) return Vector2.zero;
        return Gamepad.current.rightStick.value;
    }

    public static float GetAxis(string axisName)
    {
        return GetAxisKeyboard(axisName) + GetAxisGamepad(axisName);
    }
    private static float GetAxisKeyboard(string axisName)
    {
        if (Keyboard.current == null) return 0f;

        return Input.GetAxis(axisName);
    }
    private static float GetAxisGamepad(string axisName)
    {
        if (Gamepad.current == null) return 0f;

        switch (axisName)
        {
            case "Horizontal":
                return Gamepad.current.leftStick.value.x;
            case "Vertical":
                return Gamepad.current.leftStick.value.y;
            default:
                Debug.LogError(axisName + " : axis name not found");
                return 0;
        }
    }

    public static bool GetKeyDown(KeyCode keyCode)
    {
        return Input.GetKeyDown(keyCode);
    }
    public static bool GetKey(KeyCode keyCode)
    {
        return Input.GetKey(keyCode);
    }
    public static bool GetKeyUp(KeyCode keyCode)
    {
        return Input.GetKeyUp(keyCode);
    }

    public static bool GetButtonDown(string buttonName)
    {
        return GetButtonDownKeyboard(buttonName) || GetButtonDownGamepad(buttonName);
    }
    private static bool GetButtonDownKeyboard(string buttonName)
    {
        if (Keyboard.current == null) return false;

        return Input.GetButtonDown(buttonName);
    }
    private static bool GetButtonDownGamepad(string buttonName)
    {
        if (Gamepad.current == null) return false;

        switch (buttonName)
        {
            //case "Language":
            //return Gamepad.current.rightShoulder.isPressed;
            case "UIRight":
                return Gamepad.current.rightShoulder.wasPressedThisFrame;
            case "UILeft":
                return Gamepad.current.leftShoulder.wasPressedThisFrame;
            case "InGameMenu":
                return Gamepad.current.selectButton.wasPressedThisFrame;
            case "Fire1":
                return Gamepad.current.rightShoulder.wasPressedThisFrame;
            case "Fire2":
                return Gamepad.current.leftShoulder.wasPressedThisFrame;
            case "Esc":
                return Gamepad.current.startButton.wasPressedThisFrame || ((GameManager._Instance._GameHUD == null || !GameManager._Instance._GameHUD.activeInHierarchy) && Gamepad.current.buttonEast.wasPressedThisFrame);
            case "Interact":
                return Gamepad.current.buttonWest.wasPressedThisFrame;
            case "Cancel":
                return Gamepad.current.buttonEast.wasPressedThisFrame;
            case "Jump":
                return Gamepad.current.buttonSouth.wasPressedThisFrame;
            case "Crouch":
                return Gamepad.current.buttonNorth.wasPressedThisFrame;
            case "Kick":
                return Gamepad.current.buttonNorth.wasPressedThisFrame;
            case "Dodge":
                return Gamepad.current.buttonSouth.wasPressedThisFrame;
            default:
                Debug.LogError(buttonName + " : button name not found");
                return false;
        }
    }

    public static bool GetButton(string buttonName)
    {
        return GetButtonKeyboard(buttonName) || GetButtonGamepad(buttonName);
    }
    private static bool GetButtonKeyboard(string buttonName)
    {
        if (Keyboard.current == null) return false;

        return Input.GetButton(buttonName);
    }
    private static bool GetButtonGamepad(string buttonName)
    {
        if (Gamepad.current == null) return false;

        switch (buttonName)
        {
            case "Sprint":
                return Gamepad.current.rightTrigger.isPressed;
            case "Run":
                return Gamepad.current.leftTrigger.isPressed;
            case "CameraAngle":
                return Gamepad.current.dpad.value.x != 0f;
            case "CoolCamera":
                return Gamepad.current.leftStickButton.isPressed;
            case "CombatMode":
                return Gamepad.current.rightStickButton.isPressed;
            default:
                Debug.LogError(buttonName + " : button name not found");
                return false;
        }
    }

    public static bool GetButtonUp(string buttonName)
    {
        return GetButtonUpKeyboard(buttonName) || GetButtonUpGamepad(buttonName);
    }

    private static bool GetButtonUpKeyboard(string buttonName)
    {
        if (Keyboard.current == null) return false;

        return Input.GetButtonUp(buttonName);
    }
    private static bool GetButtonUpGamepad(string buttonName)
    {
        if (Gamepad.current == null) return false;

        switch (buttonName)
        {
            default:
                Debug.LogError(buttonName + " : buttonUp name not found");
                return false;
        }
    }
}
