using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Yarn.Unity;

public class CutsceneManager : Singleton<CutsceneManager>
{
    public static CameraController Cam => Instance.mainCamera;

    // Loaded for every scene. Contains more specialized effects
    [InspectorReadOnly] public SpecialEffectsController special;
    public static SpecialEffectsController Special => Instance.special;

    [SerializeField, NullWarn] CameraController mainCamera;
    [SerializeField, NullWarn] Image overlayImage;
    [SerializeField, NullWarn] Image underlayImage;
    [SerializeField, NullWarn] RawImage renderTexture;
    [SerializeField, NullWarn] Material posterization;

    private Light sunDirectionalLight;
    private SceneVisualsOverride visuals;

    void Awake()
    {
        visuals = VolumeManager.instance.stack.GetComponent<SceneVisualsOverride>();
    }

    void Start()
    {
    }

    private void LateUpdate()
    {
        //TODO maybe move this to SpecialEffectController and make it into a more generic volume controller?
        // Set sun according to our FX volume
        sunDirectionalLight.intensity = visuals.sunIntensity.value;
        sunDirectionalLight.transform.rotation = Quaternion.Euler(visuals.sunOrientation.value);

        // Same with posterization
        posterization.SetFloat("_RedIntensity", visuals.posterizationRGB.value.x);
        posterization.SetFloat("_GreenIntensity", visuals.posterizationRGB.value.y);
        posterization.SetFloat("_BlueIntensity", visuals.posterizationRGB.value.z);
    }

    public override void OnSceneLoad(SceneContext sceneContext)
    {
        sunDirectionalLight = sceneContext.sunDirectionalLight;
        Cam.mainCharacterCam = sceneContext.mainCamera;
        SpawnSpecialFXController(sceneContext.transform);
    }

    private void SpawnSpecialFXController(Transform parent)
    {
        var prefab = Resources.Load<GameObject>("Effects/SpecialFXController");
        special = Instantiate(prefab, parent).GetComponent<SpecialEffectsController>();
    }

    public static Sequence Sequence(bool disableControls)
    {
        // The starting point of a sequence. Use this to chain other effects, like:
        // CutsceneManager.Sequence()
        //     .Append(CutsceneManager.FadeIn())
        //     .Append(...
        var sequence = DOTween.Sequence();
        if (disableControls)
        {
            sequence.AppendCallback(() => {
                if (!InputManager.Instance.IsOnTop(InputManager.Player))
                {
                    Debug.LogWarning("Refusing to disable controls for Sequence: Player controls not on top");
                    disableControls = false;
                }
                else
                {
                    InputManager.Player.Disable();
                }
            });
            sequence.OnComplete(() => {
                // We make sure to re-enable Player controls, but only if nothing new is on the input stack
                if (disableControls && InputManager.Instance.IsOnTop(InputManager.Player))
                {
                    InputManager.Player.Enable();
                }
            });
        }
        return sequence;
    }

    public static Tween TweenEffectsWeight(Volume effects, float newWeight, float duration)
    {
        return DOTween.To(() => effects.weight, x => effects.weight = x, newWeight, duration);
    }

    public static Tween WalkMainTo(Transform location, bool run = false, float timeout = 5f)
    {
        // Walks the main character to the given world position.
        // When the character reaches target position or timeout expires,
        // the target of CharacterMovement is cleared, unless it has changed since we started

        IEnumerator follow()
        {
            var timer = 0f;
            var main = GameManager.McMovement;

            main.SetTarget(location);
            main.sprinting = run;
            while (!main.TargetInDistance)
            {
                timer += Time.deltaTime;
                if (timer > timeout) break;
                yield return null;
            }
            if (main.TargetTransform.gameObject == location.gameObject)
            {
                main.ClearTarget();
                if (timer > timeout)
                {
                    Debug.LogWarning("WalkMainTo timed out (location as reference)", location);
                }
            }
        }

        return DOTween.Sequence()
            .AppendCallback(() => {
                Instance.StartCoroutine(follow());
            });
    }

    internal static Tween SetCamFollow(bool follow)
    {
        // Whether to set the current cam to follow the designated its follow target or not
        return DOTween.Sequence()
            .AppendCallback(() =>
            {
                Cam.SetFollow(follow);
            });
    }

    internal static Tween FadeIn(Color color = default, float duration = 1f, bool underlay = false)
    {        
        if (color == default)
        {
            color = Color.black;
        }
        var image = underlay ? Instance.underlayImage : Instance.overlayImage;
        image.sprite = null; // Clear any previous image or mask (WHOOSH)
        color.a = 0;
        image.color = color;
        return image.DOFade(1, duration);
    }

    internal static Tween FadeInImage(Sprite sprite, Color color = default, float duration = 1f, bool underlay = false, float targetAlpha = 1f)
    {
        var image = underlay ? Instance.underlayImage : Instance.overlayImage;

        if (sprite != null)
            image.sprite = sprite;

        image.DOKill(); // Stop prev animations on this image
        image.color = color == default ? new(1,1,1,0) : color;
        image.enabled = true;
        // image.canvasRenderer?.SetAlpha(0f); // optional: keep UI renderer in sync

        return image
            .DOFade(targetAlpha, duration)
            .SetUpdate(false);
    }

    [YarnCommand("ShowImage")]
    public static void YarnFadeInImage(string resourcePath, float duration = 1f)
    {
        var sprite = Resources.Load<Sprite>(resourcePath);
        DOTween.Sequence().Append(FadeInImage(sprite, default, default, true));
    }

    [YarnCommand("FadeOut")]
    public static void YarnFadeOut()
    {
        DOTween.Sequence().Append(FadeOut());
    }

    internal static Tween FadeOut(float duration = 1f)
    {
        return Sequence(false)
            .Append(Instance.overlayImage.DOFade(0, duration))
            .Join(Instance.underlayImage.DOFade(0, duration));
    }

    internal static Tween Flash(Color color = default, float duration = 1f)
    {
        return Sequence(false)
            .Append(FadeIn(color, 0))
            .Append(FadeOut(duration));
    }

    internal static Tween Shake(float strength = 5f, float duration = 1f)
    {
        return Instance.renderTexture.rectTransform.DOShakePosition(duration, strength);
    }

    internal static Tween ShakeHUD(float strength = 10f, float duration = 1f)
    {
        return UIManager.Instance.hud.transform.DOShakePosition(duration, strength);
    }

    internal static Tween EnvironmentDip(float dipMultiplier, float duration = 1f)
    {
        var originalAmbient = RenderSettings.ambientIntensity; 
        var originalIntensity = Instance.sunDirectionalLight.intensity;
        var inDuration = duration * 0.2f;
        var outDuration = duration * 0.8f;
        return Sequence(false)
            .Append(DOTween.To(
                () => RenderSettings.ambientIntensity,
                x => RenderSettings.ambientIntensity = x,
                dipMultiplier * originalAmbient,
                inDuration))
            .Join(Instance.sunDirectionalLight.DOIntensity(dipMultiplier * originalIntensity, inDuration))
            .Append(DOTween.To(
                () => RenderSettings.ambientIntensity,
                x => RenderSettings.ambientIntensity = x,
                originalAmbient,
                inDuration))
            .Join(Instance.sunDirectionalLight.DOIntensity(originalIntensity, inDuration));
    }

    internal static Tween TeleportMC(Transform location)
    {
        return TeleportObject(GameManager.McObject, location);
    }

    internal static Tween TeleportObject(GameObject targetObject, Transform location)
    {
        var transform = targetObject.transform;
        var rb = targetObject.GetComponent<Rigidbody>();
        return DOTween.Sequence()
            .AppendCallback(() => {
                rb.isKinematic = true;
                transform.position = location.position;
            })
            .AppendInterval(0.1f) // Wait a bit for physics
            .AppendCallback(() => {
                rb.isKinematic = false;
            }).SetUpdate(UpdateType.Fixed);
    }

    // Bounce given transform to targetPos 
    // if startingPos is given, it's used instead of characterTransform.position
    // startingPos is useful if characterTransform.position if not correct when creating this sequence
    internal static Tween BounceTo(Transform characterTransform, Vector3 targetPos, Vector3? startingPos = null)
    {
        Vector3 startPos = (Vector3) (startingPos != null ?  startingPos : characterTransform.position);
        float distance = Vector3.Distance(targetPos, startPos);
        var success = characterTransform.TryGetComponent<Rigidbody>(out var rb);
        var sequence = DOTween.Sequence().SetUpdate(UpdateType.Fixed);
        sequence.AppendCallback(() =>
        {
            // TO-DO maybe: should the lantern be kinematic too?
            if(success) rb.isKinematic = true;
        });

        float heightDiff = Mathf.Abs(targetPos.y - startPos.y);
        float bounceHeight = heightDiff + distance * 0.5f;

        sequence.Append(characterTransform.DOJump(targetPos, bounceHeight, 1, distance / 10));
        sequence.AppendCallback(() => {
            if(success) rb.isKinematic = false;
        });
        return sequence;  
    }

    // Bounce given transform to position of given transform
    internal static Tween BounceTo(Transform characterTransform, Transform targetTransform)
    {
        return BounceTo(characterTransform, targetTransform.position);
    }

    // Show image in Canvas
    internal static Tween ShowImage(Image image, float duration)
    {
        return DOTween.Sequence()
        .Append(image.DOFade(1f, duration))
        .OnComplete(() => {
            image.enabled = false;
        });
    }

    internal static Tween ResetCamera()
    {
        return DOTween.Sequence().AppendCallback(() => { Cam.Reset(); });
    }

    public static void HideMainCharacter(bool hide)
    {
        // Uhhhhh this looks pretty nasty XD
        // Lantern
        GameManager.McTransform.GetChild(0).gameObject.SetActive(!hide);
        // Main character
        GameManager.McTransform.GetChild(1).GetChild(1).gameObject.SetActive(!hide);
    }

    internal static Tween If(bool condition, Tween tween)
    {
        // Shorthand method to conditionally chain Tweens
        if (!condition)
        {
            // Ensure DOTween doesn't run the tween
            tween.Kill();
        }
        return condition ? tween : DOTween.Sequence();
    }

    internal static Tween LockCamera(Transform lockLocation = null)
    {
        return DOTween.Sequence()
            .AppendCallback(() => { Cam.Detach(); })
            .AppendCallback(() => {
                if (lockLocation != null)
                {
                    Cam.transform.position = lockLocation.position;
                }
            });
    }
}
