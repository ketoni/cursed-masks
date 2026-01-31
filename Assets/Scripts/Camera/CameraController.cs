using System;
using Cinemachine;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Yarn.Unity;

[RequireComponent(typeof(Camera), typeof(CinemachineBrain))]
[RequireComponent(typeof(UniversalAdditionalCameraData))]
public class CameraController : MonoBehaviour
{
    private Transform parentObject;

    [Header("CMCams")]
    [InspectorReadOnly] public CinemachineVirtualCameraBase mainCharacterCam;

    public class CameraPriority
    {
        public const int Disabled = 0;
        public const int Low = 10;
        public const int Main = 20;
        public const int High = 30;
    }


    CinemachineBrain Brain => GetComponent<CinemachineBrain>();

    UniversalAdditionalCameraData camData;
    public CinemachineVirtualCameraBase ActiveCam => Brain.ActiveVirtualCamera as CinemachineVirtualCameraBase;
    private Transform stashedFollowTarget;
    private Transform DefaultFollowTarget =>
        GameManager.McObject.GetComponentInChildren<CameraTargetTransposer>().transform;

    void Awake()
    {
        camData = GetComponent<UniversalAdditionalCameraData>();
    }

    public Sequence Vorbit(float amount, float duration, float stepAngle)
    {
        throw new NotImplementedException("#Not implemented since new scene loading system");
        // Vertically orbits the camera over X-axis around the lookTarget over the given amount and duration.
        // The rotation is done in a choppy way to reduce aliasing. `stepAngle` controls the choppiness.
        /*
        float startAngle = Vector3.Angle(Vector3.forward, transform.forward);
        float endAngle = startAngle + amount;

        // Calculate the number of steps and their duration 
        int numSteps = Mathf.CeilToInt(Mathf.Abs(amount) / stepAngle);
        float stepDuration = duration / numSteps;

        Sequence sequence = DOTween.Sequence();
        sequence.AppendCallback(() => StartTween());
        for (int step = 0; step <= numSteps; step++)
        {
            // Calculate the normalized time for this step (0 to 1) and apply the easing function to get the eased time
            // to then calculate the angle for this step.
            float time = (float)step / numSteps;
            float easedTime = DOVirtual.EasedValue(0, 1, time, Ease.InOutQuad);
            float currentStepRads = Mathf.Lerp(startAngle, endAngle, easedTime) * Mathf.Deg2Rad;

            // Add a tween to move to the next position, waiting the step duration in between 
            Vector3 nextPosition = new(
                transform.position.x,
                GetTargetPosition().y + Mathf.Sin(currentStepRads) * targetDistance,
                GetTargetPosition().z - Mathf.Cos(currentStepRads) * targetDistance
            );
            sequence.Append(transform.DOMove(nextPosition, 0));
            sequence.AppendInterval(stepDuration);
        }

        // Ensure we really end up on the angle we aimed for when sequence ends
        sequence.AppendCallback(() =>
        {
            var endRotation = transform.eulerAngles;
            endRotation.x = endAngle;
            transform.eulerAngles = endRotation;
            // TODO if we have this here, battle camera tricks break
            // So we call it manually there. No time to fix it now, too bad!
            //EndTween();
        });

        return sequence;
        */
    }

    void Update()
    {
        if (mainCharacterCam && mainCharacterCam.Priority != CameraPriority.Main)
        {
            Debug.LogWarning($"MC Camera priorty should be always {CameraPriority.Main}!", mainCharacterCam);
        }
    }

    void LateUpdate()
    {
        // Make sure our effect volumes consider our follow target as the trigger.
        // We don't do this if we don't have MC camera in the scene though,
        // because then we can fall back to cameras being the triggers. 
        if (mainCharacterCam && ActiveCam != null)
        {
            // If we are not following anything (for some effect),
            // we assume our trigger should be the default trigger target 
            camData.volumeTrigger = ActiveCam.Follow == null
                ? DefaultFollowTarget
                : ActiveCam.Follow;
            if (camData.volumeTrigger == null)
            {
                throw new NullReferenceException("camera volume trigger is broken");
            }
        }
    }

    public string CameraTargetName => camData.volumeTrigger == null ? null : camData.volumeTrigger.name;

    public void FindScaleAll()
    {
        // Finds all objects which want their height to be scaled based on the camera angle and scales them. 
        foreach (var obj in FindObjectsOfType<OrtographicScaling>(includeInactive: true)) Scale(obj);
    }

    public void Scale(OrtographicScaling target)
    {
        target.ScaleByProjectionAngle(transform.root.eulerAngles.x);
    }

    // Resets the current camera to its normal pose 
    public void Reset()
    {
        if (ActiveCam.Follow == null && stashedFollowTarget)
        {
            // Return back to our follow target
            ActiveCam.Follow = stashedFollowTarget;
            stashedFollowTarget = null;
        }
    }

    public void SetActiveCamera(CinemachineVirtualCameraBase camera, int priority = CameraPriority.High)
    {
        if (camera == null)
        {
            Debug.LogError("Won't change to null camera");
            return;
        }
        if (camera == ActiveCam) return;
        if (ActiveCam != mainCharacterCam)
        {
            ActiveCam.Priority = CameraPriority.Disabled; 
        }
        if (camera != mainCharacterCam)
        {
            camera.Priority = priority;
        }
    }

    [YarnCommand("ResetCamera")]
    public static void ReturnToMCCamera()
    {
        CutsceneManager.Cam.SetActiveCamera(CutsceneManager.Cam.mainCharacterCam);
    }

    // Whether to leave the current camera "floating" and not following the camera target
    // It's important to call this for any such effects so that our FX volume
    // triggering still works as intended
    internal void SetFollow(bool follow)
    {
        if (follow)
        {
            if (stashedFollowTarget == null)
            {
                throw new NullReferenceException("Can't set camera follow, no stashed target. Someone is doing something silly!");
            }
            ActiveCam.Follow = stashedFollowTarget;
            stashedFollowTarget = null;
        }
        else
        {
            if (ActiveCam.Follow == null)
            {
                Debug.LogError("Current camera is already not following!");
                return;
            }
            stashedFollowTarget = ActiveCam.Follow;
            ActiveCam.Follow = null;
        }
    }

    // Detaches camera from the main character
    public void Detach()
    {
        parentObject = transform.parent;
        transform.parent = null;
    }

}
