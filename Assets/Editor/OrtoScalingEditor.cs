using UnityEngine;
using UnityEditor;

// Adds a button to the CameraController for immediate scaling
[CustomEditor(typeof(OrtographicScaling))]
public class OrtoScalingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var script = (OrtographicScaling)target;
        if (GUILayout.Button("Scale Now", GUILayout.Width(100)))
        {
            UIManager.Instance.SetOrtographicScaling((OrtographicScaling)target);
        }
    }
}
