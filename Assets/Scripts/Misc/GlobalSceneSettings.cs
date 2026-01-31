using UnityEngine;

public class GlobalSceneSettings : MonoBehaviour
{
    public Vector3 _LightAttenuationFactors = new Vector3(0, 1, 2);

    // Called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        UpdateShaderGlobals();
    }

    void OnValidate()
    {
        UpdateShaderGlobals();
    }

    private void UpdateShaderGlobals()
    {
        Shader.SetGlobalVector(
            "_TFLightAttenuationFactors",
            new Vector4(
                _LightAttenuationFactors.x,
                _LightAttenuationFactors.y,
                _LightAttenuationFactors.z,
                0)
            );
    }
}
