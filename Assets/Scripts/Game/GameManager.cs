using Assets.Scripts.Generic;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.GraphView;
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

    [InspectorReadOnly] public float elapsedTime;
    [InspectorReadOnly] public int timeTicksLeft;

    public int initialSanity = 8;

    [InspectorReadOnly] public int sanity;

    public int requiredKeyMaskCount = 3;

    [InspectorReadOnly] public int keyMaskCount;
    [InspectorReadOnly] public int collectedTotalBaseValue;
    [InspectorReadOnly] public int collectedTotalBonusValue;

    public List<MaskController> baseMasks;
    public int rulesPerMask = 12;

    [InspectorReadOnly] public List<(int MaskId, MaskRule Rule)> maskRules;

    [Header("Main Character")]
    [InspectorReadOnly] public GameObject mainCharacterObject;
    public List<GameObject> inventoryObjects = new ();

    // Static shorthands to access main character object, movement and transform
    // Hungry? Try the classic Mac Object, or Mac Movement with added speed, or the new Mac Transform with added suffering!
    public static GameObject McObject => Instance.mainCharacterObject;
    public static MainCharacterMovement McMovement => McObject.GetComponent<MainCharacterMovement>();
    public static Transform McTransform => McObject.transform;

    public Dictionary<InteractType, List<Interactable>> interactableCache = new();

    public bool GameRunning { get; private set; }

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

            Debug.Log($"Sanity: {sanity}");

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
            if (!GameRunning)
            {
                return;
            }

            // todo error handling :D 
            currentMask.inspecting = false;
        };

        InputManager.Player.Cleanse.performed += _ =>
        {
            if (!GameRunning)
            {
                return;
            }

            // todo same
            UIManager.Instance.CreatePopup("You cleanse the mask...");
            currentMask.CurseLevel--;
        };

        //InputManager.Player.Analyze.performed += _ =>
        //{
        //    if (!GameRunning)
        //    {
        //        return;
        //    }

        //    // TODO: Get clicked feature
        //    var text = currentMask.GetRandomRelevantRuleText(MaskFeature.Nose);
        //    if (text != null)
        //    {
        //        DialogueManager.Instance.ShowText(text);
        //    }
        //};

        InputManager.Player.Click.performed += _ =>
        {
            Vector2 screenPos = InputManager.Player.Point.ReadValue<Vector2>();
            Ray ray = Camera.main.ScreenPointToRay(screenPos / 4); // Camera space is quarter of FullHD
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.TryGetComponent<MaskPart>(out var part))
                {
                    var hint = AnalyzeAndGetText(currentMask, part);
                    DialogueManager.Instance.ShowText(hint);
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
        if (GameRunning)
        {
            ProgressTime();
        }
    }

    IEnumerator GameLoop()
    {
        ResetGame();

        Debug.Log($"Initial time ticks: {timeTicksLeft}");

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
            yield return new WaitUntil(() => GameRunning && !mask.inspecting);

            // Done inspeting, now the outcome
            if (mask.CurseLevel > 0)
            {
                // Oops!
                DOTween.Sequence()
                    .Append(Cuts.Flash(Color.red, 1f));

                UIManager.Instance.CreatePopup("Oops!");

                Sanity -= mask.CurseLevel;
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

    private void GenerateRuleSet()
    {
        if (maskRules == null)
        {
            maskRules = new List<(int, MaskRule)>(rulesPerMask * baseMasks.Count);
        }
        else
        {
            maskRules.Clear();
        }

        for (int maskId = 0; maskId < baseMasks.Count; maskId++)
        {
            // The rules for generating mask rules:
            // - At least one key rule per base mask
            // - Only one rule per part X & part Y combo (?)
            // - Part X and part Y must not be the same
            // - Part X is always required, part Y may be disallowed

            var defaultKeyRule = new MaskRule();
            defaultKeyRule.first = (MaskFeature)Random.Range(1, 5);
            defaultKeyRule.second = (MaskFeature)Random.Range(0, 5);
            defaultKeyRule.requireSecond = Utils.FiftyFifty();
            defaultKeyRule.effect = MaskEffect.Key;
            maskRules.Add((maskId, defaultKeyRule));

            for (int i = 1; i < rulesPerMask; i++)
            {
                var newRule = new MaskRule();
                newRule.first = (MaskFeature)Random.Range(1, 5);
                newRule.second = (MaskFeature)Random.Range(0, 5);
                newRule.requireSecond = Utils.FiftyFifty();
                newRule.effect = (MaskEffect)Random.Range(0, 8); // For now, only the most basic effects are used

                maskRules.Add((maskId, newRule));
            }
        }
    }

    //private MaskController GenerateMask()
    //{
    //    var baseMask = baseMasks[Random.Range(0, baseMasks.Count)];
    //    var partsIncluded = 1 + Random.Range(0, 3);
        
    //}

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

            Debug.Log($"Time ticks left: {timeTicksLeft}");

            if (timeTicksLeft <= 0)
            {
                CheckEscape();
            }
        }
    }

    private string AnalyzeAndGetText(MaskController mask, MaskPart part)
    {
        var analyzedFeature = part.part;
        var model = part.GetComponent<MeshRenderer>();

        // Flash the part of the mask visually
        mask.ModelAnimation(model.material);

        var text = GetRandomRelevantRuleText(mask, analyzedFeature);
        return text == null
            ? "There's nothing interesting about this."
            : text;
    }

    private string GetRandomRelevantRuleText(MaskController mask, MaskFeature feature)
    {
        if (feature == MaskFeature.None)
        {
            return null;
        }

        var relevantRules = maskRules
            .Where(r => r.Rule.first == feature || r.Rule.second == feature).ToList();

        if (relevantRules == null || relevantRules.Count == 0)
        {
            return null;
        }

        var randomRule = relevantRules[Random.Range(0, relevantRules.Count)];
        return BuildRuleText(randomRule.Rule);
    }

    private string BuildRuleText(MaskRule rule)
    {
        var ruleText = new StringBuilder();
        ruleText.Append($"If {rule.first} and ");

        if (!rule.requireSecond)
        {
            ruleText.Append($"not ");
        }

        ruleText.AppendLine($"{rule.second}, {rule.effect}");

        return ruleText.ToString();
    }

    public void CheckEscape()
    {
        GameRunning = false;

        Debug.Log("Attempting escape...");

        if (!HasEnoughKeyMasks)
        {
            EndGameInFailure(outOfSanity: false, notEnoughKeyMasks: true);
        }
        else
        {
            Debug.Log("SUCCESS!");
        }

        var totalScore = collectedTotalBaseValue + collectedTotalBonusValue;
        Debug.Log($"Total base value: {collectedTotalBaseValue}");
        Debug.Log($"Total bonus value: {collectedTotalBonusValue}");
        Debug.Log($"Total score: {totalScore}");
    }

    public void EndGameInFailure(bool outOfSanity, bool notEnoughKeyMasks)
    {
        // TODO

        GameRunning = false;

        if (outOfSanity)
        {
            Debug.Log("FAILURE! Out of sanity!");
        }
        else if (notEnoughKeyMasks)
        {
            Debug.Log($"FAILURE! Only {keyMaskCount} out of {requiredKeyMaskCount} key masks collected.");
        }
    }

    public void ResetGame()
    {
        timeTicksLeft = initialTimeTicks;
        elapsedTime = 0;
        sanity = initialSanity;
        collectedTotalBaseValue = 0;
        collectedTotalBonusValue = 0;
        GenerateRuleSet();
        GameRunning = true;
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
