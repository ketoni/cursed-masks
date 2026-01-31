using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CanvasGroup))]
public class Popup : MonoBehaviour
{
    public TextMeshProUGUI content;
    public CanvasGroup canvasGroup;
    public InputAction DismissAction
    {
        set
        {
            // Like and subscribe
            dismissAction = value;
            dismissAction.performed += OnDismiss;
        }
    }

    private InputAction dismissAction;

    public void Dismiss()
    {
        canvasGroup.DOFade(0f, 2)
            .SetEase(Ease.InQuad)
            .OnComplete(() => Destroy(gameObject));
    }

    private void OnDismiss(InputAction.CallbackContext ctx)
    {
        // Don't like and unsubscribe
        dismissAction.performed -= OnDismiss;
        Dismiss();
    }

    private void OnDestroy()
    {
        // Cleanup if destroyed early
        if (dismissAction != null)
            dismissAction.performed -= OnDismiss;
    }
}
