using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Interactable))]
public class InteractableEventsEditor : Editor 
{
    // Lists event names under the Interactable
    // and buttons for each for immediate Execute() calls
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GameManager.McObject == null)
        {
            return; // We are probably outside playmode
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);

        var interactable = (Interactable)target;
        if (!interactable.HasEvents)
        {
            EditorGUILayout.LabelField("None");
            return;
        }

        // Create a context for testing
        var dummyContext = new InteractionContext()
        {
            interactor = GameManager.McObject.GetComponentInChildren<Interactor>(), 
            interactable = interactable,
        };

        foreach (var evt in interactable.Events)
        {
            EditorGUILayout.BeginHorizontal();

            // Show a button to execute the event but have it disabled outside playmode
            GUI.enabled = EditorApplication.isPlaying;
            if (GUILayout.Button("Execute", GUILayout.Width(80)))
            {
                evt.Execute(
                    dummyContext,
                    DebugManager.Instance.eventExecuteArgument,
                    validate: false);
                Debug.Log($"Editor executed {evt}'");
            }
            GUI.enabled = true;

            EditorGUILayout.LabelField(evt.ToString());
            EditorGUILayout.EndHorizontal();
        }

        // Yarn debugging
        EditorGUILayout.LabelField("Main Character Interaction", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        var node = EditorGUILayout.TextField("Node", interactable.yarnNodeName, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Interact", GUILayout.Width(80)))
        {
            interactable.Interact(dummyContext.interactor, node, force: true);
        }
        EditorGUILayout.EndHorizontal();

    }
}