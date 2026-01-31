using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using static NPCMovement;
using System;

public class MoveTo : InteractionEvent
{
    [SerializeField] protected List<Transform> followTargets = new();
    [SerializeField] CharacterMovement movement; // Who are we moving
    [Min(0.5f)]
    [SerializeField] float stopDistance = 1f;
    [SerializeField] bool sprint = false;
    [Obsolete("Use interactableDuringMovement")]
    [SerializeField] protected bool enableInteraction = false;

    public InteractableDuring interactableDuringMovement = InteractableDuring.After;

    public enum InteractableDuring
    {
        After,
        During,
        Both,
        Never,
    }

    void Reset()
    {
        // The component should exist in our parent at least
        if (transform.parent.TryGetComponent<CharacterMovement>(out var parentMovement))
        {
            movement = parentMovement;
        }
    }

    protected override void Execute(InteractionContext context, string arg)
    {
        // If we are moving an NPC, set their behavior and interactability
        var npc = movement as NPCMovement;
        if (npc != null)
        {
            movement.SetTarget(followTargets[0], stopDistance);
            npc.SetBehavior(MovementBehavior.FOLLOW);

            // Interactable during movement or not?
            context.interactable.enabled =
                interactableDuringMovement != InteractableDuring.Never &&
                interactableDuringMovement != InteractableDuring.After;

        }
        // If we are moving the MC, disable player controls
        else
        {
            InputManager.Player.Disable();
        }
        movement.sprinting = sprint;

        // TODO: Use dotween here, but how?
        // The controls disabling here is boilerplate otherwise
        IEnumerator MoveThroughTargets()
        {
            foreach (var targetLocation in followTargets)
            {
                movement.SetTarget(targetLocation, stopDistance);

                while (!movement.TargetInDistance)
                {
                    yield return null; // Wait for the next frame
                }
            }
            // Reached final position enable controls if we're MC
            if (npc == null)
            {
                InputManager.Player.Enable();
            }
            else
            {
                npc.SetBehavior(MovementBehavior.STATIC);
                // Interactable after movement or not?
                context.interactable.enabled =
                    interactableDuringMovement != InteractableDuring.Never &&
                    interactableDuringMovement != InteractableDuring.During;
            }
            movement.ClearTarget();
        }

        StartCoroutine(MoveThroughTargets());
    }

    private void OnDrawGizmosSelected()
    {
        if (followTargets.Count == 0 || movement == null)
        {
            return;
        }

        if (followTargets.Count == 1)
        {
            // If we only have one target, we only draw the line if we are moving towards it
            var target = followTargets[0];
            CircleDrawer.DrawCircle(target.position, stopDistance, Color.green);
            if (movement.TargetTransform == target)
            {
                Debug.DrawLine(movement.transform.position, target.position, Color.cyan);
            }
        }

        Transform previousTarget = null;

        foreach (var targetLocation in followTargets)
        {
            if (targetLocation == null)
            {
                return;
            }
            if (movement.TargetTransform == targetLocation)
            {
                // Draw a separate line with that transform we are moving towards
                Debug.DrawLine(movement.transform.position, targetLocation.position, Color.cyan);
            }
            // For targets beyond the first draw connecting lines 
            if (previousTarget != null)
            {
                Debug.DrawLine(previousTarget.position, targetLocation.position, Color.blue);
            } 
            previousTarget = targetLocation;

            // Draw circles with stopdistance for every target
            CircleDrawer.DrawCircle(targetLocation.position, stopDistance, Color.green);
        }
    }
}
