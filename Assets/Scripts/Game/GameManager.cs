using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;
using static Interactable;
using Cuts = CutsceneManager;

public class GameManager : Singleton<GameManager>
{
    [InspectorReadOnly] public GameObject masksContainer;

    [InspectorReadOnly] public MaskController currentMask;

    [Header("Main Character")]
    [InspectorReadOnly] public GameObject mainCharacterObject;
    public List<GameObject> inventoryObjects = new ();

    // Static shorthands to access main character object, movement and transform
    // Hungry? Try the classic Mac Object, or Mac Movement with added speed, or the new Mac Transform with added suffering!
    public static GameObject McObject => Instance.mainCharacterObject;
    public static MainCharacterMovement McMovement => McObject.GetComponent<MainCharacterMovement>();
    public static Transform McTransform => McObject.transform;

    public Dictionary<InteractType, List<Interactable>> interactableCache = new();

    public string PlayerName { get; private set; }

    private void Awake()
    {
        PlayerName = "Someone";

        InputManager.Player.Take.performed += _ =>
        {
            // todo error handling :D 
            currentMask.inspecting = false;
        };

        InputManager.Player.Cleanse.performed += _ =>
        {
            // todo same
            UIManager.Instance.CreatePopup("You cleanse the mask...");  
            currentMask.cleansed = true;
        };
    }

    public void Start()
    {
        InputManager.Instance.Push(InputManager.Player);
    }

    IEnumerator GameLoop()
    {
        // Testing mostly!
        var children = masksContainer.transform.Cast<Transform>().ToArray();
        foreach (Transform child in children)
        {
            // Move to the mask's position
            McTransform.DOMoveX(child.position.x, duration: 1f);

            // Start inspection the mask and stop to wait until it's done
            var mask = child.GetComponent<MaskController>();
            currentMask = mask; 
            mask.Inspect();
            yield return new WaitUntil(() => !mask.inspecting);

            // Done inspeting, now the outcome
            if (mask.stats.cursed && !mask.cleansed)
            {
                // Oops!
                DOTween.Sequence()
                    .Append(Cuts.Flash(Color.red, 1f));

                UIManager.Instance.CreatePopup("Oops!");  
            }
    
            // We don't need the object anymore
            child.DOKill();
            Destroy(child.gameObject);
        }
        ExitGame();
    }



    public override void OnSceneLoad(SceneContext context)
    {
        mainCharacterObject = context.mainCharacterObject;
        masksContainer = context.masksContainer;
        //ReloadInteractableCache(context.cachedInteractables);

        StartCoroutine(GameLoop());
    }

    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public Interactable FindInteractable(string targetName, InteractType targetType)
    {
        foreach (var i in FindObjectsOfType<Interactable>(includeInactive: true))
        {
            if (i.type != targetType) continue;
            if (i.gameObject.name == targetName)
            {
                return i;
            }
        }
        return null;
    }

    internal void SetPlayerName(string playerName)
    {
        Debug.Log($"Setting player name: '{playerName}'");
        DialogueManager.Instance.SetVariable("$playerName", playerName);
        PlayerName = playerName;
    }

    [YarnCommand("Quit")]
    public static void ExitGame()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

}
