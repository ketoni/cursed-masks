using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using System;
using System.Linq;
using TMPro;
using Yarn.Markup;
using UnityEngine.InputSystem;
using YarnEffects = Yarn.Unity.Effects;
using DG.Tweening;
using System.Numerics;
using System.Drawing;

// Based on Yarn's LineView implementation


/// <summary>
/// A Dialogue View that presents lines of dialogue, using Unity UI
/// elements.
/// </summary>
public class DialogueView : DialogueViewBase, IControlSchema
{
    public static readonly Dictionary<string, string> knownTagsDefaults = new()
    { 
        { "face", "default" },
        { "font", DialogueManager.defaultFontAttribute},
    };

    public bool waitForTextChange;

    DialogueEffects lineEffects;

    /// <summary>
    /// The canvas group that contains the UI elements used by this Line
    /// View.
    /// </summary>
    /// <remarks>
    /// If <see cref="useFadeEffect"/> is true, then the alpha value of this
    /// <see cref="CanvasGroup"/> will be animated during line presentation
    /// and dismissal.
    /// </remarks>
    /// <seealso cref="useFadeEffect"/>
    internal CanvasGroup canvasGroup;

    /// <summary>
    /// Controls whether the line view should fade in when lines appear, and
    /// fade out when lines disappear.
    /// </summary>
    /// <remarks><para>If this value is <see langword="true"/>, the <see
    /// cref="canvasGroup"/> object's alpha property will animate from 0 to
    /// 1 over the course of <see cref="fadeInTime"/> seconds when lines
    /// appear, and animate from 1 to zero over the course of <see
    /// cref="fadeOutTime"/> seconds when lines disappear.</para>
    /// <para>If this value is <see langword="false"/>, the <see
    /// cref="canvasGroup"/> object will appear instantaneously.</para>
    /// </remarks>
    /// <seealso cref="canvasGroup"/>
    /// <seealso cref="fadeInTime"/>
    /// <seealso cref="fadeOutTime"/>
    [SerializeField]
    internal bool useFadeEffect = true;

    /// <summary>
    /// The time that the fade effect will take to fade lines in.
    /// </summary>
    /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
    /// <see langword="true"/>.</remarks>
    /// <seealso cref="useFadeEffect"/>
    [SerializeField]
    [Min(0)]
    internal float fadeInTime = 0.25f;

    /// <summary>
    /// The time that the fade effect will take to fade lines out.
    /// </summary>
    /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
    /// <see langword="true"/>.</remarks>
    /// <seealso cref="useFadeEffect"/>
    [SerializeField]
    [Min(0)]
    internal float fadeOutTime = 0.05f;

    /// <summary>
    /// The <see cref="TextMeshProUGUI"/> object that displays the text of
    /// dialogue lines.
    /// </summary>
    [SerializeField]
    internal TextMeshProUGUI textContainer = null;

    /// <summary>
    /// Controls whether the <see cref="textContainer"/> object will show the
    /// character name present in the line or not.
    /// </summary>
    /// <remarks>
    /// <para style="note">This value is only used if <see
    /// cref="characterNameText"/> is <see langword="null"/>.</para>
    /// <para>If this value is <see langword="true"/>, any character names
    /// present in a line will be shown in the <see cref="textContainer"/>
    /// object.</para>
    /// <para>If this value is <see langword="false"/>, character names will
    /// not be shown in the <see cref="textContainer"/> object.</para>
    /// </remarks>
    [SerializeField]
    [UnityEngine.Serialization.FormerlySerializedAs("showCharacterName")]
    internal bool showCharacterNameInLineView = false;

    /// <summary>
    /// The <see cref="TextMeshProUGUI"/> object that displays the character
    /// names found in dialogue lines.
    /// </summary>
    /// <remarks>
    /// If the <see cref="LineView"/> receives a line that does not contain
    /// a character name, this object will be left blank.
    /// </remarks>
    [SerializeField]
    internal TextMeshProUGUI characterNameText = null;

    /// <summary>
    /// The gameobject that holds the <see cref="characterNameText"/> textfield.
    /// </summary>
    /// <remarks>
    /// This is needed in situations where the character name is contained within an entirely different game object.
    /// Most of the time this will just be the same gameobject as <see cref="characterNameText"/>.
    /// </remarks>
    [SerializeField] internal GameObject characterNameContainer = null;

    /// <summary>
    /// Controls whether the text of <see cref="textContainer"/> should be
    /// gradually revealed over time.
    /// </summary>
    /// <remarks><para>If this value is <see langword="true"/>, the <see
    /// cref="textContainer"/> object's <see
    /// cref="TMP_Text.maxVisibleCharacters"/> property will animate from 0
    /// to the length of the text, at a rate of <see
    /// cref="defaultSpeechSpeed"/> letters per second when the line
    /// appears. <see cref="onSymbolTyped"/> is called for every new
    /// character that is revealed.</para>
    /// <para>If this value is <see langword="false"/>, the <see
    /// cref="textContainer"/> will all be revealed at the same time.</para>
    /// <para style="note">If <see cref="useFadeEffect"/> is <see
    /// langword="true"/>, the typewriter effect will run after the fade-in
    /// is complete.</para>
    /// </remarks>
    /// <seealso cref="textContainer"/>
    /// <seealso cref="onSymbolTyped"/>
    /// <seealso cref="defaultSpeechSpeed"/>
    [SerializeField]
    internal bool useTypewriterEffect = false;

    /// <summary>
    /// A Unity Event that is called each time a character is revealed
    /// during a typewriter effect.
    /// </summary>
    /// <remarks>
    /// This event is only invoked when <see cref="useTypewriterEffect"/> is
    /// <see langword="true"/>.
    /// </remarks>
    /// <seealso cref="useTypewriterEffect"/>
    [SerializeField]
    internal UnityEngine.Events.UnityEvent onSymbolTyped;

    /// <summary>
    /// A Unity Event that is called when a pause inside of the typewriter effect occurs.
    /// </summary>
    /// <remarks>
    /// This event is only invoked when <see cref="useTypewriterEffect"/> is <see langword="true"/>.
    /// </remarks>
    /// <seealso cref="useTypewriterEffect"/>
    [SerializeField] internal UnityEngine.Events.UnityEvent onPauseStarted;
    /// <summary>
    /// A Unity Event that is called when a pause inside of the typewriter effect finishes and the typewriter has started once again.
    /// </summary>
    /// <remarks>
    /// This event is only invoked when <see cref="useTypewriterEffect"/> is <see langword="true"/>.
    /// </remarks>
    /// <seealso cref="useTypewriterEffect"/>
    [SerializeField] internal UnityEngine.Events.UnityEvent onPauseEnded;

    /// <summary>
    /// The number of characters per second that should appear during a
    /// typewriter effect.
    /// </summary>
    /// <seealso cref="useTypewriterEffect"/>
    [SerializeField]
    [Min(1)]
    internal float defaultSpeechSpeed = 1f;

    /// <summary>
    /// The amount of time to wait after any line
    /// </summary>
    [SerializeField]
    [Min(0)]
    internal float holdTime = 1f;

    /// <summary>
    /// Controls whether this Line View will wait for user input before
    /// indicating that it has finished presenting a line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If this value is true, the Line View will not report that it has
    /// finished presenting its lines. Instead, it will wait until the <see
    /// cref="UserRequestedViewAdvancement"/> method is called.
    /// </para>
    /// <para style="note"><para>The <see cref="DialogueRunner"/> will not
    /// proceed to the next piece of content (e.g. the next line, or the
    /// next options) until all Dialogue Views have reported that they have
    /// finished presenting their lines. If a <see cref="LineView"/> doesn't
    /// report that it's finished until it receives input, the <see
    /// cref="DialogueRunner"/> will end up pausing.</para>
    /// <para>
    /// This is useful for games in which you want the player to be able to
    /// read lines of dialogue at their own pace, and give them control over
    /// when to advance to the next line.</para></para>
    /// </remarks>
    [SerializeField]
    internal bool autoAdvance = false;

    [SerializeField]
    internal MarkupPalette palette;

    public GameObject inputIconKeyboard;
    public GameObject inputIconGamepad;

    [Header("Customization to dialogue display")]
    public GameObject dialogueBox;
    public GameObject characterPortrait;
    public GameObject backdropBG;
    public GameObject dialogueText;

    /// <summary>
    /// The current <see cref="LocalizedLine"/> that this line view is
    /// displaying.
    /// </summary>
    LocalizedLine currentLine = null;

    /// <summary>
    /// A stop token that is used to interrupt the current animation.
    /// </summary>
    YarnEffects.CoroutineInterruptToken currentStopToken = new YarnEffects.CoroutineInterruptToken();

    Character currentCharacter;

    // Whether to pause the dialogue typewriter
    private bool isPaused;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        lineEffects = new DialogueEffects(textContainer);
    }

    private void Start()
    {
        Hide();
        InputManager.Dialogue.Advance.performed += _ => {
            //if (BattleManager.Instance.inBattle) return;
            UserRequestedViewAdvancement();
        };
        onSymbolTyped.AddListener(() => PlayTypewriterSound());
    }

    void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ON_TEXT_CHANGED);
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ON_TEXT_CHANGED);
    }

    void ON_TEXT_CHANGED(UnityEngine.Object obj)
    {
        // If the dialogue text changes we must re-apply visual effects to keep them consistent.
        if (obj == textContainer && currentLine != null)
        {
            lineEffects.shouldUpdateCache = true;
            lineEffects.Apply(currentLine.AttributesToEffects());
        }
    }

    public void Show()
    {
        canvasGroup.alpha = 1;
        isPaused = false;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0;
        isPaused = true;
    }

        // Does DOTween Punch effect to an object
    public void PunchIcon(GameObject obj)
    {
        UnityEngine.Vector3 v;
        v = new UnityEngine.Vector3(0f, 10f, 0f);
        obj.transform.DOPunchPosition(v, 0.3f, 5, 1f);
    }

    // Hides a object
    public void HideIcon(UnityEngine.UI.Image img)
    {
        img.enabled = false;
    }

    // Reveals an object
    public void RevealIcon(UnityEngine.UI.Image img)
    {
        img.enabled = true;
    }

    public bool CurrentSpeaker(out string name)
    {
        name = null;
        if (currentLine != null)
        {
            name = currentLine.CharacterName;
        }
        return name != null;
    }

    /// <inheritdoc/>
    public override void DismissLine(Action onDismissalComplete)
    {
        currentLine = null;

        StartCoroutine(DismissLineInternal(onDismissalComplete));
    }

    private IEnumerator DismissLineInternal(Action onDismissalComplete)
    {
        // disabling interaction temporarily while dismissing the line
        // we don't want people to interrupt a dismissal
        //var interactable = canvasGroup.interactable;
        //canvasGroup.interactable = false;

        // If we're using a fade effect, run it, and wait for it to finish.
        if (useFadeEffect)
        {
            yield return StartCoroutine(YarnEffects.FadeAlpha(canvasGroup, 1, 0, fadeOutTime, currentStopToken));
            currentStopToken.Complete();
        }
        
        //canvasGroup.alpha = 0;
        //canvasGroup.blocksRaycasts = false;
        // turning interaction back on, if it needs it
        //canvasGroup.interactable = interactable;
        
        if (onDismissalComplete != null)
        {
            onDismissalComplete();
        }
    }

    /// <inheritdoc/>
    public override void InterruptLine(LocalizedLine dialogueLine, Action onInterruptLineFinished)
    {
        currentLine = dialogueLine;

        // Cancel all coroutines that we're currently running. This will
        // stop the RunLineInternal coroutine, if it's running.
        StopAllCoroutines();
        
        // for now we are going to just immediately show everything
        // later we will make it fade in
        textContainer.gameObject.SetActive(true);
        canvasGroup.gameObject.SetActive(true);

        int length;

        if (characterNameText == null)
        {
            if (showCharacterNameInLineView)
            {
                textContainer.text = dialogueLine.Text.Text;
                length = dialogueLine.Text.Text.Length;
            }
            else
            {
                textContainer.text = dialogueLine.TextWithoutCharacterName.Text;
                length = dialogueLine.TextWithoutCharacterName.Text.Length;
            }
        }
        else
        {
            characterNameText.text = dialogueLine.CharacterName;
            textContainer.text = dialogueLine.TextWithoutCharacterName.Text;
            length = dialogueLine.TextWithoutCharacterName.Text.Length;
        }

        // Show the entire line's text immediately.
        textContainer.maxVisibleCharacters = length;

        // Make the canvas group fully visible immediately, too.
        canvasGroup.alpha = 1;

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        onInterruptLineFinished();
    }

    /// <inheritdoc/>
    public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
    {
        // Stop any coroutines currently running on this line view (for
        // example, any other RunLine that might be running)
        StopAllCoroutines();

        // Begin running the line as a coroutine.
        StartCoroutine(RunLineInternal(dialogueLine, onDialogueLineFinished));
    }

    private IEnumerator LineEffects(Dictionary<string, List<DialogueEffects.TextEffect>> effects)
    {
        // A coroutine to apply any animated effects on the current line until stopped.
        if (currentLine == null)
        {
            Debug.LogError("Tried to start dialogue line effects routine without active line");
            yield break;
        }
        while (true)
        {
            lineEffects.Apply(effects);
            yield return null;
        }
    }

    void PlayTypewriterSound()
    {
        if (currentCharacter != null && currentCharacter.speechSoundPath != "")
        {
            FMODUnity.RuntimeManager.PlayOneShot(currentCharacter.speechSoundPath);
        }
        else
        {
            FMODUnity.RuntimeManager.PlayOneShot(Sounds.UI.Text.Typewriter);
        }
    }

    private IEnumerator RunLineInternal(LocalizedLine dialogueLine, Action onDialogueLineFinished)
    {
        IEnumerator PresentLine()
        {
            textContainer.gameObject.SetActive(true);
            canvasGroup.gameObject.SetActive(true);

            MarkupParseResult text = dialogueLine.TextWithoutCharacterName;
            if (characterNameContainer != null && characterNameText != null)
            {
                // we are set up to show a character name, but there isn't one
                // so just hide the container
                if (string.IsNullOrWhiteSpace(dialogueLine.CharacterName))
                {
                    characterNameContainer.SetActive(false);
                }
                else
                {
                    // we have a character name text view, show the character name
                    characterNameText.text = dialogueLine.CharacterName;
                    characterNameContainer.SetActive(true);
                }
            }
            else
            {
                // We don't have a character name text view. Should we show
                // the character name in the main text view?
                if (showCharacterNameInLineView)
                {
                    // Yep! Show the entire text.
                    text = dialogueLine.Text;
                }
            }

            // if we have a palette file need to add those colours into the text
            if (palette != null)
            {
                textContainer.text = LineView.PaletteMarkedUpText(text, palette);
            }
            else
            {
                textContainer.text = LineView.AddLineBreaks(text);
            }

            if (useTypewriterEffect)
            {
                // If we're using the typewriter effect, hide all of the
                // text before we begin any possible fade (so we don't fade
                // in on visible text).
                textContainer.maxVisibleCharacters = 0;
            }
            else
            {
                // Ensure that the max visible characters is effectively
                // unlimited.
                textContainer.maxVisibleCharacters = int.MaxValue;
            }

            // If we're using the fade effect, start it, and wait for it to
            // finish.
            if (useFadeEffect)
            {
                yield return StartCoroutine(YarnEffects.FadeAlpha(canvasGroup, 0, 1, fadeInTime, currentStopToken));
                if (currentStopToken.WasInterrupted)
                {
                    // The fade effect was interrupted. Stop this entire
                    // coroutine.
                    yield break;
                }
            }

            // If we're using the typewriter effect, start it, and wait for
            // it to finish.
            if (useTypewriterEffect)
            {
                var pauseList = new List<(int position, float length)>(LineView.GetPauseDurationsInsideLine(text));
                for (int i = 0; i < textContainer.text.Length - 1; i++)
                {
                    var symbol = textContainer.text[i];
                    var nextPos = i+1;
                    switch (symbol)
                    {
                        case '.' or '!' or '?':
                            // If we repeat these characters only pause on the last
                            if (".!?".Contains(textContainer.text[nextPos])) continue;
                            pauseList.Add((nextPos, 0.7f));
                            break;
                        case ',' or ':':
                            pauseList.Add((nextPos, 0.25f));
                            break;
                    }
                }
                pauseList.Sort((a, b) => b.position.CompareTo(a.position));
                var pauses = new Stack<(int, float)>(pauseList); 

                // setting the canvas all back to its defaults because if we didn't also fade we don't have anything visible
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;

                var typewriterSpeed = defaultSpeechSpeed;
                if (currentCharacter != null)
                {
                    typewriterSpeed = currentCharacter.speechSpeed;
                }

                yield return StartCoroutine(PausableTypewriter(
                    textContainer,
                    typewriterSpeed,
                    () => onSymbolTyped.Invoke(),
                    () => onPauseStarted.Invoke(),
                    () => onPauseEnded.Invoke(),
                    pauses,
                    currentStopToken
                ));

                if (currentStopToken.WasInterrupted)
                {
                    // The typewriter effect was interrupted. Stop this
                    // entire coroutine.
                    yield break;
                }
            }
        }
        currentLine = dialogueLine;

        // Define current Character based on who's currently speaking
        if (CurrentSpeaker(out var speakerName))
        {
            currentCharacter = DialogueManager.Instance.FindCharacter(speakerName);
        }
        else
        {
            currentCharacter = null;
        }

        DialogueManager.Instance.UpdateCharacterPortrait(currentCharacter, currentLine);

        // Start line effects coroutine if we have any for the current line.
        var effects = currentLine.AttributesToEffects();
        if (effects.Count > 0)
        {
            StartCoroutine(LineEffects(effects));
        }

        // Set dialogue font style(s)
        DialogueManager.Instance.SetDialogueStyle(currentCharacter, currentLine);

        // Run any presentations as a single coroutine. If this is stopped,
        // which UserRequestedViewAdvancement can do, then we will stop all
        // of the animations at once.
        yield return StartCoroutine(PresentLine());

        currentStopToken.Complete();

        // All of our text should now be visible.
        textContainer.maxVisibleCharacters = int.MaxValue;

        // Our view should at be at full opacity.
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // If we have a hold time, wait that amount of time, and then
        // continue.
        if (holdTime > 0)
        {
            yield return new WaitForSeconds(holdTime);
        }

        if (autoAdvance == false)
        {
            // The line is now fully visible, and we've been asked to not
            // auto-advance to the next line. Stop here, and don't call the
            // completion handler - we'll wait for a call to
            // UserRequestedViewAdvancement, which will interrupt this
            // coroutine.
            yield break;
        }

        // Our presentation is complete; call the completion handler.
        onDialogueLineFinished();
    }

    /// <inheritdoc/>
    public override void UserRequestedViewAdvancement()
    {
        // We received a request to advance the view. If we're in the middle of
        // an animation, skip to the end of it. If we're not current in an
        // animation, interrupt the line so we can skip to the next one.

        // we have no line, so the user just mashed randomly
        if (currentLine == null)
        {
            return;
        }

        // we may want to change this later so the interrupted
        // animation coroutine is what actually interrupts
        // for now this is fine.
        // Is an animation running that we can stop?
        if (currentStopToken.CanInterrupt) 
        {
            // Stop the current animation, and skip to the end of whatever
            // started it.
            currentStopToken.Interrupt();
        }
        else
        {
            // No animation is now running. Signal that we want to
            // interrupt the line instead.
            requestInterrupt?.Invoke();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// If a line is still being shown dismisses it.
    /// </remarks>
    public override void DialogueComplete()
    {
        // do we still have a line lying around?
        if (currentLine != null)
        {
            currentLine = null;
            StopAllCoroutines();
            StartCoroutine(DismissLineInternal(null));
        }
    }

    /// <summary>
    /// Applies the <paramref name="palette"/> to the line based on it's markup.
    /// </summary>
    /// <remarks>
    /// This is static so that other dialogue views can reuse this code.
    /// While this is simplistic it is useful enough that multiple pieces might well want it.
    /// </remarks>
    /// <param name="line">The parsed marked up line with it's attributes.</param>
    /// <param name="palette">The palette mapping attributes to colours.</param>
    /// <param name="applyLineBreaks">If the [br /] marker is found in the line should this be replaced with a line break?</param>
    /// <returns>A TMP formatted string with the palette markup values injected within.</returns>
    public static string PaletteMarkedUpText(MarkupParseResult line, MarkupPalette palette, bool applyLineBreaks = true)
    {
        string lineOfText = line.Text;
        line.Attributes.Sort((a, b) => (b.Position.CompareTo(a.Position)));
        foreach (var attribute in line.Attributes)
        {
            // we have a colour that matches the current marker
            UnityEngine.Color markerColour;
            if (palette.ColorForMarker(attribute.Name, out markerColour))
            {
                // we use the range on the marker to insert the TMP <color> tags
                // not the best approach but will work ok for this use case
                lineOfText = lineOfText.Insert(attribute.Position + attribute.Length, "</color>");
                lineOfText = lineOfText.Insert(attribute.Position, $"<color=#{ColorUtility.ToHtmlStringRGB(markerColour)}>");
            }

            if (applyLineBreaks && attribute.Name == "br")
            {
                lineOfText = lineOfText.Insert(attribute.Position, "<br>");
            }
        }
        return lineOfText;
    }

    public static string AddLineBreaks(Yarn.Markup.MarkupParseResult line)
    {
        string lineOfText = line.Text;
        line.Attributes.Sort((a, b) => (b.Position.CompareTo(a.Position)));
        foreach (var attribute in line.Attributes.Where(a => a.Name == "br"))
        {
            // we then replace the marker with the tmp <br>
            lineOfText = lineOfText.Insert(attribute.Position, "<br>");
        }
        return lineOfText;
    }

    /// <summary>
    /// Creates a stack of typewriter pauses to use to temporarily halt the typewriter effect.
    /// </summary>
    /// <remarks>
    /// This is intended to be used in conjunction with the <see cref="YarnEffects.PausableTypewriter"/> effect.
    /// The stack of tuples created are how the typewriter effect knows when, and for how long, to halt the effect.
    /// <para>
    /// The pause duration property is in milliseconds but all the effects code assumes seconds
    /// So here we will be dividing it by 1000 to make sure they interconnect correctly.
    /// </para>
    /// </remarks>
    /// <param name="line">The line from which we covet the pauses</param>
    /// <returns>A stack of positions and duration pause tuples from within the line</returns>
    public static Stack<(int position, float duration)> GetPauseDurationsInsideLine(Yarn.Markup.MarkupParseResult line)
    {
        var pausePositions = new Stack<(int, float)>();
        var label = "pause";
        
        // sorting all the attributes in reverse positional order
        // this is so we can build the stack up in the right positioning
        var attributes = line.Attributes;
        attributes.Sort((a, b) => (b.Position.CompareTo(a.Position)));
        foreach (var attribute in line.Attributes)
        {
            // if we aren't a pause skip it
            if (attribute.Name != label)
            {
                continue;
            }

            // did they set a custom duration or not, as in did they do this:
            //     Alice: this is my line with a [pause = 1000 /]pause in the middle
            // or did they go:
            //     Alice: this is my line with a [pause /]pause in the middle
            if (attribute.Properties.TryGetValue(label, out Yarn.Markup.MarkupValue value))
            {
                // depending on the property value we need to take a different path
                // this is because they have made it an integer or a float which are roughly the same
                // note to self: integer and float really ought to be convertible...
                // but they also might have done something weird and we need to handle that
                switch (value.Type)
                {
                    case Yarn.Markup.MarkupValueType.Integer:
                        float duration = value.IntegerValue;
                        pausePositions.Push((attribute.Position, duration / 1000));
                        break;
                    case Yarn.Markup.MarkupValueType.Float:
                        pausePositions.Push((attribute.Position, value.FloatValue / 1000));
                        break;
                    default:
                        Debug.LogWarning($"Pause property is of type {value.Type}, which is not allowed. Defaulting to one second.");
                        pausePositions.Push((attribute.Position, 1));
                        break;
                }
            }
            else
            {
                // they haven't set a duration, so we will instead use the default of one second
                pausePositions.Push((attribute.Position, 1));
            }
        }
        return pausePositions;
    }

    // Change the view's icons for the given device
    public void ChangeControlSchema(InputDevice device)
    {
        // Change UI Icon to gamepad
        if (device is Gamepad)
        {
           inputIconKeyboard.gameObject.SetActive(false);
           inputIconGamepad.gameObject.SetActive(true);
        }
        // Change UI Icon to Keyboard/Mouse
        else if (device is Keyboard)
        {
           inputIconGamepad.gameObject.SetActive(false);
           inputIconKeyboard.gameObject.SetActive(true);
        }
    }

    /// Based on Yarn.Unity.Effects.PauseableTypewriter
    /// <summary>
    /// A coroutine that gradually reveals the text in a <see cref="TextMeshProUGUI"/> object over time.
    /// </summary>
    /// <remarks>
    /// <para>This method works by adjusting the value of the <paramref name="text"/> parameter's <see cref="TextMeshProUGUI.maxVisibleCharacters"/> property. This means that word wrapping will not change half-way through the presentation of a word.</para>
    /// <para style="note">Depending on the value of <paramref name="lettersPerSecond"/>, <paramref name="onCharacterTyped"/> may be called multiple times per frame.</para>
    /// <para>Due to an internal implementation detail of TextMeshProUGUI, this method will always take at least one frame to execute, regardless of the length of the <paramref name="text"/> parameter's text.</para>
    /// </remarks>
    /// <param name="text">A TextMeshProUGUI object to reveal the text of</param>
    /// <param name="lettersPerSecond">The number of letters that should be revealed per second.</param>
    /// <param name="onCharacterTyped">An <see cref="Action"/> that should be called for each character that was revealed.</param>
    /// <param name="onPauseStarted">An <see cref="Action"/> that will be called when the typewriter effect is paused.</param>
    /// <param name="onPauseEnded">An <see cref="Action"/> that will be called when the typewriter effect is restarted.</param>
    /// <param name="pausePositions">A stack of character position and pause duration tuples used to pause the effect. Generally created by <see cref="LineView.GetPauseDurationsInsideLine"/></param>
    /// <param name="stopToken">A <see cref="CoroutineInterruptToken"/> that can be used to interrupt the coroutine.</param>
    public IEnumerator PausableTypewriter(
        TextMeshProUGUI text,
        float lettersPerSecond,
        Action onCharacterTyped,
        Action onPauseStarted,
        Action onPauseEnded,
        Stack<(int position, float duration)> pausePositions,
        YarnEffects.CoroutineInterruptToken stopToken = null)
    {
        stopToken?.Start();

        HideIcon(inputIconKeyboard.GetComponent<UnityEngine.UI.Image>());
        HideIcon(inputIconGamepad.GetComponent<UnityEngine.UI.Image>());

        // Start with everything invisible
        text.maxVisibleCharacters = 0;

        // Wait a single frame to let the text component process its
        // content, otherwise text.textInfo.characterCount won't be
        // accurate
        yield return null;

        // How many visible characters are present in the text?
        var characterCount = text.textInfo.characterCount;

        // Early out if letter speed is zero, text length is zero
        if (lettersPerSecond <= 0 || characterCount == 0)
        {
            // Show everything and return
            text.maxVisibleCharacters = characterCount;
            stopToken?.Complete();
            yield break;
        }

        // Convert 'letters per second' into its inverse
        float secondsPerLetter = 1.0f / lettersPerSecond;

        // If lettersPerSecond is larger than the average framerate, we
        // need to show more than one letter per frame, so simply
        // adding 1 letter every secondsPerLetter won't be good enough
        // (we'd cap out at 1 letter per frame, which could be slower
        // than the user requested.)
        //
        // Instead, we'll accumulate time every frame, and display as
        // many letters in that frame as we need to in order to achieve
        // the requested speed.
        var accumulator = Time.deltaTime;

        while (waitForTextChange || text.maxVisibleCharacters < characterCount)
        {
            if (stopToken?.WasInterrupted ?? false)
            {
                PunchIcons();
                yield break;
            }

            if (isPaused)
            {
                yield return null;
                continue;
            }

            // We need to show as many letters as we have accumulated
            // time for.
            while (accumulator >= secondsPerLetter)
            {
                text.maxVisibleCharacters += 1;

                // Clamp the counter if we are stalling
                // This way we don't shoot to the end of the next line change
                if (waitForTextChange && text.maxVisibleCharacters >= characterCount)
                {
                    text.maxVisibleCharacters = characterCount;       
                    // Do not have these wrong way around
                    characterCount = text.textInfo.characterCount;
                }
                else
                {
                    onCharacterTyped?.Invoke();
                }

                // ok so the change needs to be that if at any point we hit the pause position
                // we instead stop worrying about letters
                // and instead we do a normal wait for the necessary duration
                if (pausePositions != null && pausePositions.Count != 0)
                {
                    if (text.maxVisibleCharacters == pausePositions.Peek().Item1)
                    {
                        var pause = pausePositions.Pop();
                        onPauseStarted?.Invoke();
                        yield return YarnEffects.InterruptableWait(pause.Item2, stopToken);
                        onPauseEnded?.Invoke();

                        // need to reset the accumulator
                        accumulator = Time.deltaTime;
                    }
                }

                accumulator -= secondsPerLetter;
            }
            accumulator += Time.deltaTime;

            yield return null;
        }

        // We either finished displaying everything, or were
        // interrupted. Either way, display everything now.
        text.maxVisibleCharacters = characterCount;

        PunchIcons();

        stopToken?.Complete();
    }

    private void PunchIcons()
    {
        if (!waitForTextChange)
        {
            RevealIcon(inputIconKeyboard.GetComponent<UnityEngine.UI.Image>());
            RevealIcon(inputIconGamepad.GetComponent<UnityEngine.UI.Image>());

            PunchIcon(inputIconKeyboard);
            PunchIcon(inputIconGamepad);
        }
    }

    internal void AppendText(string analysisText)
    {
        textContainer.text += "\n\n" + analysisText;
    }
}

