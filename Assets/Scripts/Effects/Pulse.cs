using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Pulse : MonoBehaviour
{
    public float frequency = 1f;
    public float min = 0f;

    private float max;
    private SpriteRenderer sprite;
    // Start is called before the first frame update
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        max = sprite.color.a; 
    }

    // Update is called once per frame
    void Update()
    {
        float t = (Mathf.Sin(Time.time * frequency) + 1f) / 2f;
        var color = sprite.color;
        color.a = Mathf.Lerp(min, max, t);
        sprite.color = color;
    }
}
