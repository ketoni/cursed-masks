using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractionTrigger : MonoBehaviour
{
    public Interactable target;
    public string customYarnNode;
    public bool singleShot;
    public AutoTriggerType triggerOn;

    public enum AutoTriggerType
    {
        Enter,
        Exit,
    }

    void Interact(Interactor other)
    {
        target.Interact(other, customYarnNode == "" ? null : customYarnNode);
        if (singleShot)
        {
            GetComponent<Collider>().enabled = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggerOn == AutoTriggerType.Enter &&
            other.TryGetComponent<Interactor>(out var interactor))
        {
            Interact(interactor);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (triggerOn == AutoTriggerType.Exit &&
            other.TryGetComponent<Interactor>(out var interactor))
        {
            Interact(interactor);
        }
    }

}
