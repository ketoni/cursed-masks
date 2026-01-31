
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

public abstract class InteractionEvent : MonoBehaviour
{
    public string eventIdentifier;
    public string EventName => GetType().Name;

    // The interface which derived events override
    protected abstract void Execute(InteractionContext context, string argument);

    // Public interface with validation 
    public void Execute(InteractionContext context, string argument, bool validate)
    {
        argument = (argument ?? "").Trim().ToLower(); // Sanityze (convenient spaghetti) 

        var executor = context.interactable.gameObject.name;
        var label = $"<color=yellow>{EventName}</color>"
            + (eventIdentifier is "" ? "" : $" <color=cyan>'{eventIdentifier}'</color>")
            + (argument is "" ? "" : $" <color=magenta>({argument})</color>");
        Debug.Log($"{executor} executing {label}", this);

        Execute(context, validate ? ValidateArgument(argument) : argument);
    }

    protected Interactable Interactable => transform.parent.GetComponent<Interactable>();

    void Awake()
    {
        if (Interactable == null)
        {
            Debug.LogWarning("InteractionEvents should be immediate children of Interactables", this);
        }
    }

    protected string ValidateArgument(string arg)
    {
        // Can be used to validate event argument with evil reflection-revel spaghetti NYEHHEHHEH >:3

        // Whenever you see 'using System.Reflection', you know someone has def been smokin some herb :DDD
        var method = GetType().GetMethod(
            nameof(Execute), BindingFlags.Instance | BindingFlags.NonPublic,
            null, new[] { typeof(InteractionContext), typeof(string) }, null);

        // If we have a default and arg is undefined, use it instead 
        var name = EventName; 
        var def = method.GetCustomAttribute<DefaultArgumentAttribute>()?.DefaultArgument;
        if (!string.IsNullOrEmpty(def) && string.IsNullOrEmpty(arg))
        {
            arg = def;
            name = "(default) " + EventName;
        }

        // Ensure arg is as expected
        var expected = method.GetCustomAttribute<EventArgumentsAttribute>();
        if (expected != null && !expected.Arguments.Contains(arg))
            throw new ArgumentException(
                $"Invalid {name} argument '{arg}'! Expected any of " +
                string.Join(", ", expected.Arguments.Select(a => $"'{a}'")));


        return arg;
    }

    // An interface for random scripts to execute events of Interactables.
    // If you do this while the event requires a proper Interactor, you will likely hit a NullReferenceError.
    public void ExecuteFrom(MonoBehaviour mono, string argument, bool validate = true)
    {
        // It's no use using the context from DialogueManager, as it could be stale at this point.
        // We can only assume that the calling script is an Interactor itself, if at all.
        var context = new InteractionContext()
        {
            interactor = mono as Interactor,
            interactable = Interactable,
        };
        Debug.Log($"{mono.name} executing {EventName} ({argument}) on {Interactable}");
        Execute(context, argument, validate);
    }

    public override string ToString()
    {
        return eventIdentifier != "" ? $"Event '{EventName}' ({eventIdentifier})" : $"Event '{EventName}'";
    }

    public int ParseIntArg(string argument, int defaults)
    {
        if (!int.TryParse(argument, out int number))
        {
            return defaults; 
        }
        return number;
    }

    // Function to handle simple "true" and "false" arguments
    public static bool MapBoolean(string argument)
    {
        if (string.IsNullOrEmpty(argument))
            throw new ArgumentException("Input cannot be null or empty.");

        string key = argument.Trim().ToLower();
        if (key == "true") return true;
        if (key == "false") return false;
        throw new ArgumentException($"Invalid event argument: '{argument}'. Expected 'true' or 'false'.");
    }


}

// These classes are used by the argument validation of the base InteractionEvent,
// and to define the expected and default argument(s) for each derived event
internal class EventArgumentsAttribute : Attribute
{
    public string[] Arguments { get; }

    public EventArgumentsAttribute(params string[] arguments)
    {
        Arguments = arguments;
    }
}

internal class DefaultArgumentAttribute : Attribute
{
    public string DefaultArgument { get; }

    public DefaultArgumentAttribute(string argument)
    {
        DefaultArgument = argument;
    }
}