using UnityEngine;


// Skibidi chibidi scripiti i'm tired xD
public class StayInFrontOfCamera : MonoBehaviour
{
    [Header("Follow Settings")]
    public bool followCam = false;
    [Min(0f)] public float distanceFromCam = 5.8f;

    [Tooltip("Optional lateral/vertical offset in the camera's local space.")]
    public Vector3 localOffset = Vector3.zero;

    [Tooltip("Leave null to use Camera.main each frame.")]
    public Transform cameraTransform;

    void LateUpdate()
    {
        if (!followCam) return;

        // Resolve which camera to follow
        Transform cam = cameraTransform != null
            ? cameraTransform
            : (Camera.main != null ? Camera.main.transform : null);

        if (cam == null) return; // No camera found

        // Position directly in front of the camera, respecting rotation in real time
        Vector3 targetPos = cam.position + cam.forward * distanceFromCam + cam.TransformVector(localOffset);
        transform.position = targetPos;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Transform cam = cameraTransform != null
            ? cameraTransform
            : (Camera.main != null ? Camera.main.transform : null);

        if (cam == null) return;

        Gizmos.color = Color.cyan;
        Vector3 p = cam.position + cam.forward * distanceFromCam + cam.TransformVector(localOffset);
        Gizmos.DrawLine(cam.position, p);
        Gizmos.DrawSphere(p, 0.05f);
    }
#endif
}
