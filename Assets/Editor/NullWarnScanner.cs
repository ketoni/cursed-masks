using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

[InitializeOnLoad]
public static class WarnIfNullScanner
{
    private static HashSet<Object> warnedObjects = new HashSet<Object>();

    static WarnIfNullScanner()
    {
        EditorApplication.delayCall += ScanScene;
        EditorApplication.hierarchyChanged += ScanScene;
    }

    private static void ScanScene()
    {
        warnedObjects.Clear();
        var allComponents = Object.FindObjectsOfType<MonoBehaviour>();

        foreach (var comp in allComponents)
        {
            if (comp == null) continue;

            var fields = comp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<NullWarn>();
                if (attr == null) continue;

                bool isSerialized = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
                if (!isSerialized) continue;

                object value = field.GetValue(comp);
                if (value == null && !warnedObjects.Contains(comp))
                {
                    Debug.LogWarning($"[{comp.GetType().Name}] `{field.Name}` is unassigned.", comp);
                    warnedObjects.Add(comp);
                }
            }
        }
    }
}