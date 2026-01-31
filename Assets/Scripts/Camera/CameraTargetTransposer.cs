using UnityEngine;

public class CameraTargetTransposer : MonoBehaviour
{
    // Makes the game object or "target" to latch to an Y position 
    // but follow its parent transform otherwise.
    private Transform anchor;
    private float fixedY;

    void Awake()
    {
        // Initially fix to our starting Y
        fixedY = transform.position.y;
        anchor = transform.parent;
    }

    public void ResetY()
    {
        fixedY = anchor.transform.position.y;
    }

    void Update()
    {
        transform.position = new Vector3(anchor.position.x, fixedY, anchor.position.z);
    }

}
