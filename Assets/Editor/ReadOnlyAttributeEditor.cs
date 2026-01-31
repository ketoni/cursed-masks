using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(InspectorReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUIContent newLabel = new(label.text + " (runtime)");

        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, newLabel, true);
        GUI.enabled = true;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

}
