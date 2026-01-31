using UnityEngine;
using UnityEditor;

// Adds a button to the CameraController for immediate scaling
[CustomEditor(typeof(CameraController))]
public class CameraControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var script= (CameraController)target;
        /*
        if (GUILayout.Button("Reset pose"))
        {
            script.Reset();
        }
        */
        if (GUILayout.Button("Scale Everything Now"))
        {
            script.FindScaleAll();
        }
    }
}
