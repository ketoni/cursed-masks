using UnityEngine;

public class Sway : MonoBehaviour
{
    public Vector3 swayAxis = Vector3.forward;
    public float swayAngle;
    public float swayFrequency = 1f;

    public Vector3 secondSwayAxis = Vector3.forward;
    public float secondSwayAngle;
    public float secondSwayFrequency = 0f;


    private Quaternion initialRotation;

    void Start()
    {
        swayAxis.Normalize();
        initialRotation = transform.rotation;
    }

    void Update()
    {
        // Makes the object sway over time around the set axis / axes
        float swayOffset = Mathf.Sin(Time.time * swayFrequency) * swayAngle;
        float secondSwayOffset = Mathf.Sin(Time.time * secondSwayFrequency) * secondSwayAngle;
        transform.rotation =
            initialRotation
            * Quaternion.AngleAxis(swayOffset, swayAxis)
            * Quaternion.AngleAxis(secondSwayOffset, secondSwayAxis);
    }
}