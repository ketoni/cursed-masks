using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class StartTextContainer : MonoBehaviour, IControlSchema
{
    public GameObject KeyboardIcon;
    public GameObject GamepadIcon;

    public void ChangeControlSchema(InputDevice device)
    {
        // Change UI Icon to gamepad
        if(device is Gamepad)
        {
            KeyboardIcon.gameObject.SetActive(false);
            GamepadIcon.gameObject.SetActive(true);
        }
        // Change UI Icon to Keyboard/Mouse
        else if (device is Keyboard)
        {
            GamepadIcon.gameObject.SetActive(false);
            KeyboardIcon.gameObject.SetActive(true);
        }
    }
}
