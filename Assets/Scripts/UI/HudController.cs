using UnityEngine;

public class HudController : MonoBehaviour
{

    [HideInInspector] public bool skipHiding;

    void Start()
    {
    #if UNITY_EDITOR
        if (skipHiding)
        {
            Debug.LogWarning("Skipping hiding any HUD panels");
            return;
        }
    #endif

    }

}
