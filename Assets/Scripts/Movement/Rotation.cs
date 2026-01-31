using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotation : MonoBehaviour
{
    public Quaternion endRotation;
    public float rotationTime;

    float elapsedTime = 0;
    Quaternion startingRotation;

    void Start()
    {
        startingRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (elapsedTime < rotationTime)
        {
            transform.rotation = Quaternion.Slerp(startingRotation, endRotation, elapsedTime / rotationTime);
            Debug.Log(transform.rotation);
        }
        elapsedTime += Time.deltaTime;
    }

}
