using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yarn.Unity;
using FMOD.Studio;
using static DialogueEffects;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public static class Extensions
{
    public static List<T> GetComponentsInFirstChildren<T>(this GameObject obj)
    {
        // Same as GetComponentsInChildren but only looks at the immediate children of the GameObject
        var components = new List<T>();
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            T comp = obj.transform.GetChild(i).gameObject.GetComponent<T>();
            if (comp != null)
            {
                components.Add(comp);
            }
        }
        return components;
    }

    public static string Capitalized(this string str)
    {
        // Returns the string with the first alphabetic character capitalized
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }
        char[] charArray = str.ToCharArray();
        for (int i = 0; i < charArray.Length; i++)
        {
            if (char.IsLetter(charArray[i]))
            {
                charArray[i] = char.ToUpper(charArray[i]);
                break;
            }
        }
        return new string(charArray);
    }

    private static Dictionary<string, string> ParseLocalizedLine(LocalizedLine line)
    {
        // Parses metadata/tags from the line, respecting two different formats:
        // #tag
        // #tag:value
        // and returns a dictionary with tags as keys.
        // Tags without a value will have null as the value in the dictionary
        var tags = new Dictionary<string, string>();
        if (line.Metadata == null)
        {
            return tags;
        }
        foreach (var meta in line.Metadata)
        {
            var parts = meta.Split(":");
            var key = parts[0];
            string value = null;
            if (parts.Length > 1)
            {
                if (key == "")
                {
                    Debug.LogError($"Empty meta tag!");
                    continue;
                }
                value = parts[1]; 
            }
            if (tags.ContainsKey(key))
            {
                Debug.LogWarning($"Duplicate tag '{key}'!");
            }
            tags[key] = value;
        }
        return tags;
    } 

    public static Dictionary<string, string> ParseTags(this LocalizedLine line)
    {
        // Parses given LocalizedLine of its metadata tags, dropping any we aren't expecting
        var tags = ParseLocalizedLine(line);
        var droppedKeys = new List<string>();
        foreach (var (tag, _) in tags)
        {
            if (!DialogueView.knownTagsDefaults.Keys.Contains(tag) && tag != "lastline")
            {
                Debug.LogWarning($"Ignoring uknown dialogue tag '{tag}'");
                droppedKeys.Add(tag);
            }
        }
        foreach (var key in droppedKeys)
        {
            tags.Remove(key);
        }
        return tags;
    }

    public static bool ParseTagValue(this LocalizedLine line, string tagName, out string value)
    {
        // Tries to return the value of the given tag from a LocalizedLine via `value` and return true.
        // If the tag is not present we set a default value instead and return false.
        var defaultsDict = DialogueView.knownTagsDefaults;
        if (!defaultsDict.Keys.Contains(tagName))
        {
            throw new KeyNotFoundException($"DialogueView does not provide default value for '{tagName}'");
        }
        var tags = line.ParseTags();
        if (tags.Keys.Contains(tagName))
        {
            value = tags[tagName];
            return true;
        }
        value = defaultsDict[tagName];
        return false;
    }

    public static Dictionary<string, List<TextEffect>> AttributesToEffects(this LocalizedLine line)
    {
        Dictionary<string, List<TextEffect>> effects = new();

        var attributes = line.TextWithoutCharacterName.Attributes;
        attributes.Sort((a, b) => b.Position.CompareTo(a.Position));

        foreach (var attribute in line.TextWithoutCharacterName.Attributes)
        {
            var effect = TextEffect.FromMarkup(attribute);
            if (!effects.ContainsKey(effect.Name))
            {
                effects[effect.Name] = new();
            }
            effects[effect.Name].Add(effect);
        }
        return effects;
    }

    public static Dictionary<string, string> ParseTags(this DialogueOption option)
    {
        var tags = ParseLocalizedLine(option.Line);
        /*
        foreach (var kvp in tags.ToList())
        {
            var tag = kvp.Key;
            if (!ExpressionView.knownTags.Contains(tag))
            {
                Debug.LogWarning($"Ignoring uknown option tag '{tag}'");
                tags.Remove(tag);
            }
        }
        */
        return tags;
    }

    public static Vector3 WorldPos(this TreeInstance tree)
    {
        // Returns the world position of a terrain tree using the TerrainSampler
        var terrain = TerrainSampler.Instance.Terrain;
        return Vector3.Scale(tree.position, terrain.terrainData.size) + terrain.transform.position;
    }

    public static bool IsOnLayers(this GameObject obj, LayerMask otherLayers)
    {
        // Returns whether the GO is on any of the given layers
        return ((1 << obj.layer) & otherLayers) != 0;
    }

    public static Vector3 WithX(this Vector3 vec, float value)
    {
        return new Vector3(value, vec.y, vec.z);
    }

    public static Vector3 WithY(this Vector3 vec, float value)
    {
        return new Vector3(vec.x, value, vec.z);
    }

    public static Vector3 WithZ(this Vector3 vec, float value)
    {
        return new Vector3(value, vec.y, value);
    }

    public static bool HasParameter(this Animator animator, string parameterName)
    {
        foreach (var param in animator.parameters)
        {
            if (param.name == parameterName)
            {
                return true;
            }
        }
        return false;
    }

    public static T FindInScene<T>(this Scene scene) where T : Component
    {
        foreach (var rootObj in scene.GetRootGameObjects())
        {
            var component = rootObj.GetComponentInChildren<T>(true); // include inactive
            if (component != null)
                return component;
        }
        return null;
    }
}
