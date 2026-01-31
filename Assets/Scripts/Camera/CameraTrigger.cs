using UnityEngine;
using Cinemachine;

// Today is friday.. IN CALIFORNIA! *shoot*
// https://www.youtube.com/watch?v=9WaYCdQ8FOQ&ab_channel=LewisM
// I promise it's worth it :DD
// In all seriousness, add this script to a collider object in the scene to make it swap camera when hit
public class CameraTrigger : MonoBehaviour
{
    public CinemachineVirtualCamera targetCamera;
    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Tytti")) return;
        if (mainCam != null)
            mainCam.GetComponent<CameraController>().SetActiveCamera(targetCamera);
    }
}
