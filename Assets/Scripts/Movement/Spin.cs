using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class Spin : MonoBehaviour
{

    public float spinSpeed = 0f;

    private RectTransform trans;

    void Start()
    {
        trans = GetComponent<RectTransform>();
    }

    void Update()
    {
        trans.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }
}