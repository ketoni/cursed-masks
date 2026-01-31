using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Singleton<T> : Manager where T : MonoBehaviour
{
    // Static instance variable to hold the single instance of the class
    private static T _instance;

    // Property to access the singleton instance
    public static T Instance
    {
        get
        {
            // If there is no instance yet, find it in the scene
            if (_instance == null)
            {
                T[] instances = FindObjectsOfType<T>();

                // If there are multiple instances, log a warning and use the first one found
                if (instances.Length > 1)
                {
                    Debug.LogWarning($"Multiple instances of {typeof(T)}, using the first one found.");
                }
                _instance = instances.Length > 0 ? instances[0] : null;
            }
            return _instance;
        }
    }

}

public class Manager : MonoBehaviour
{
    protected virtual void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoad;
    }

    protected virtual void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoad;
    }

    private void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        var context = scene.FindInScene<SceneContext>();
        OnSceneLoad(context);
    }

    public virtual void OnSceneLoad(SceneContext sceneContext)
    {
        
    }
}
