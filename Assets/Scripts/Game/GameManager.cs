using Assets.Scripts.Generic;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
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

    public TextMeshProUGUI baseValueText;

    [InspectorReadOnly] public int collectedTotalBaseValue;
    [InspectorReadOnly] public int collectedTotalBonusValue;

    public List<MaskController> baseMasks;
    public int rulesPerMask = 12;

    [InspectorReadOnly] public List<(int MaskId, MaskRule Rule)> maskRules;

    public Vector3 initialMaskPosition = new Vector3(0f, -12.5f, 28.5f);

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

            currentMask.inspecting = false;
        };

        InputManager.Player.Cleanse.performed += _ =>
        {
            if (!GameRunning)
            {
                return;
            }

            UIManager.Instance.CreatePopup("You cleanse the mask...");
            AudioManager.Instance.PlaySound("event:/SFX/Player/cleanseMask");
            currentMask.CurseLevel--;
        };

        InputManager.Player.Abandon.performed += _ =>
        {

            currentMask.inspecting = false;
            currentMask.discarded = true;
            AudioManager.Instance.PlaySound("event:/SFX/Interactions/sfx_door_break");
            UIManager.Instance.CreatePopup("You destroy the mask...");
        };

        InputManager.Player.Click.performed += _ =>
        {
            Vector2 screenPos = InputManager.Player.Point.ReadValue<Vector2>();
            Ray ray = Camera.main.ScreenPointToRay(screenPos / 4); // Camera space is quarter of FullHD
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.TryGetComponent<MaskPart>(out var part)
                    || hit.transform.parent.TryGetComponent(out part))
                {
                    var hint = AnalyzeAndGetText(currentMask, part);
                    var maskName = currentMask.name;
                    DialogueManager.Instance.ShowText(maskName, hint);
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
        if (GameRunning && currentMask != null)
        {
            ProgressTime();
        }
    }

    IEnumerator GameLoop()
    {
        ResetGame();

        Debug.Log($"Initial time ticks: {timeTicksLeft}");

        var maskX = masksContainer.transform.position.x;
        var movedDist = 0;
        var maskDeltaX = 3;

        bool firstMask = true;

        while (GameRunning)
        {
            var mask = GenerateMask();
            currentMask = mask;
            //mask.transform.position += new Vector3(movedDist, 0, 0);

            if (firstMask)
            {
                // Move to the mask's position
                McTransform.DOMoveX(maskX, duration: 1f);

                firstMask = false;
            }

            // Start inspection the mask and stop to wait until it's done
            baseValueText.text = $"Mask base value: {(mask.id + 1) * 1000} MuskDollars";
            mask.Init();
            mask.Inspect();
            yield return new WaitUntil(() => GameRunning && !mask.inspecting);

            if (mask.discarded)
            {
                // thrashed
            }
            else
            {
                // taken
                if (mask.CurseLevel > 0)
                {
                    DOTween.Sequence()
                        .Append(Cuts.Flash(Color.red, 1f));

                    UIManager.Instance.CreatePopup("There was a curse on the mask!");
                    AudioManager.Instance.PlaySound("event:/SFX/Player/cursedMaskChosen");
                    Sanity -= mask.CurseLevel;
                }
                else
                {
                    // all good
                    AudioManager.Instance.PlaySound("event:/SFX/Player/chooseMask");
                    UIManager.Instance.CreatePopup("Nice...");
                }
            }

            // We don't need the object anymore
            mask.DOKill();
            Destroy(mask.gameObject);

            maskX += maskDeltaX;
            movedDist += maskDeltaX;
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
            // - [NOT USED] Only one rule per part X & part Y combo
            // - Part X and part Y must not be the same
            // - Part X is always required, part Y may be disallowed

            maskRules.Add((maskId, GenerateRule(true)));

            for (int i = 1; i < rulesPerMask; i++)
            {
                maskRules.Add((maskId, GenerateRule(false)));
            }
        }
    }

    private MaskRule GenerateRule(bool forceKeyRule)
    {
        var rule = new MaskRule();
        rule.first = (MaskFeature)Random.Range(1, 5);
        rule.second = (MaskFeature)Random.Range(0, 5);

        if (rule.second == rule.first)
        {
            rule.second = MaskFeature.None;
        }

        rule.requireSecond = Utils.FiftyFifty();

        // For now, only the most basic effects are used
        rule.effect = forceKeyRule ? MaskEffect.Key : (MaskEffect)Random.Range(0, 8);

        rule.text = BuildRuleText(rule);

        return rule;
    }

    private MaskController GenerateMask()
    {
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        var maxNum = numbers.Count;
        var numbersInRandOrder = new List<int>(maxNum);

        for (int i = 0; i < maxNum; i++)
        {
            var possibleNumbers = numbers.Where(n => !numbersInRandOrder.Contains(n)).ToList();
            var num = possibleNumbers[Random.Range(0, possibleNumbers.Count())];
            numbersInRandOrder.Add(num);
        }

        bool[] includedMaskParts = new bool[maxNum];

        var mask = Instantiate(baseMasks[Random.Range(0, baseMasks.Count)], masksContainer.transform);
        var partsNotIncludedCount = 1 + Random.Range(0, maxNum - 2);

        for (int i = 0; i < partsNotIncludedCount; i++)
        {
            var part = mask.GetModelFeature((MaskFeature)numbersInRandOrder[i]);
            part.SetActive(false);
        }

        return mask;
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
        var models = new List<MeshRenderer>();

        var hasRenderer = part.TryGetComponent<MeshRenderer>(out var model);
        if (hasRenderer)
        {
            models.Add(model);
        }
        else
        {
            var childRends = part.GetComponentsInChildren<MeshRenderer>();
            if (childRends != null || childRends.Length > 0)
            {
                models.AddRange(childRends);
            }
            else
            {
                return string.Empty;
            }
        }

        // Flash the part of the mask visually
        foreach (var individualModel in models)
        {
            mask.ModelAnimation(individualModel.material);
        }

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
        return randomRule.Rule.text;
    }

    private string BuildRuleText(MaskRule rule)
    {
        var ruleText = new StringBuilder();

        if (rule.RequireJustFirst)
        {
            ruleText.Append($"If {rule.first}, ");
        }
        else
        {
            ruleText.Append($"If {rule.first} and ");

            if (!rule.requireSecond)
            {
                ruleText.Append($"no ");
            }

            ruleText.Append($"{rule.second}, ");
        }

        ruleText.AppendLine($"{rule.effect}");

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
