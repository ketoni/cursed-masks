using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleBackdrop : InteractionEvent
{
    public bool toggleBackdropOn;

    protected override void Execute(InteractionContext context, string arg)
    {
        UIManager.Instance.ToggleBackdropElements(toggleBackdropOn);
    }
}
