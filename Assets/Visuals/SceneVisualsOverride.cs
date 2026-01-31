using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("Scene/Scene Visuals Override")]
public class SceneVisualsOverride : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter sunIntensity = new(1, 0, 100);
    public Vector3Parameter sunOrientation = new(new Vector3(0, 180, 0));
    public Vector3Parameter posterizationRGB = new(new Vector3(100, 100, 100));
    public ClampedFloatParameter lanternBrightness = new(1, 0, 1);
    public BoolParameter lanternNightMode = new(false);

    public bool IsActive() => true;
    public bool IsTileCompatible() => true;
}
