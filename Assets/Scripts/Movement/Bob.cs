using UnityEngine;

public class Bob : MonoBehaviour
{
    public Vector3 bobAxis = Vector3.up;
    public float frequency = 1f;
    public float amount = 1f;
    private Vector3 localOffset;

    void Start()
    {
        bobAxis.Normalize();
        localOffset = transform.localPosition; // Store the initial local position
    }

    void Update()
    {
        // Calculate the bobbing offset
        float t = Mathf.Sin(Time.time * frequency);
        Vector3 bobbingOffset = amount * t * bobAxis;

        // Update the local position to follow the parent while bobbing
        transform.localPosition = localOffset + bobbingOffset;
    }
}