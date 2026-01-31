using UnityEngine;

public class PersistentObject : MonoBehaviour
{
    void Awake()
    {
        transform.SetParent(null); // DDOL requires root level
        DontDestroyOnLoad(gameObject);
    }
}