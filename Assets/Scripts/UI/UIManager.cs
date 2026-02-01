using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : Singleton<UIManager>
{
    public DialogueView dialogueView;
    public HudController hud;
    public GameObject popupPrefab;
    public GameObject popupParentObject;

    //public float DefaultTypewriterSpeed;

    private void Start()
    {
    #if !UNITY_EDITOR
        //HideAndLockCursor();
    #endif

    }

    public void CreatePopup(string text, InputAction dismissAction = null)
    {
        var prefab = Instantiate(Instance.popupPrefab, Instance.popupParentObject.transform);
        var popup = prefab.GetComponent<Popup>();
        popup.content.text = text;

        // Auto dismiss or dismiss via action?
        if (dismissAction == null)
        {
            DOTween.Sequence().AppendInterval(5).AppendCallback(() => popup.Dismiss());
        }
        else
        {
            popup.DismissAction = dismissAction;
        }
    }

    public void ToggleBackdropElements(bool showElements)
    { 
        // enable
        if(showElements)
        {
            Instance.dialogueView.backdropBG.SetActive(true);
            Instance.dialogueView.characterPortrait.SetActive(false);
            Instance.dialogueView.dialogueBox.SetActive(false);
        }
        else // disable
        {
            Instance.dialogueView.backdropBG.SetActive(false);
            Instance.dialogueView.characterPortrait.SetActive(true);
            Instance.dialogueView.dialogueBox.SetActive(true);
        }
    }

    public void SetOrtographicScaling(OrtographicScaling target)
    {
        var angle = CutsceneManager.Cam.transform.rotation.eulerAngles.x;
        target.ScaleByProjectionAngle(angle);
    }

    private void HideAndLockCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
