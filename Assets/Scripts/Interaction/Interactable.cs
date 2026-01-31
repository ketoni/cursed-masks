using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    public InteractType type;
    public Character character;
    public string yarnNodeName;
    public List<string> instanceVariableNames = new();
    public AutoTriggerType autoInteract;
    public bool once;
    public InteractionEvent[] Events => GetComponentsInChildren<InteractionEvent>();
    public string Identifier => Math.Abs(gameObject.GetInstanceID()).ToString();

    [Header("Highlight")]
    [SerializeField] private bool useSpriteOutline = false; // ← choose 2D sprite outline
    [SerializeField] private Color highlightColor = Color.white;
    [SerializeField, Range(0f, 8f)] private float highlightWidth = 2f;

    // Use a generic Behaviour so we can store either Outline or SpriteOutline
    Behaviour interactionHighlight;

    const float OUTLINE_WIDTH = 1f; // you can remove this if you prefer highlightWidth above

    // Defaults for 3D outline
    [SerializeField] private Color default3DOutlineColor = new Color(1f, 0f, 0f, 42f / 256f);
    [SerializeField] private Outline.Mode default3DOutlineMode = Outline.Mode.OutlineVisible;

    public bool HasHighlight => type == InteractType.Interest && autoInteract == AutoTriggerType.None;

    public bool HasDialogue => yarnNodeName != "";

    // Only required for NPC types
    public NPCMovement Movement => GetComponent<NPCMovement>();

    public GameObject speechBubble;

    //TODO it would be best to have these as derived classes... Onegai desuuu~
    public enum InteractType
    {
        Collectable,
        NPC,
        Interest,
    }

    public enum AutoTriggerType
    {
        None,
        OnEnter,
        OnExit,
    }

    public bool CanInteractWith
    {
        // Whether an Interactor should be able to interact with this
        get
        {
            return enabled && !held;
        }
    }

    public bool HasEvents => Events.Length != 0;

    public InteractionEvent FindEvent(string eventName, string identifier)
    {
        bool eventExists = false;
        foreach (var e in Events)
        {
            if (e.EventName != eventName) continue;
            eventExists = true;
            if (identifier != null && e.eventIdentifier != identifier) continue;
            return e;
        }
        if (eventExists && identifier != null)
        {
            throw new ArgumentException($"Could not find '{eventName}' with identifier '{identifier}'");
        }
        throw new ArgumentException($"Interactable does not implement any InteractionEvent named '{eventName}'");
    }

    bool held;
    float followForceMagnitude = 3f;

    void Awake()
    {
        if (type == InteractType.NPC)
        {
            if (character == null)
            {
                Debug.LogError("NPC is missing Character definition!");
            }
            if (Movement == null)
            {
                Debug.LogWarning("NPCs should have NPCMovement");
            }
        }

        foreach (var instanceVariable in instanceVariableNames)
        {
            DialogueManager.Instance.InitInstanceVariable(instanceVariable, this);
        }

        if (HasHighlight)
        {
            if (useSpriteOutline)
            {
                var so = gameObject.AddComponent<SpriteOutline>();
                so.outlineColor = highlightColor;
                so.outlineWidth = highlightWidth; // in texels/pixels
                so.enabled = false;
                interactionHighlight = so;
            }
            else
            {
                var mo = gameObject.AddComponent<Outline>();
                mo.OutlineWidth = highlightWidth;   // or OUTLINE_WIDTH
                mo.OutlineMode = default3DOutlineMode;
                mo.OutlineColor = default3DOutlineColor;
                mo.enabled = false;
                interactionHighlight = mo;
            }
        }

        if (speechBubble != null)
        {
            speechBubble.SetActive(false);
        }
    }

    protected virtual void Reset()
    {
        // Ensure we have a trigger 
        foreach (var collider in GetComponents<Collider>())
        {
            if (collider.isTrigger)
            {
                return;
            }
        }
        var first = GetComponent<Collider>();
        Debug.Log("Made the first Interactable Collider a trigger");
        first.isTrigger = true;
    }

    private void FixedUpdate()
    {
        if (held)
        {
            var targetPos = transform.parent.position;
            if (TryGetComponent<Rigidbody>(out var rb))
            {
                var direction = (targetPos - transform.position).normalized;
                rb.AddForce(direction * followForceMagnitude, ForceMode.Force);
            }
            else
            {
                transform.position = targetPos;
            }
        }
    }

    internal void Destroy(InteractionContext context)
    {
        // Correctly removes the Interactable GameObject from the scene
        // TODO: We could just use onDestroy, but Interactables do not know about the context on that level
        // They'd need to know where it is, and if it's "still valid"
        context.interactor.Dequeue(this);
        Destroy(gameObject);
    }

    // Note: Currently it's proper to use Delete() (see above),
    // whenever destroying Interactables during interaction
    private void OnDestroy()
    {
        enabled = false;
    }

    public void SetInteractionHighlight(bool enabled)
    {
        if (interactionHighlight == null) return; 
        interactionHighlight.enabled = enabled;
    }

    // Note: Currently, Interactables are the one reacting to Interactor colliding with them.
    // The problem is, that Interactables can have multiple colliders which react to Interactor's
    // 'trigger entering'. The right way would be to have the Interactor run these 
    // functions, because it only has one (trigger) collider, and thus it's easier for it to see
    // if we hit a trigger on an Interactable or not.
    private void OnTriggerEnter(Collider other)
    {
        if (!CanInteractWith) return;
        if (other.TryGetComponent<Interactor>(out var interactor))
        {
            if (HasHighlight) SetInteractionHighlight(true);

            if (speechBubble != null && !DialogueManager.Instance.IsDialogueRunning)
                speechBubble.SetActive(true);

            if (autoInteract == AutoTriggerType.OnEnter)
                Interact(interactor);
            else
                interactor.Enqueue(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Interactor>(out var interactor))
        {
            if (speechBubble != null)
                speechBubble.SetActive(false);

            if (HasHighlight) SetInteractionHighlight(false);

            if (CanInteractWith && autoInteract == AutoTriggerType.OnExit)
                Interact(interactor);

            interactor.Dequeue(this);
        }
    }

    public virtual bool Interact(Interactor interactor, string customYarnNode = null, bool force = false)
    {
        // Interacts with an Interactable and returns whether the interaction was done
        if (!CanInteractWith && !force)
        {
            return false;
        }

        if (once)
        {
            enabled = false;
        }

        // Build context and run dialogue, if any
        var context = new InteractionContext()
        {
            interactor = interactor,
            interactable = this,
        };
        DialogueManager.Instance.SetContext(context);
        if (customYarnNode != null)
        {
            DialogueManager.Instance.StartDialogue(customYarnNode);
        }
        else if (HasDialogue)
        {
            DialogueManager.Instance.StartDialogue(yarnNodeName);
        }
        if (speechBubble != null)
        {
            speechBubble.SetActive(false);
        }

        // Do interactable type-specific stuff after dialogue is done
        switch (type)
        {
            // Item: Add to inventory
            case InteractType.Collectable:
                interactor.Collect(this);
                held = true;
                break;
            // Interest: If we don't have dialogue (to run an attached event),
            // execute it now. We ought to have one in this case.
            case InteractType.Interest:
                if (!HasDialogue)
                {
                    Events[0].Execute(context, argument:"", validate:true);
                    if (TryGetComponent<FMODUnity.StudioEventEmitter>(out var sound))
                    {
                        //If this interest has a sound, play it immediately
                        sound.Play();
                    }
                }
                break;
            // NPC: Make both parties face eachother 
            case InteractType.NPC:
                Movement.orientation.LookAt(interactor.parentTransform);
                interactor.Movement.orientation.LookAt(transform);
                break;
            default:
                Debug.LogError($"Unhandled interactType {type}");
                return false;
        }
        return true;
    }

    [YarnCommand("ExecuteEvent")]
    public static void ExecuteContextEvent(string eventName, string identifier = null, string argument = default)
    {
        // Runs an event attached to the interactor using the dialogue interaction context
        var context = DialogueManager.Instance.Context;
        if (context == null)
        {
            Debug.LogError($"Can't execute {eventName} Event: No context!");
            return;
        }
        var interactable = context.interactable;
        if (!interactable.HasEvents)
        {
            Debug.LogError("ExecuteEvent called on an Interactable without Events", interactable);
            return;
        }
        try
        {
            var evt = interactable.FindEvent(eventName, identifier);
            evt.Execute(context, argument, validate:true);
        }
        catch (ArgumentException ex)
        {
            Debug.LogError("Could not execute event: " + ex.Message, interactable);
        }
    }

    [YarnCommand("ExecuteEventOf")]
    public static void RemoteExecuteContextEvent(string targetString, string eventName, string identifier = null, string argument = default)
    {
        var targetName = targetString;
        var targetType = InteractType.NPC;

        // Parse targetString
        var parts = targetString.Split("/");
        if (parts.Length == 2)
        {
            targetName = parts[0];
            var typeString = parts[1];
            if (!Enum.TryParse(typeString, out targetType))
            {
                throw new ArgumentException($"'{typeString}' is not a valid Interactable type");
            }
        }
        else
        {
            Debug.LogWarning("Please use the slash format \"name/type\" in ExecuteEventOf for more efficient lookups");
        }

        // First try looking up from the cache, then do a scene-wide search if not found
        var target = GameManager.Instance.GetCachedInteractable(targetName, targetType);
        if (target == null)
        {
            target = GameManager.Instance.FindInteractable(targetName, targetType);
            if (target == null)
            {
                throw new ArgumentException($"Failed to find remote event target '{targetName}/{targetType}'");
            }
            Debug.LogWarning($"Add Interactable '{targetName}' to the scene context cache for better lookups");
        }

        // Found target: Temporarily inject it into the interaction context
        var context = DialogueManager.Instance.Context;
        var tempContext = new InteractionContext()
        {
            interactor = context?.interactor,
            interactable = target,
        };
        // And then execute the given event
        DialogueManager.Instance.SetContext(tempContext);
        ExecuteContextEvent(eventName, identifier, argument);
        if (context != null)
        {
            DialogueManager.Instance.SetContext(context);
        }

    }

    [YarnCommand("SetBehavior")]
    public static void SetNPCBehaviorYarn(string behaviorName)
    {
        var interactable = DialogueManager.Instance.Context.interactable;
        if (interactable.type != InteractType.NPC)
        {
            throw new InvalidOperationException("SetBehavior can only be called when interacting with NPCs with movement");
        }
        if (Enum.TryParse(behaviorName.ToUpper(), out NPCMovement.MovementBehavior behavior))
        {
            interactable.Movement.SetBehavior(behavior);
        }
        else
        {
            throw new ArgumentException($"Unknown NPCMovement behavior '{behaviorName}'");
        }
    }
}

public static class InteractableExtensions
{
    public static void SetInstanceVariable(this Interactable interactable, string variableName, bool value)
    {
        DialogueManager.Instance.SetInstaceVariable(interactable, variableName, value);
    }
}
