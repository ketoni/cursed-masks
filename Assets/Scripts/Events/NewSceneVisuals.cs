using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using Cuts = CutsceneManager;

public class NewSceneVisuals : InteractionEvent
{
    [SerializeField, NullWarn] Volume visuals;
    public float newWeight;
    [SerializeField] float tweenTime = 60f;

    protected override void Execute(InteractionContext context, string arg)
    {
        var time = ParseIntArg(arg, defaults: (int)tweenTime);
        Cuts.Sequence(false)
            .Append(Cuts.TweenEffectsWeight(visuals, newWeight, time));
    }

}
