
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class InputManager : Singleton<InputManager>
{
    /*
     * A global input manager that exposes the input system
     */

    Inputs inputs;
    List<InputActionMap> inputLayers;
    InputDevice currentDevice;

    private void Awake()
    {
        // All controls are disabled initially, but other scripts can activate them
        // in their Start() by using `InputManager.Instance.Push()`
        inputLayers = new();
        inputs = new();
        inputs.Disable();

        // Global is always available (pausing basically for now)
        //inputs.Global.Enable();
    }

    private void Start()
    {
        // Start listening on inputs to determine which device the user is using
        InputSystem.onEvent
        .Where(e => e.HasButtonPress())
        .Call(eventPtr =>
        {
            CheckInputDevice(eventPtr.GetAllButtonPresses());
        });
    }

    // This can be used to change the UI icons for the user controls for example
    private void CheckInputDevice(IEnumerable<InputControl> inputControls)
    {
        foreach (var button in inputControls)
        {
            InputDevice device = null;
            // User pressed a gamepad button
            if (button.device is Gamepad)
            {
                device = button.device;
            }
            // User pressed a keyboard button
            else if (button.device is Keyboard)
            {
                device = button.device;
            }
            // Device changed, call necessary methods
            if (device != null && device != currentDevice)
            {
                currentDevice = device;
                var controlSchemas = FindObjectsOfType<MonoBehaviour>().OfType<IControlSchema>().ToList();
                foreach (var schema in controlSchemas)
                {
                    schema.ChangeControlSchema(currentDevice);
                }
                // Return when a device is found, its possible that multiple devices have input the same frame
                // But it makes no sense to trigger code in here multiples times a frame
                return;
            }
        }
    }

    public bool IsOnTop(InputActionMap inputMap)
    {
        if (inputLayers.Count == 0) return false;
        return inputMap == inputLayers[^1]; 
    }

    internal void Push(InputActionMap inputMap)
    {
        // Disables the current inputs and pushes the given new inputs on top
        // Note that the given map is a generated wrapper for an `InputActionMap`
        if (inputLayers.Count > 0)
        {
            var top = inputLayers[^1];
            if (top == inputMap)
            {
                Debug.LogError($"Inputs '{inputMap.name}' already on top of the stack!");
            }
            top.Disable();
        }
        inputLayers.Add(inputMap);
        inputLayers[^1].Enable();
    }

    public void Remove(InputActionMap inputMap)
    {
        // Removes an input map from the layers, disabling it.
        // The one below it gets enabled, unless there are enabled maps on top.
        bool blocked = false;
        for (int i = inputLayers.Count - 1; i >= 0; i--)
        {
            if (inputLayers[i] == inputMap)
            {
                inputLayers[i].Disable();
                if (i == 0)
                {
                    Debug.LogWarning("Removing the lowest input map. You might want to consider disabling it instead.");
                }
                else if (!blocked) // i > 0
                {
                    inputLayers[i - 1].Enable();
                }
                inputLayers.RemoveAt(i);
                return;
            }
            else if (inputLayers[i].enabled)
            {
                blocked = true;
            }
        }
        Debug.LogError("Cannot remove inexistent input map from the layers. It must be Push()ed first.");
    }

    public List<string> CurrentInputs()
    {
        // Returns the currently stacked input map names as strings.
        // Active inputs have an asterisk as the last symbol.
        var stackNames = new List<string>();
        foreach (var map in inputLayers)
        {
            stackNames.Add(map.name.ToString() + (map.enabled ? "*": ""));
        }
        return stackNames;
    }

    // Shorthands
    public static Inputs.PlayerActions Player { get { return Instance.inputs.Player; } }
    public static Inputs.DialogueActions Dialogue { get { return Instance.inputs.Dialogue; } }
}
