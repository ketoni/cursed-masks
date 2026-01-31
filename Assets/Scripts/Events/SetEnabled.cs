using System;
using System.Collections.Generic;
using UnityEngine;

// An event to enable and disable objects in scene
public class SetEnabled : InteractionEvent
{
    [SerializeField] List<GameObject> targets = new();

    [EventArguments("true", "false")]
    protected override void Execute(InteractionContext context, string argument)
    {
        foreach (var t in targets) t.SetActive(bool.Parse(argument));
    }
}
