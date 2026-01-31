using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class Interactor : MonoBehaviour
{
    public Character character;
    public Transform parentTransform;
    public Transform inventory;
    List<Interactable> interactablesInRange = new();

    public CharacterMovement Movement => parentTransform.GetComponent<CharacterMovement>();

    public Collider Trigger => GetComponent<Collider>();

    public event System.Action<bool> OnAvailabilityChanged;
    private bool _wasAvailable;

    private void OnEnable()
    {
        Trigger.enabled = true;
    }

    private void OnDisable()
    {
        Trigger.enabled = false;
        interactablesInRange.Clear();
    }

    private void LateUpdate()
    {
        // Invoke interaction availability when it changes
        bool nowAvailable = AvailableInRange.Count > 0;
        if (nowAvailable != _wasAvailable)
        {
            _wasAvailable = nowAvailable;
            OnAvailabilityChanged?.Invoke(nowAvailable);
        }
    }

    internal void Enqueue(Interactable interactable)
    {
        if (!interactablesInRange.Contains(interactable))
        {
            interactablesInRange.Add(interactable);
        }
    }

    internal void Dequeue(Interactable interactable)
    {
        interactablesInRange.RemoveAll(i => i == interactable);
        interactable.SetInteractionHighlight(false);
    }

    public List<Interactable> AllInRange
    {
        // Returns all Interactables in interaction range
        // The function clears any stored interactable references of which have stopped existing
        get
        {
            interactablesInRange.RemoveAll(item => item == null);
            var inRange = new List<Interactable>();
            foreach (var interactable in interactablesInRange)
            {
                inRange.Add(interactable);
            }
            return inRange;
        }
    }

    public List<Interactable> AvailableInRange
    {
        // Returns only those Interactables in range which can be currently interacted with
        get
        {
            var items = AllInRange;
            items.RemoveAll(item => !item.CanInteractWith); // wtf ??
            return items;
        }
    }

    protected Interactable GetClosest()
    {
        // Returns the closes available interactable in range
        if (AvailableInRange.Count != 0)
        {
            var closest = AvailableInRange[0];
            for (int i = 1; i < AvailableInRange.Count; i++)
            {
                var other = AvailableInRange[i];
                var otherDist = Vector3.Distance(transform.position, other.transform.position);
                var closestDist = Vector3.Distance(transform.position, closest.transform.position);
                if (otherDist < closestDist) closest = other;
            }
            return closest;
        }
        return null;
    }

    protected virtual void InteractWithClosest(InputAction.CallbackContext _)
    {
        // Interacts with the closest available interactable in range
        var closest = GetClosest();
        if (closest != null) closest.Interact(this);
    }

    public virtual Interactable GetFromInventory(Interactable interactable)
    {
        foreach (var i in inventory.GetComponentsInChildren<Interactable>())
        {
            if (i == interactable)
            {
                return interactable;
            }
        }
        return null;
    }


    public virtual void Collect(Interactable interactable)
    {
        interactable.gameObject.transform.SetParent(inventory);
    }

}
