using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Cuts = CutsceneManager;

public class TravelTransition : InteractionEvent
{
    [Header("— Fade In —")]
    public float fadeInTime = 1f;
    public Color fadeColor = Color.black;
    public Sprite fadeImage;
    public bool fadeInFreezeCam;
    public Transform walkInLocation;
    public bool runIn;
    [NullWarn] public Transform teleportLocation;
    [Header("— Fade Out —")]
    public float fadeOutTime = 1;
    public Transform walkOutLocation;
    public bool runOut;
    [Header("— In and Out —")]
    public float InOutFreezeTime = 0f;

    // Sequence storage
    Sequence sequence = null;

    [EventArguments("in", "out", "both"), DefaultArgument("both")]
    protected override void Execute(InteractionContext context, string arg)
    {
        if (string.IsNullOrEmpty(arg)) arg = "both";

        // Cancel previous tween, if any
        if (sequence.IsActive())
        {
            sequence.Complete();
            sequence.Kill();
            // We have to now teleport instantly and manually,
            // since our teleport tween does not support Complete() properly
            Cuts.Sequence(false)
                .Append(Cuts.TeleportMC(teleportLocation))
                    .Join(Cuts.ResetCamera());
        }
        sequence = Cuts.Sequence(disableControls: true);

        void FadeIn()
        {
            // We check if there is a sprite to fade into, if not we go with the fade to black
            var fadeInTween = fadeImage != null ?
                Cuts.FadeInImage(fadeImage, duration: fadeInTime, underlay: true) :
                Cuts.FadeIn(duration: fadeInTime, color: fadeColor, underlay: true);

            sequence
                // Disable interactions for the tween. This solves a bug where teleporting
                // inside a trigger zone fires OnTriggerEXITs for some reason???
                .AppendCallback(() => context.interactor.Dequeue(context.interactable))
                .AppendCallback(() => context.interactor.enabled = false)
                // Fade in, optionally freeze cam and walk in
                .Append(Cuts.If(walkInLocation != null, Cuts.WalkMainTo(walkInLocation, runIn)))
                    .Join(Cuts.If(fadeInFreezeCam, Cuts.SetCamFollow(false)))
                    .Join(fadeInTween)
                // Teleport after we have fully faded in and reset camera, then wait
                .Append(Cuts.TeleportMC(teleportLocation))
                    .Join(Cuts.ResetCamera());
        }

        void FadeOut()
        {
            sequence
                // Walk and fade out, enable interactions
                .Append(Cuts.If(walkOutLocation != null, Cuts.WalkMainTo(walkOutLocation, runOut)))
                .Append(Cuts.FadeOut(fadeOutTime))
                .AppendCallback(() => context.interactor.enabled = true);
        }

        switch (arg)
        {
            case "in":
                FadeIn();
                break;
            case "out":
                FadeOut();
                break;
            case "both":
                FadeIn();
                sequence.AppendInterval(InOutFreezeTime);
                FadeOut();
                break;
            default:
                throw new ArgumentException(arg);
        }

    }
}
