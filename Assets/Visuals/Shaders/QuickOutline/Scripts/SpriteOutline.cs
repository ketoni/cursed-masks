using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SpriteOutline : MonoBehaviour
{
    [SerializeField] public Color outlineColor = Color.white;
    [SerializeField, Range(0f, 8f)] public float outlineWidth = 1f;

    private readonly List<SpriteRenderer> spriteRenderers = new();
    private readonly List<Material> originalMaterials = new();   // ✅ cache originals
    private Material runtimeMat;

    private static readonly int _OutlineColor = Shader.PropertyToID("_OutlineColor");
    private static readonly int _OutlineThickness = Shader.PropertyToID("_OutlineThickness");

    void Awake()
    {
        GetComponentsInChildren(includeInactive: true, result: spriteRenderers);
        if (spriteRenderers.Count == 0)
        {
            Debug.LogWarning($"{name}: No SpriteRenderers found for SpriteOutline.", this);
            enabled = false;
            return;
        }

        // ✅ Cache each original material (use sharedMaterial so we don't force-instantiation)
        originalMaterials.Clear();
        foreach (var sr in spriteRenderers)
            originalMaterials.Add(sr != null ? sr.sharedMaterial : null);

        // Find shader and build our runtime material
        var shader = Shader.Find("Hidden/URP/SpriteOutlineURP");
        if (shader == null)
        {
            Debug.LogError("SpriteOutline: Shader 'Hidden/URP/SpriteOutlineURP' not found.", this);
            enabled = false;
            return;
        }

        runtimeMat = new Material(shader);
        runtimeMat.SetColor(_OutlineColor, outlineColor);
        runtimeMat.SetFloat(_OutlineThickness, outlineWidth);
    }

    void OnEnable()
    {
        if (runtimeMat == null) return;

        // ✅ Assign outline material while enabled
        foreach (var sr in spriteRenderers)
        {
            if (sr == null) continue;
            // Use .material to get a renderer-local instance (won’t change shared material in assets)
            sr.material = runtimeMat;
        }
    }

    void OnDisable()
    {
        // ✅ Restore the original material precisely; no purple surprises
        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            var sr = spriteRenderers[i];
            if (sr == null) continue;
            sr.sharedMaterial = originalMaterials[i]; // back to what it had
        }
    }

    void OnDestroy()
    {
        // Make sure no SR still references runtimeMat before destroying it
        if (Application.isPlaying && runtimeMat != null) Destroy(runtimeMat);
    }

    // Optional setters if you tweak at runtime
    public void SetOutlineColor(Color c)
    {
        outlineColor = c;
        if (runtimeMat) runtimeMat.SetColor(_OutlineColor, c);
    }

    public void SetOutlineWidth(float w)
    {
        outlineWidth = w;
        if (runtimeMat) runtimeMat.SetFloat(_OutlineThickness, w);
    }
}
