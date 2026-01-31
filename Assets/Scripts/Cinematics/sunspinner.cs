using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sunspinner : MonoBehaviour
{
    public float rotationSpeed = 50f; // Speed of rotation

    void Update()
    {
        // Rotate the object around the X-axis
        transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
    }
}
