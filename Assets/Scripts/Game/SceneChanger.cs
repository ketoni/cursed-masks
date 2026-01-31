using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void LoadNextScene()
    {
        // Get the current active scene
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        // Load the next scene by adding 1 to the current scene index
        SceneManager.LoadScene(currentSceneIndex + 1);
    }
}
