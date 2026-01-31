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

    public int initialTimeTicks = 12;
    public float timeTickDurationInSec = 15f;
    public int timeTicksLeft;
    public float elapsedTime;

    public int initialSanity = 8;
    public int sanity;

    public int requiredKeyMaskCount;
    public int keyMaskCount;
    public int collectedTotalBaseValue;
    public int collectedTotalBonusValue;

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

    public int Sanity
    {
        get
        {
            return sanity;
        }

        set
        {
            sanity = value;
            if (sanity <= 0)
            {
                EndGameInFailure(outOfSanity: true, !HasEnoughKeyMasks);
            }
        }
    }

    public bool HasEnoughKeyMasks => keyMaskCount >= requiredKeyMaskCount;

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
            currentMask.CurseLevel--;
        };

        InputManager.Player.Analyze.performed += _ =>
        {
            // TODO: Get clicked feature
            var text = currentMask.GetRandomRelevantRuleText(MaskFeature.Nose);
            if (text != null)
            {
                DialogueManager.Instance.ShowText(text);
            }
        };

        InputManager.Player.Click.performed += _ =>
        {
            Vector2 screenPos = InputManager.Player.Point.ReadValue<Vector2>();
            Ray ray = Camera.main.ScreenPointToRay(screenPos / 4); // Camera space is quarter of FullHD
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.TryGetComponent<MaskPart>(out var part))
                {
                    // TODO inspecting currentMask/part here
                    DialogueManager.Instance.ShowText(part.transform.name);
                } 
            }
        };
    }

    public void Start()
    {
        InputManager.Instance.Push(InputManager.Player);
    }

    public void Update()
    {
        ProgressTime();
    }

    IEnumerator GameLoop()
    {
        ResetGame();

        // Testing mostly!
        var children = masksContainer.transform.Cast<Transform>().ToArray();
        foreach (Transform child in children)
        {
            // Move to the mask's position
            McTransform.DOMoveX(child.position.x, duration: 1f);

            // Start inspection the mask and stop to wait until it's done
            var mask = child.GetComponent<MaskController>();
            currentMask = mask; 
            mask.Init();
            mask.Inspect();
            yield return new WaitUntil(() => !mask.inspecting);

            // Done inspeting, now the outcome
            if (mask.CurseLevel > 0)
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

    public void ProgressTime()
    {
        if (currentMask.StopsTime)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        if (elapsedTime >= timeTickDurationInSec)
        {
            timeTicksLeft--;
            elapsedTime = 0;

            if (timeTicksLeft <= 0)
            {
                CheckEscape();
            }
        }
    }

    public void CheckEscape()
    {
        if (!HasEnoughKeyMasks)
        {
            EndGameInFailure(outOfSanity: false, notEnoughKeyMasks: true);
        }
    }

    public void EndGameInFailure(bool outOfSanity, bool notEnoughKeyMasks)
    {
        // TODO
    }

    public void ResetGame()
    {
        timeTicksLeft = initialTimeTicks;
        elapsedTime = 0;
        sanity = initialSanity;
        collectedTotalBaseValue = 0;
        collectedTotalBonusValue = 0;
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
