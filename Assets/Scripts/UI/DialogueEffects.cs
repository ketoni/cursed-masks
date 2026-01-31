using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Yarn.Markup;

public class DialogueEffects
{
    // Methods to apply visual effects to a TMPro text container,
    // which are designed for a dialogue text view.

    public bool shouldUpdateCache = true;

    TMP_MeshInfo[] cachedMeshInfo;
    TMP_Text textContainer;

    float zeroTime = -1;
    float previousTime;

    readonly float rand = (Random.value * 2) - 1;

    public DialogueEffects(TMP_Text container)
    {
        textContainer = container;
    }

    public void Apply(Dictionary<string, List<TextEffect>> effectsDict)
    {
        // Applies given effects to the current contents of our text container.
        // The effects dict has effect names as keys and lists of effects as values.
        // The latter tells us how strong or fast the effect should be on a speficied part of the line(s).
        // We need to iterate through the actual vertexes of the mesh the text is rendered on to apply the effects.

        var textInfo = textContainer.textInfo;
        int symbolCount = textInfo.characterCount;
        if (symbolCount == 0)
        {
            // Nothing to do if there is no text
            return;
        }

        // Cache new vertex data if needed 
        if (shouldUpdateCache)
        {
            cachedMeshInfo = textContainer.textInfo.CopyMeshInfoVertexData();
            shouldUpdateCache = false;
        }

        // Keep time
        float time = Time.time;
        if (zeroTime < 0)
        {
            zeroTime = 0;
        }
        else
        {
            zeroTime += time - previousTime;
        }
        previousTime = time;

        // Prepare a copy of the cached colors for each mesh
        Color32[][] meshVertexColors = new Color32[textInfo.meshInfo.Length][];
        for (int i = 0; i < meshVertexColors.Length; i++)
        {
            meshVertexColors[i] = new Color32[cachedMeshInfo[i].colors32.Length];
            cachedMeshInfo[i].colors32.CopyTo(meshVertexColors[i], 0);
        }

        // Iterate through each symbol in the text.
        for (int i = 0; i < symbolCount; i++)
        {
            float rand_i = rand + i;

            var symbolInfo = textInfo.characterInfo[i];
            if (!symbolInfo.isVisible)
            {
                // Invisible symbols have no geometry to manipulate.
                continue;
            }

            // Find the vertices we want to manipulate 
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
            Vector3[] cachedVertices = cachedMeshInfo[materialIndex].vertices;

            // Find the index of the vertex quad of this symbol
            // and apply any effects that are applicable on the symbol's index
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            foreach (var (effectName, effects) in effectsDict)
            {
                foreach (var effect in effects)
                {
                    if (!effect.IsOnIndex(i))
                    {
                        // The effects simply have a starting index (in the text) and a length
                        // so if the effect doesn't span this symbol, we do nothing to it.
                        continue;
                    }
                    // Effects should be based on the cached vertices if they are cumulative over time.
                    // Some of the scalar values are here to provide a sensible "baseline" effect with
                    // strengths, speeds, etc. of 1.0.
                    if (effectName == "wave")
                    {
                        // _.~^~._.~^
                        var offset = Mathf.Sin(time * effect.Speed * 5f - i) * effect.Strength * 3f;
                        for (int j = 0; j < 4; j++)
                        {
                            vertices[vertexIndex + j].y = cachedVertices[vertexIndex + j].y + offset;
                        }
                    }
                    else if (effectName == "shake")
                    {
                        // Just shake randomly brrrrr
                        var amount = effect.Strength * 1.25f;
                        float offsetX = Random.Range(-amount, amount);
                        float offsetY = Random.Range(-amount, amount);
                        for (int j = 0; j < 4; j++)
                        {
                            vertices[vertexIndex + j].x = cachedVertices[vertexIndex + j].x + offsetX;
                            vertices[vertexIndex + j].y = cachedVertices[vertexIndex + j].y + offsetY;
                        }
                    }
                    else if (effectName == "explode")
                    {
                        // Strength controls angle from 0 to 360 deg
                        float halfSpread = Mathf.Lerp(0f, 2 * Mathf.PI, effect.Strength) * 0.5f;

                        // Generate a random speed and direction based on the index
                        float speedVariance = 0.8f + (0.4f * (Mathf.Sin(rand_i) + 1) / 2); // From 0.8 to 1.2
                        float randomSpreadAngle = effect.Angle + (Mathf.Sin(rand_i) * halfSpread);
                        Vector3 direction = new(Mathf.Cos(randomSpreadAngle), Mathf.Sin(randomSpreadAngle), 0);

                        float fadeProgress = Mathf.Clamp01(1 - (zeroTime / 1f));
                        byte targetAlpha = (byte)(fadeProgress * 255);

                        // Move each vertex of the letter over time 
                        Vector3 offset = effect.Speed * speedVariance * zeroTime * direction.normalized;
                        for (int j = 0; j < 4; j++)
                        {
                            vertices[vertexIndex + j] = cachedVertices[vertexIndex + j] + offset * 500;
                            Color32 originalColor = cachedMeshInfo[materialIndex].colors32[vertexIndex + j];
                            meshVertexColors[materialIndex][vertexIndex + j] = new Color32(originalColor.r, originalColor.g, originalColor.b, targetAlpha);
                        }
                    }
                }
            }
        }

        // Push mesh changes into the container
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].colors32 = meshVertexColors[i];
            textContainer.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
        }
    }

    public static IEnumerator SingleEffectRoutine(TMP_Text target, TextEffect effect, float duration)
    {
        // Check if we want to span the whole target
        if (effect.Length == -1)
        {
            effect.Span(target);
        }
        // Build effects dict
        var dict = new Dictionary<string, List<TextEffect>>()
        {
            [effect.Name] = new List<TextEffect>() { effect }
        };

        // Run routine
        var effects = new DialogueEffects(target);
        float timer = 0;
        while (timer < duration)
        {
            effects.Apply(dict);
            yield return null;
            timer += Time.deltaTime;
        }
    }

    public static IEnumerator SingleEffectRoutine(TMP_Text target, string name, float duration)
    {
        return SingleEffectRoutine(target, new TextEffect(name), duration);
    }

    public class TextEffect
    {
        public string Name { get; protected set; }
        public int StartIndex { get; protected set; }
        public int Length { get; protected set;}
        public float Strength { get; protected set; }
        public float Speed { get; protected set; }
        public float Angle { get; protected set; }

        public TextEffect(
            string name,
            int startIndex = 0,
            int length = -1,
            float strength = 1f,
            float speed = 1f,
            float angle = 0f)
        {
            Name = name;
            StartIndex = startIndex;
            Length = length;
            Strength = strength;
            Speed = speed;
            Angle = angle * Mathf.Deg2Rad;
        }

        internal bool IsOnIndex(int index)
        {
            // Does given index span this effect?
            return StartIndex <= index && index < StartIndex + Length;
        }

        public void Span(TMP_Text target)
        {
            // Makes the effect span the whole given text
            Length = target.text.Length;
        }

        public static TextEffect FromMarkup(MarkupAttribute attribute)
        {
            var effect = new TextEffect(
                name: attribute.Name.ToLower(),
                startIndex: attribute.Position,
                length: attribute.Length
            );

            foreach (var kvp in attribute.Properties)
            {
                // Loop through all the attribute's properties and see if we can
                // replace this instance's values with any using reflection
                var propertyName = kvp.Key.ToLower().Capitalized();
                var property = typeof(TextEffect).GetProperty(propertyName);
                if (property == null)
                {
                    Debug.LogWarning($"Ignoring unsupported property '{propertyName}' for attribute '{effect.Name}'");
                    continue;
                } 
                var markupValue = kvp.Value;
                switch (markupValue.Type)
                {
                    case MarkupValueType.Float:
                        property.SetValue(effect, markupValue.FloatValue);
                        break;
                    case MarkupValueType.Integer:
                        property.SetValue(effect, (float)markupValue.IntegerValue);
                        break;
                    default:
                        Debug.LogWarning($"Attribute property values should be of type Float");
                        continue;
                }
            }

            return effect;
        }
    }
}
