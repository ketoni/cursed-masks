using UnityEngine;

public class SurfaceType : MonoBehaviour
{
    // Currently only used for determining which sound effects to play when walking on objects
    public TerrainLayer type;
    public GameObject walkEffect;
    public float effectOffset;
}
