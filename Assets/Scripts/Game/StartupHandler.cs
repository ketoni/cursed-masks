using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupHandler : MonoBehaviour
{
    [Header("Editor Only")]
    public bool loadInitialScene;
    public bool noPlayerControl;
    private int initialSceneIndex;

    void Awake()
    {
        initialSceneIndex = gameObject.scene.buildIndex + 1;
        if (loadInitialScene)
        {
            Debug.Log($"StartupHandler is loading scene {initialSceneIndex}");
        }

        #if UNITY_EDITOR
        // While developing, we are loading arbitrary scenes but don't want to explicitly call SceneManager.
        // This is to avoid scripts starting twice unnecessarily due to the unload of the same scene. 
        // We want to trigger scene load logic though, so we do it now. Assuming will be only called when bootstrapping. 
        var context = FindObjectOfType<SceneContext>();
        if (SceneManager.sceneCount > 1)
        {
            SimulateSceneLoad(context);
        }
        #else
        loadInitialScene = true;
        #endif

        #if DEVELOPMENT_BUILD
        // Ensure our debug manager is enabled in debug builds
        DebugManager.Instance.enabled = true;
        #endif

        if (loadInitialScene)
        {
            SceneManager.LoadScene(initialSceneIndex);
        }
    }

    void Start()
    {
    }

    void SimulateSceneLoad(SceneContext context)
    {
        // Simulates a scene load for all Managers by providing them the provided context
        if (context == null)
        {
            throw new NullReferenceException("Simulated Scene load has no context!");
        }
        foreach (var manager in FindObjectsOfType<Manager>())
        {
            manager.OnSceneLoad(context);
        }
    }
}
