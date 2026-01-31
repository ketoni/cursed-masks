using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Strafe : MonoBehaviour
{
    public float magnitude;
    public Vector3 direction;
    
    void Update()
    {
        gameObject.transform.position += direction.normalized * magnitude / 1000f;
    }
}
