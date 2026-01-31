
// We exclude debug tools from non-development builds 
#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugManager : Singleton<DebugManager>
{
    public string eventExecuteArgument;

    [Header("Dialogue")]
    [SerializeField] DialogueView dialogueView;
    public bool skipInitialDialogue;

    [Header("Audio")]
    public bool disableMusic;

    [Header("HUD")]
    public bool dontHideHudPanels;

    [Header("Interaction")]
    public bool disableAllInteractionTriggers;

    [Header("Debug window")]
    [SerializeField] GameObject canvasObject;
    [SerializeField] Text battleDebugText;
    [SerializeField] Text characterNameText;
    [SerializeField] Text terrainSamplingText;
    [SerializeField] Text inputLayersText;
    [SerializeField] TextMeshProUGUI interactorsText;
    [SerializeField] Text cameraTargetText;

    private Transform terrainSamplingLocation;

    private void Awake()
    {
        // Note: Even if you disable the GO of the DebugManager script or the script itself,
        // this will still run. So, be mindful of what you put here.
        DialogueManager.Instance.skipInitialDialogue = skipInitialDialogue;
        UIManager.Instance.hud.skipHiding = dontHideHudPanels;
        if (disableMusic) AudioManager.StopShoreMusic();

        if (disableAllInteractionTriggers)
        {
            foreach (var trigger in FindObjectsOfType<InteractionTrigger>())
            {
                trigger.gameObject.SetActive(false);
            }
        }
    }

    private void Start()
    {
        // Make sure debug panels are visible in (debug) build 
        canvasObject.SetActive(true);
    }

    private void Update()
    {
        UpdateInputDebug();
        UpdateSpeakerText();
        UpdateTerrainSamplingDebug();
        UpdateCameraDebug();
    }

    public override void OnSceneLoad(SceneContext sceneContext)
    {
        terrainSamplingLocation = sceneContext.mainCharacterObject.transform;
    }

    private void UpdateSpeakerText()
    {
        if (characterNameText == null)
        {
            return;
        }
        if (dialogueView != null && dialogueView.CurrentSpeaker(out var name))
        {
            characterNameText.text = $"Speaker: '{name}'";
        }
        else
        {
            characterNameText.text = "Speaker: (null)";
        }

    }

    private void UpdateInputDebug()
    {
        // Input layers
        if (inputLayersText == null)
        {
            return;
        }
        var names = InputManager.Instance.CurrentInputs();
        inputLayersText.text = "";
        for (int i = names.Count() - 1; i >= 0; i--)
        {
            inputLayersText.text += $"{names[i]} ({i})\n";
        }
        // Interactables

        var actor = GameManager.Instance.mainCharacterObject.GetComponentInChildren<Interactor>();
        interactorsText.text = "";
        foreach (var interactable in actor.AllInRange)
        {
            var text = $"{interactable.name} ({interactable.transform.parent.name})";
            interactorsText.text += interactable.CanInteractWith ? $"{text}\n" : $"<s>{text}</s>\n";
        }
    }

    private void UpdateTerrainSamplingDebug()
    {
        if (terrainSamplingLocation == null || terrainSamplingText == null)
        {
            return;
        }
        var boxText = "";

        // Sample terrain layers
        var textures = TerrainSampler.Instance.SampleTerrainLayersAt(terrainSamplingLocation.position);
        foreach (var (textureName, weight) in textures)
        {
            boxText += $"{textureName}: {weight:F2}\n";
        }

        // Sample grass
        boxText += "\n";
        var grass = TerrainSampler.Instance.SampleTerrainGrassAt(terrainSamplingLocation.position);
        foreach (var (grassName, amount) in grass)
        {
            boxText += $"{grassName}: {amount}\n";
        }

        terrainSamplingText.text = boxText;
    }

    private void UpdateCameraDebug()
    {
        cameraTargetText.text = CutsceneManager.Cam.CameraTargetName ?? "None (null)"; 
    }
}

#endif