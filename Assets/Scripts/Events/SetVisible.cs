using System.Collections.Generic;
using UnityEngine;

// An event to make the object seemingly inivisible and uninteractable
// You should use SetEnabled() if you want to also disable all script activity etc
public class SetVisible : InteractionEvent
{
    public List<Light> lights = new();
    public List<Renderer> renders = new();

    void Start()
    {
        foreach (var light in transform.parent.GetComponentsInChildren<Light>())
        {
            lights.Add(light);
        }
        foreach (var rend in transform.parent.GetComponentsInChildren<Renderer>())
        {
            renders.Add(rend);
        }
    }

    protected override void Execute(InteractionContext context, string argument)
    {
        var enabled = MapBoolean(argument);
        foreach (var light in lights)
        {
            light.enabled = enabled; 
        }
        foreach (var rend in renders)
        {
            rend.enabled = enabled;
        }
        context.interactable.enabled = enabled;
    }
}
