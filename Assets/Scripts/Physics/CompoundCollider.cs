using Unity.VisualScripting;
using UnityEngine;

public class CompoundCollider : MonoBehaviour
{

    public Collider[] Colliders { get; private set; }

    void Awake()
    {
        Colliders = GetComponentsInChildren<Collider>();
        if (Colliders.Length == 0)
        {
            Debug.LogWarning("CompoundCollider doesn't have any child colliders");
        }
    }    

    void OnEnable()
    {
        SetEnabled(true);
    }

    void OnDisable()
    {
        SetEnabled(false);
    }

    public void SetEnabled(bool enabled)
    {
        foreach (var collider in Colliders)
        {
            collider.enabled = enabled;
        }
    }

}
