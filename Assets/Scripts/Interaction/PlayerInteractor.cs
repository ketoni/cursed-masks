using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : Interactor
{
    private void Start()
    {
        InputManager.Player.Interact.performed += InteractWithClosest;
    }

    void OnDestroy()
    {
        //InputManager.Player.Interact.performed -= InteractWithClosest;
    }

    protected override void InteractWithClosest(InputAction.CallbackContext ctx)
    {
        base.InteractWithClosest(ctx);
    }

    public override void Collect(Interactable interactable)
    {
        base.Collect(interactable);
        FMODUnity.RuntimeManager.PlayOneShotAttached(Sounds.Player.CollectItem, gameObject);
    }

    public override Interactable GetFromInventory(Interactable interactable)
    {
        var ret = base.GetFromInventory(interactable);
        if (ret != null)
        {
            FMODUnity.RuntimeManager.PlayOneShotAttached(Sounds.Player.GiveItem, gameObject);
        }
        return ret;
    }
}
