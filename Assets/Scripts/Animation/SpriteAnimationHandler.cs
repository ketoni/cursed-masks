using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public class SpriteAnimationHandler : MonoBehaviour
{
    [SerializeField] Transform feetLocation;

    [Header("Animator Parameters")]
    public float horizontal;
    public float vertical;
    public float speed;
    [SerializeField] bool verticalMirrorsX;
    bool originalX;

    public float PlaybackRate
    {
        get
        {
            return animator.speed;
        }
        set
        {
            animator.speed = value;
        }
    }

    Animator animator;
    new SpriteRenderer renderer;
    float height = 0;

    void Awake()
    {
        animator = GetComponent<Animator>();
        renderer = GetComponent<SpriteRenderer>();
        height = renderer.sprite.bounds.size.y;
        originalX = renderer.flipX;
    }

    void Start()
    {
        // Change the animator speed by 5% so that similar animations do not sync
        //animator.speed *= Random.Range(0.95f, 1.05f);
    }

    void Update()
    {
        if (animator.HasParameter("Horizontal"))
        {
            animator.SetFloat("Horizontal", horizontal);
        }
        if (animator.HasParameter("Vertical"))
        {
            animator.SetFloat("Vertical", vertical);
        }
        if (animator.HasParameter("Speed"))
        {
            animator.SetFloat("Speed", speed);
        }
        if (verticalMirrorsX)
        {
            renderer.flipX = vertical < 0 ? originalX : !originalX;
        }
    }

    void WalkStepCallback(AnimationEvent evt)
    {
        if(feetLocation == null) return; // In the 2D minigames there might not be a feet location
        if (evt.animatorClipInfo.weight <= 0.5f)
        {
            // For some reason other animations can still fire their events
            // even if they are not the one currently displayed by the blend tree.
            // Thus we check the weigh here. See: https://discussions.unity.com/t/576456
            return;
        }

        // First check if we are submerged in water
        float waterSubmersion = TerrainSampler.Instance.SampleWaterPlanesAt(feetLocation.position, height);
        if (waterSubmersion > 0.01)
        {
            // Note: You might want to tweak the volume of this based on `waterSubmersion`.
            // Using just the value by itself might not be enough, since it ramps up too slowly
            AudioManager.Instance.PlaySound(
                eventPath: "event:/SFX/Player/footstep",
                parameterName: "Material",
                parameterValue: Sounds.Walking.Water);

            // If we are in water we don't need to do other terrain sampling
            return;
        }

        // Then check if we are walking on an object with a material
        var surface = TerrainSampler.Instance.SampleSurfaceTypeAt(feetLocation.position);
        
        if (surface != null)
        {
            string surfaceName = surface.type.name;
            if (!AudioManager.Instance.GetParameterValue(typeof(Sounds.Walking), surfaceName, out int value))
            {
                Debug.LogError($"Missing walk sound parameter for terrain layer '{surfaceName}' (referenced by SurfaceType)");
                return;
            }
            AudioManager.Instance.PlaySound(
                eventPath: "event:/SFX/Player/footstep",
                volume: 1.0f,
                parameterName: "Material",
                parameterValue: value);
            // If we are walking on top of something with a walk sound, no need to sample textures

            if (surface.walkEffect != null)
            {
                var effectOffset = new Vector3(0, surface.effectOffset, 0);
                Instantiate(surface.walkEffect, feetLocation.position + effectOffset, Quaternion.identity);
            }
            return;
        }
        
        // Lastly, Sample textures under the object and emit appropriate sounds
        var texturesMap = TerrainSampler.Instance.SampleTerrainLayersAt(transform.position);
        foreach (var (layerName, weight) in texturesMap)
        {
            if (weight < 0.2f) continue; // Clamp any footsteps too quiet
            if (!AudioManager.Instance.GetParameterValue(typeof(Sounds.Walking), layerName, out int value))
            {
                Debug.LogError($"Missing walk sound parameter for terrain layer '{layerName}'");
                continue;
            }
            AudioManager.Instance.PlaySound(
                eventPath: "event:/SFX/Player/footstep",
                volume: Mathf.Min(weight, 1.0f),
                parameterName: "Material",
                parameterValue: value);
        }
    }

}
