using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(NullWarn))]
public class WarnIfNullDrawer : PropertyDrawer
{
    private Texture2D _icon;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool warn = property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null;
        if (_icon == null)
            _icon = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;

        // Draw a warning symbol next to unassigned references
        Rect fieldRect = position;
        Rect iconRect = new(position.x + position.width - 16, position.y + 1, 16, 16);
        if (warn)
        {
            fieldRect = new(position.x, position.y, position.width - 18, position.height);
        }
        EditorGUI.PropertyField(fieldRect, property, label);
        if (warn)
        {
            GUI.DrawTexture(iconRect, _icon);
        }
    }

}