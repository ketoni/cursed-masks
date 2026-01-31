using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;
using static Interactable;
using Cuts = CutsceneManager;

public class GameManager : Singleton<GameManager>
{
    [Header("Main Character")]
    [InspectorReadOnly] public GameObject mainCharacterObject;
    public List<GameObject> inventoryObjects = new ();

    // Static shorthands to access main character object, movement and transform
    // Hungry? Try the classic Mac Object, or Mac Movement with added speed, or the new Mac Transform with added suffering!
    public static GameObject McObject => Instance.mainCharacterObject;
    public static MainCharacterMovement McMovement => McObject.GetComponent<MainCharacterMovement>();
    public static Transform McTransform => McObject.transform;

    [Space]
    [Header("Meditation")]
    public int meditationMaxCharges = 3;
    public int meditationCharges = 3;
    public bool inWispfireRange = false;
    public string wispfireTeleportLocationName;
    public Transform wispfireTeleportLocation;

    public Dictionary<InteractType, List<Interactable>> interactableCache = new();

    public string PlayerName { get; private set; }

    private void Awake()
    {
        PlayerName = "Someone";
    }

    public void Start()
    {
        InputManager.Instance.Push(InputManager.Player);
    }

    public override void OnSceneLoad(SceneContext context)
    {
        mainCharacterObject = context.mainCharacterObject;
        ReloadInteractableCache(context.cachedInteractables);
    }

    private void ReloadInteractableCache(List<Interactable> cachedInteractables)
    {
        // Clear previous cache and populate it with the provided list.
        // The cache is used for fast remote event execution between Interactables
        interactableCache.Clear();
        foreach (var interactable in cachedInteractables)
        {
            if (!interactableCache.ContainsKey(interactable.type))
            {
                interactableCache.Add(interactable.type, new()); 
            }
            // Check for dopplegangers (amogus..)
            foreach (var i in interactableCache[interactable.type])
            {
                if (i.name == interactable.name)
                {
                    Debug.LogWarning($"Interactable '{interactable.name}' has a matching {interactable.type} " +
                    "in the loaded cache. This will most likely cause wrong remote interactions to trigger!");
                }
            }
            // We add them nonetheless
            interactableCache[interactable.type].Add(interactable);
        }
    }

    public void ChangeScene(string sceneName)
    {
        // To-do: fade outs and ins etc.
        SceneManager.LoadScene(sceneName);
    }

    // For fast access to remote execution targets
    public Interactable GetCachedInteractable(string targetName, InteractType targetType)
    {
        if (!interactableCache.ContainsKey(targetType)) return null;
        foreach (var i in interactableCache[targetType])
        {
            if (i.name == targetName) return i;
        }
        return null;
    }

    // For scene-wide searches. This should be only used as a fallback.
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
