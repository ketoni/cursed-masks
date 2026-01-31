
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Yarn.Unity;

using Cuts = CutsceneManager;

public class BetaStartMenu : MonoBehaviour
{
    [SerializeField, NullWarn] TextMeshProUGUI nameField;
    public CanvasGroup inputContainerGroup;
    public string introYarnNode;

    public static bool acceptingInput = false;
    private string playerName;
    private bool nameConfirmed = false;


    private Keyboard keyboard;
    private const int MAX_NAME_LEN = 30;

    void OnEnable()
    {
        keyboard = Keyboard.current;
    }

    void Start()
    {
        acceptingInput = false;
        nameField.text = "";

        Cuts.Sequence(disableControls: false)
            .Append(Cuts.FadeIn(Color.black, 0f, underlay: true));
    }

    [YarnCommand("EnableNameInput")]
    public static void EnableInput()
    {
        acceptingInput = true;
    }

    void Update()
    {
        if (!acceptingInput || keyboard == null || nameConfirmed)
        {
            inputContainerGroup.alpha = 0;
            return;
        }
        else
        {
            inputContainerGroup.alpha = 1;
        }

        // Handle backspace
        if (keyboard.backspaceKey.wasPressedThisFrame && playerName.Length > 0)
        {
            playerName = playerName[..^1];
        }

        // Handle Enter / Return
        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            ConfirmName();
            return;
        }

        // Handle regular character input and update text field
        foreach (KeyControl key in keyboard.allKeys)
        {
            if (key.wasPressedThisFrame)
            {
                char c = GetCharFromKey(key);
                if (c != '\0')
                    playerName += c;
            }
        }

        // Clamp length and update
        if (playerName.Length > MAX_NAME_LEN)
        {
            playerName = playerName[..MAX_NAME_LEN];
        }
        nameField.text = playerName;
    }

    private void ConfirmName()
    {
        acceptingInput = false;
        nameConfirmed = true;
        GameManager.Instance.SetPlayerName(playerName);
        //AudioManager.Instance.PlaySound("event:/Music/...");
        DialogueManager.Instance.NextLine(enableControls: true);
    }

    [YarnCommand("StartMenuTransition")]
    public static void NextSceneTransition()
    {
        Cuts.Sequence(disableControls: false)
            .Append(Cuts.FadeIn(Color.white, 5f, underlay: false).SetEase(Ease.InQuad))
            .AppendCallback(() =>
            {
                GameManager.Instance.ChangeScene("MainScene");
                DialogueManager.Instance.NextLine();
                InputManager.Instance.Push(InputManager.Player);
            })
            .Append(Cuts.FadeOut(duration: 2))
            .AppendCallback(() => DialogueManager.Instance.StartDialogue(DialogueManager.Instance.sceneFirstYarnNode));
    }

    // Converts pressed KeyControl into a printable character
    private char GetCharFromKey(KeyControl key)
    {
        // Ignore non-character keys
        if (key.displayName == null || key.displayName.Length != 1)
            return '\0';

        char c = key.displayName[0];

        // Handle Shift for uppercase
        if (keyboard.shiftKey.isPressed)
            c = char.ToUpper(c);
        else
            c = char.ToLower(c);

        return c;
    }

}