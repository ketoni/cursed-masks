using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Volume))]
public class SpecialEffectsController : MonoBehaviour
{
    // A class for applying temporary global fx volume overrides
    // for different special occasions
    private Volume fx; 
    private SceneVisualsOverride sceneVisuals;
    private ColorAdjustments colorAdjustments;

    void Awake()
    {
        fx = GetComponent<Volume>();
        fx.profile.TryGet(out sceneVisuals);
        fx.profile.TryGet(out colorAdjustments);
    }

    void Start()
    {
        fx.weight = 0; 
    }

    public Tween FadeBack(float duration)
    {
        // Generic tween to fade the FX to zero
        return DOTween.To(() => fx.weight, x => fx.weight = x, 0, duration);
    }


}
