using DG.Tweening;
using UnityEngine;
using Cuts = CutsceneManager;

public class VerticalTransition : InteractionEvent
{
    // A transition for moving up slopes which would otherwise take you out of view
    // We fade in, reposition the camera, then fade out
    public Transform camLocation;

    protected override void Execute(InteractionContext context, string argument)
    {
        Cuts.Sequence(disableControls: true)
            .Append(Cuts.FadeIn(duration: 1.5f))
            .Append(Cuts.LockCamera(camLocation))
            .Append(Cuts.FadeOut());
    }
}
