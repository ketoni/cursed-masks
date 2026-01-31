using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Yarn.Unity;

using Cuts = CutsceneManager;

public class InteractionContext
{
    // A data class to hold information about both sides of an interaction
    // and also about the associated quest, if any
    public Interactor interactor;
    public Interactable interactable;
}

public class DialogueManager : Singleton<DialogueManager>
{
    [InspectorReadOnly] public string sceneFirstYarnNode;
    [HideInInspector] public bool skipInitialDialogue;
    [SerializeField] DialogueView dialogueView;
    [SerializeField] Image portraitImage;
    [SerializeField] public TMP_FontAsset defaultFont;

    public Image nextButtonUI;

    DialogueRunner Yarn => GetComponent<DialogueRunner>();
    Dictionary<string, Character> characters = new();
    public InteractionContext Context { get; private set; }
    public static Interactable Interactable => Instance.Context.interactable;
    private Character previousCharacter;

    public const string defaultFontAttribute = "normal";

    public bool IsDialogueRunning => Yarn.IsDialogueRunning;
    public bool canHaveDialogueControls = true;
    public bool useOnlyDefaultFont;

    private void Awake()
    {
        // Check Yarn runner
        if (Yarn == null)
        {
            Debug.LogError("DialogueManager couldn't find Yarn runner!");
            return;
        }

        if (defaultFont == null)
        {
            Debug.LogWarning("Default dialogue font has not been set!");
        }

        // Attach dialogue system listeners
        Yarn.onDialogueStart.AddListener(new UnityAction(OnDialogueStart));
        Yarn.onDialogueComplete.AddListener(new UnityAction(OnDialogueComplete));

        LoadCharacters();
    }

    private void Start()
    {
        if (sceneFirstYarnNode != "")
        {
            if (skipInitialDialogue)
            {
                Debug.LogWarning("Skipping intial scene dialogue!");
            }
            else
            {
                StartDialogue(sceneFirstYarnNode);
            }
        }
    }

    private void Update()
    {
        var tempColor = nextButtonUI.color;
        tempColor.a = 1;
        nextButtonUI.color = tempColor;
    }

    public override void OnSceneLoad(SceneContext context)
    {
        Yarn.SetProject(context.yarnProject);
        sceneFirstYarnNode = context.firstYarnNode;
    }

    private void OnDialogueStart()
    {
        UIManager.Instance.dialogueView.Show();
        if (canHaveDialogueControls)
        {
            InputManager.Instance.Push(InputManager.Dialogue);
        }
    }

    private void OnDialogueComplete()
    {
        if (canHaveDialogueControls)
        {
            InputManager.Instance.Remove(InputManager.Dialogue);
        }
        UIManager.Instance.dialogueView.Hide();
        canHaveDialogueControls = true;

        // Disables dialogue variants (e.g. portraits) not carry over to next dialogue
        previousCharacter = null;
    }

    public void NextLine(bool enableControls = false)
    {
        dialogueView.UserRequestedViewAdvancement();
        if (enableControls)
        {
            InputManager.Dialogue.Enable();
        }
    }

    private void LoadCharacters()
    {
        var data = Resources.LoadAll<Character>("Characters");
        if (data.Length == 0)
        {
            Debug.LogError($"Loaded 0 Characters!");
            return;
        }

        // Populate the dictionary
        foreach (var c in data)
        {
            if (characters.ContainsKey(c.characterName))
            {
                Debug.LogError($"Duplicate Character name '{c.characterName}' encountered!");
            }
            characters[c.characterName] = c;
        }

    }

    public Character FindCharacter(string name)
    {
        // Try exact match
        if (characters.ContainsKey(name))
        {
            return characters[name];
        }

        // Find closest character by name with Levenshthein distance
        Character closestMatch = null;
        int minDistance = int.MaxValue;
        foreach (var kvp in characters)
        {
            var chara = kvp.Value;
            int distance = StringMatcher.LevenshteinDistance(name, chara.characterName);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestMatch = chara;
            }
        }
        var foundName = closestMatch.characterName;
        var foundObject = closestMatch.name;
        Debug.LogWarning($"No exact match for Character '{name}', but found '{foundName} ({foundObject})'");
        return closestMatch;
    }

    public void UpdateCharacterPortrait(Character character, LocalizedLine line)
    {
        // Determine current character and the portrait variant name,
        // if one is defined in the provided line's metadata
        bool requested = line.ParseTagValue("face", out string variantName);
        if (character == null)
        {
            // Character may not always defined, which may be the case if we are showing
            // informational messages to the player using the dialogue view.
            // If so, we simply hide the (previous) portrait image.
            portraitImage.enabled = false;
            if (requested)
            {
                Debug.LogWarning("No current Character but a portrait variant was requested");
            }
            return;
        }
        portraitImage.enabled = true;

        // We are showing a portrait,
        // Is the same character speaking as last time?
        var sameCharacterAsBefore = false;
        if (previousCharacter != null && character == previousCharacter)
        {
            sameCharacterAsBefore = true;
        }
        previousCharacter = character;
        // If so and no portrait variant has not been requested, we don't have to continue
        // as the portrait will just remain unchanged
        if (sameCharacterAsBefore && !requested)
        {
            return;
        }

        // Character changed or variant requested: Load the new sprite
        var fileVariant = $"Portrait_{variantName.FirstCharacterToUpper()}";
        var resourcePath = character.AssetPath("Dialogue", fileVariant);
        var asset = Resources.Load<Sprite>(resourcePath);
        if (asset == null)
        {
            Debug.LogError($"Failed to load portrait Sprite from '{resourcePath}'");
            Debug.LogWarning("Current portrait remains unchanged");
            return;
        }
        portraitImage.sprite = asset;
        portraitImage.SetNativeSize(); // Pixel perfect
    }

    internal void SetDialogueStyle(Character character, LocalizedLine line)
    {
        // Adjusts the text container contents based on given character and line
        // The character can be null in case of e.g. narration and characters might not define a custom font.
        var font = defaultFont;
        if (character != null && character.dialogueFont != null && !useOnlyDefaultFont)
        {
            font = character.dialogueFont;
        }
        dialogueView.textContainer.font = font;

        // Change text style if requested on the line
        if (!line.ParseTagValue("font", out string style))
        {
            return;
        }
        switch (style)
        {
            case "bold":
                dialogueView.textContainer.fontStyle = FontStyles.Bold;
                return;
            case "italic":
                dialogueView.textContainer.fontStyle = FontStyles.Italic;
                return;
            case defaultFontAttribute:
                break;
            case "":
                Debug.LogError("Empty dialogue font style string");
                break;
            default:
                Debug.LogWarning($"Unsupported dialogue font style '{style}'");
                break;
        }
        dialogueView.textContainer.fontStyle = FontStyles.Normal;
    }

    public void SetVariable<T>(string name, T value)
    {
        if (!Yarn.VariableStorage.TryGetValue<T>(name, out _))
        {
            Debug.LogWarning($"Defining a new ({typeof(T).Name}) Yarn variable '{name}'");
        }
        if (value is float floatvalue)
        {
            Yarn.VariableStorage.SetValue(name, floatvalue);
        }
        else if (value is bool boolvalue)
        {
            Yarn.VariableStorage.SetValue(name, boolvalue);
        }
        else if (value is string stringvalue)
        {
            Yarn.VariableStorage.SetValue(name, stringvalue);
        }
        else
        {
            Debug.LogError($"Yarn does not support variables of type '{typeof(T).Name}'");
        }
    }

    public T GetVariable<T>(string name)
    {
        if (!Yarn.VariableStorage.TryGetValue(name, out T value))
        {
            Debug.LogError($"Invalid type '{typeof(T).Name}' or undefined Yarn variable '{name}'");
            return default;
        }
        return value;
    }

    internal void InitInstanceVariable(string variableName, Interactable instance)
    {
        // Create a variable used for separate instances of Interactables
        var fullname = variableName + instance.Identifier;
        Yarn.VariableStorage.SetValue(fullname, false);
    }

    [YarnCommand("Pause")]
    public static void PauseDialogue()
    {
        InputManager.Dialogue.Disable();
    }

    [YarnCommand("noop")]
    public static void NoOpLine()
    {
        // A "no op" command to handle empty expression selection choices
    }

    [YarnCommand("SetInstanceVariable")]
    public static void SetInstanceBoolean(string variableName, bool value)
    {
        // Set a variable used for separate instances of Interactables.
        // Currently only supports booleans.
        var identifier = Instance.Context.interactable.Identifier;
        Instance.SetVariable(variableName + identifier, value);
    }

    // Public interface for instance variables, for use outside of Yarn
    public void SetInstaceVariable(Interactable interactable, string variableName, bool value)
    {
        Instance.SetVariable(variableName + interactable.Identifier, value);
    }

    [YarnFunction("InstanceVariable")]
    public static bool GetInstanceBoolean(string name)
    {
        // Get a variable used for separate instances of Interactables.
        // Currently only supports booleans.
        if (Instance.Context == null)
        {
            Debug.LogError("Can't get instance variable: No context!");
            return false;
        }
        var identifier = Instance.Context.interactable.Identifier;
        return Instance.GetVariable<bool>(name + identifier);
    }

    [YarnCommand("DisableObject")]
    public static void DisableParent()
    {
        // Disables the attached GameObject, ensuring it's also removed
        // from the Interactor's queue
        var interactable = Instance.Context.interactable;
        Instance.Context.interactor.Dequeue(interactable);
        interactable.gameObject.SetActive(false);
    }

    public void StartDialogue(string node)
    {
        Yarn.StartDialogue(node);
    }

    public void StopDialogue()
    {
        Debug.Log("Stopping Yarn Runner");
        Yarn.Stop();
    }

    public static void AfterDialogue(System.Action callback)
    {
        Instance.StartCoroutine(nameof(WaitForDialogueToEnd));

        IEnumerator WaitForDialogueToEnd()
        {
            while (Instance.IsDialogueRunning)
            {
                yield return null;
            }
            callback?.Invoke();
        }
    }

    internal void ForceJump(string node)
    {
        // This method circumvents the need for us to always provide a selected option
        // while the options view is waiting for one. Essentially allows us to do
        // default Expressions without any major trickery in .yarn files
        // We stop the dialogue and immediately start on a new node, preserving the context.
        var c = Context;
        StopDialogue();

        IEnumerator WaitForJump()
        {
            while (IsDialogueRunning)
            {
                yield return null;
            }
            SetContext(c);
            StartDialogue(node);
        }
        StartCoroutine(WaitForJump());
    }

    internal string CurrentSpeakerName()
    {
        if (Yarn.VariableStorage.TryGetValue("$npcName", out string name))
        {
            return $"'{name}'";
        }
        return "(error)";
    }

    public void Interrupt()
    {
        dialogueView.requestInterrupt();
    }

    internal void SetContext(InteractionContext c)
    {
        // Stores the interaction context and defines the npc name yarn variable
        // The variable is used in default interactions
        Context = c;
        if (Context.interactable.type == Interactable.InteractType.NPC)
        {
            SetVariable("$npcName", Context.interactable.character.name);
        }
    }

    [YarnCommand("ShakeEffect")]
    public static void YarnShakeEffect()
    {
        Cuts.Sequence(disableControls: false)
            .Append(Cuts.Shake())
            .Join(Cuts.ShakeHUD());
    }

    internal void ShowText(string analysisText)
    {
        // First stop normal dialogue, then start custom dialogue,
        // which stops to wait for lines to change

        //StopDialogue(); we should not need this since analysis can only be done outside normal dialogue?
        dialogueView.waitForTextChange = true;
        canHaveDialogueControls = false;

        // Future calls should simply advance the currently running dialogue
        if (!IsDialogueRunning)
        {
            StartDialogue("Analysis_Base");
        }
        dialogueView.AppendText(analysisText);
        // forcibly take away dialogue controls until we are done?
    }
}