using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class SplineFollower : MonoBehaviour
{
    [SerializeField] Transform followedObject;
    SplineContainer splineContainer;

    void Awake()
    {
        splineContainer = GetComponentInParent<SplineContainer>();
        if (splineContainer == null)
        {
            Debug.LogError("Didn't find a spline from parent!");
        }
    }

    readonly int resolution = 10;
    readonly int iterations = 2;

    void Update()
    {
        // Constantly set our position to be a point from the spline that's closest to the followed object
        var followedLocalPosition = splineContainer.transform.InverseTransformPoint(followedObject.position);
        SplineUtility.GetNearestPoint(
            splineContainer.Spline,
            followedLocalPosition,
            out float3 nearestPoint,
            out _,
            resolution,
            iterations);
        transform.position = splineContainer.transform.TransformPoint(nearestPoint);
    }
}
