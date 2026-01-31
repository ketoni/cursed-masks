using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using Yarn.Unity;

public class SceneContext : MonoBehaviour
{
    public string sceneName;
    [NullWarn] public GameObject mainCharacterObject;
    [NullWarn] public YarnProject yarnProject;
    public string firstYarnNode;
    public CinemachineVirtualCameraBase mainCamera;
    public GameObject masksContainer;
    public Light sunDirectionalLight;
}