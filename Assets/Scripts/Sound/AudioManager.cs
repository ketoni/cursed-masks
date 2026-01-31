using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using FMOD.Studio;
using Yarn.Unity;

public class AudioManager : Singleton<AudioManager> 
{
    [SerializeField] GameObject treeEmitters;
    [SerializeField] GameObject treeEmitterPrefab;
    [SerializeField] float treeSearchRadius; // Radius for tree emitter placement 
    [SerializeField] float minimumEmitterDistance; // Minimum distance between mass placed emitters

    [SerializeField] GameObject sceneMusic;

    void Start()
    {
        // If we don't have any emitters initially, place them
        if (treeEmitters != null && treeEmitters.transform.childCount == 0)
        {
            PlaceEmittersAtTreePositions();
        }

        // Check for missing walk sounds
        if (TerrainSampler.Instance != null)
        {
            CheckTextureWalkSounds();
        }
    }

    public void PlaySound(
        string eventPath,
        float volume = 1.0f,
        string parameterName = default,
        string parameterLabel = default,
        float parameterValue = default,
        Vector3 position = default)
    {
        // A common interface to play sounds via FMOD
        try
        {
            var instance = CreateEventInstance(eventPath);
            instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
            instance.setVolume(volume);
            if (parameterName != default)
            {
                if(parameterLabel != default)
                {
                    instance.setParameterByNameWithLabel(parameterName, parameterLabel);
                }
                else
                {
                    instance.SetParameter(parameterName, parameterValue);

                }
            }
            instance.start();
            instance.release();
        }
        catch (FMODUnity.EventNotFoundException)
        {
            Debug.LogWarning("[FMOD] Event not found: " + eventPath);
        }
    }


    public EventInstance CreateEventInstance(string eventPath)
    {
        var guid = FMODUnity.RuntimeManager.PathToGUID(eventPath);
        return FMODUnity.RuntimeManager.CreateInstance(guid);
    }


    public void ClearEmitters()
    {
        // Clear existing emitters under the parent object
        while (treeEmitters.transform.childCount != 0)
        {
            DestroyImmediate(treeEmitters.transform.GetChild(0).gameObject);
        }
    }

    public void PlaceEmittersAtTreePositions()
    {
        // Places terrain tree sound emitters inside the specified radius distance,
        // keeping the emitter density under a threshold based on the minimum dinstance variable.

        // This might take some time so we log keep count and log it at the end
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        ClearEmitters();
        List<GameObject> placedEmitters = new();

        // Iterate over all terrain trees randomly
        foreach (var tree in TerrainSampler.Instance.TreesRandom)
        {
            var treeWorldPos = tree.WorldPos(); 

            // Calculate tree distance in just the XZ plane
            // and exclude trees which are beyond emitter placement radius
            Vector2 treeXZ = new(treeWorldPos.x, treeWorldPos.z);
            Vector2 centerXZ = new(transform.position.x, transform.position.z);
            if (Vector2.Distance(treeXZ, centerXZ) > treeSearchRadius)
            {
                continue;
            }

            // Check if this position is far enough from all placed emitters
            bool tooClose = false;
            foreach (GameObject placedEmitter in placedEmitters)
            {
                Vector2 emitterXZ = new(placedEmitter.transform.position.x, placedEmitter.transform.position.z);
                if (Vector2.Distance(treeXZ, emitterXZ) < minimumEmitterDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose)
            {
                continue;
            }

            // Instantiate an emitter at tree world position and place it under the parent container
            var emitter = Instantiate(treeEmitterPrefab, treeWorldPos, Quaternion.identity);
            emitter.transform.SetParent(treeEmitters.transform);
            emitter.name = "TreeEmitter_" + placedEmitters.Count;
            placedEmitters.Add(emitter);
        }

        stopwatch.Stop();
        Debug.Log($"Placed {placedEmitters.Count} emitters at tree positions in {stopwatch.ElapsedMilliseconds} ms.");
    }

    // Draw the radius as a circle in the Scene view (XZ plane)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        CircleDrawer.DrawCircle(transform.position, treeSearchRadius, segments: 128);
    }


    void CheckTextureWalkSounds()
    {
        // Checks all terrain textures in use. If we do not define a walk sound for one, log a warning.
        foreach (var name in TerrainSampler.Instance.GetTextureNames())
        {
            if (!GetParameterValue(typeof(Sounds.Walking), name, out _))
            {
                Debug.LogWarning($"We are using terrain texture '{name}' but it doesn't have a walking sound");
            }
        }
    }

    public bool GetParameterValue(Type soundType, string propertyName, out int value)
    {
        // A way to get a sound path from the Sounds namespace by type and name.
        // Returns null if not defined.
        value = default;
        var property = soundType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
        if (property != null && property.GetValue(null) is int intValue)
        {
            value = intValue;
            return true;   
        }
        return false;
    }

    public void PlaySceneMusic(bool shouldPLay)
    {
        if(shouldPLay == false)
        {
            sceneMusic.SetActive(false);
        }
        if (shouldPLay == true)
        {
            sceneMusic.SetActive(true);
        }
    }

    [YarnCommand("StartMusic")]
    public static void StartShoreMusic()
    {
        Instance.PlaySceneMusic(true);
    }

    [YarnCommand("StopMusic")]
    public static void StopShoreMusic()
    {
        Instance.PlaySceneMusic(false);
    }

    [YarnCommand("PlaySound")]
    public static void YarnPlaySound(string eventPath, string parameterName = null, string parameterLabel = default)
    {
        Instance.PlaySound(eventPath, volume: 1, parameterName, parameterLabel);
    }

    [YarnCommand("PlaySound3D")]
    public static void YarnPlaySoundSpatial(string eventPath)
    {
        var emitter = DialogueManager.Instance.Context.interactable;
        Instance.PlaySound(eventPath, position: emitter.transform.position);
    }

    internal void MuteAll()
    {
        FMODUnity.RuntimeManager.GetBus("bus:/").setVolume(0);
    }
}


static class AudioExtensions
{
    public static bool HasParameter(this EventInstance instance, string parameterName)
    {
        // Returns whether the given FMOD instance supports the given parameter.
        // A nice to have since, for some reason, setting an inexistent parameter
        // returns ERR_EVENT_NOTFOUND which is misleading?
        instance.getDescription(out EventDescription description);
        description.getParameterDescriptionCount(out int parameterCount);
        for (int i = 0; i < parameterCount; i++)
        {
            description.getParameterDescriptionByIndex(i, out PARAMETER_DESCRIPTION parameter);
            if (parameter.name == parameterName)
            {
                return true;
            }
        }
        return false;
    }
    public static void SetParameter(this EventInstance instance, string parameterName, float parameterValue)
    {
        if (instance.HasParameter(parameterName))
        {
            var result = instance.setParameterByName(parameterName, parameterValue);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogWarning($"[FMOD] Failed to set parameter '{parameterName}' by name ({result})");
            }
        }
        else
        {
            Debug.LogWarning($"[FMOD] Instance {instance} does not have a parameter '{parameterName}'");
        }
    }
}
