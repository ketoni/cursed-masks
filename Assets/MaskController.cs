using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using static MaskController;

public class MaskController : MonoBehaviour
{
    public enum EffectType
    {
        Time,
        Sanity,
        Curse,
        Value,
        Key,
        Cleanse,
        Special
    }

    public MaskStats stats;
    public List<MaskRuleTextScriptableObject> ruleTexts;

    [InspectorReadOnly] public bool inspecting;
    public GameObject ears;
    public GameObject eyes;
    public GameObject horns;
    public GameObject spikes; 
    public GameObject mouth;
    public GameObject nose;
    public GameObject teeth;
    public GameObject eyeLightsContainer;


    static Vector3 inspectOffset = new(0, 7, -3);

    private Vector3 axis; // Testing animation axis
    private int curseLevel;

    public int CurseLevel
    {
        get
        {
            return curseLevel;
        }

        set
        {
            curseLevel = value;
            if (curseLevel < 0 && !CanBeBlessed)
            {
                curseLevel = 0;
            }
        }
    }

    public int BonusValue { get; set; }
    public bool IsKey { get; set; }
    public bool CanBeCleansed { get; set; }
    public bool CanBeBlessed { get; set; }
    public bool SlowerCleansing { get; set; }
    public bool StopsTime { get; set; }

    public List<(MaskEffect Effect, EffectType Type, int Intensity)> MaskEffectTypeAssignments => new()
    {
        (MaskEffect.AddTime, EffectType.Time, 5),
        (MaskEffect.AddSanity, EffectType.Sanity, 3),
        (MaskEffect.LowCurse, EffectType.Curse, 1),
        (MaskEffect.MedCurse, EffectType.Curse, 2),
        (MaskEffect.HighCurse, EffectType.Curse, 3),
        (MaskEffect.Cheap, EffectType.Value, -1),
        (MaskEffect.Cheaper, EffectType.Value, -2),
        (MaskEffect.Valuable, EffectType.Value, 1),
        (MaskEffect.Valuabler, EffectType.Value, 2),
        (MaskEffect.PriceMultiplier, EffectType.Value, 2),
        (MaskEffect.Key, EffectType.Key, 1),
        (MaskEffect.NegateKey, EffectType.Key, -1),
        (MaskEffect.NegateBless, EffectType.Special, 0),
        (MaskEffect.RepeatCurse, EffectType.Curse, 0),
        (MaskEffect.CollectedCountValue, EffectType.Special, 0),
        (MaskEffect.ClockStop, EffectType.Time, 0),
        (MaskEffect.NoCleanse, EffectType.Cleanse, 0),
        (MaskEffect.SlowCleanse, EffectType.Cleanse, 0),
        (MaskEffect.Possessed, EffectType.Special, 0)
    };

    void Awake()
    {
        // If we don't have stats set, load one randomly 
        if (stats == null)
        {
            var allStats = Resources.LoadAll<MaskStats>("Masks");
            stats = allStats[Random.Range(0, allStats.Length)];
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        var mat = GetComponent<Renderer>().material;
        mat.color = Color.HSVToRGB(Random.value, 1f, 1f);
    
        axis = Random.onUnitSphere;
    }

    // Update is called once per frame
    void Update()
    {
        if (inspecting)
        {
            transform.Rotate(axis, 10f * Time.deltaTime, Space.World);
        } 
    }

    public void Init()
    {
        CurseLevel = GetCombinedEffectValue(stats.rules, EffectType.Curse);
        BonusValue = GetCombinedEffectValue(stats.rules, EffectType.Value);
        IsKey = GetCombinedEffectValue(stats.rules, EffectType.Key) > 0;
        CanBeCleansed = true;
    }

    private int GetCombinedEffectValue(List<MaskRule> rules, EffectType effectType)
    {
        /*
        var relevantRules = rules
            .Where(r => MaskEffectTypeAssignments.Any(a => a.Effect == r.effect)).ToList();
        */

        /*
        var ruleAssignments = MaskEffectTypeAssignments
            .Where(a => a.Type == effectType && rules.Any(r => r.effect == a.Effect)).ToList();
        */

         var ruleAssignments =
            rules.Select(r =>
                MaskEffectTypeAssignments.FirstOrDefault(a => a.Type == effectType && a.Effect == r.effect)).ToList();

        // TODO: Handle special cases

        return ruleAssignments.Sum(a => a.Intensity);
    }

    internal void Inspect()
    {
        inspecting = true;

        transform.DOLocalMove(transform.localPosition + inspectOffset, duration: 2f);
        transform.DOScale(endValue: 3f, duration: 3f);

        Debug.Log($"You are looking at a {(CurseLevel > 0 ? "cursed" : "not cursed")} mask!");
    }

    public string GetRandomRelevantRuleText(MaskFeature scannedFeature)
    {
        if (scannedFeature == MaskFeature.None)
        {
            return null;
        }

        var relevantRuleTexts = ruleTexts
            .Where(r => r.rule.first == scannedFeature || r.rule.second == scannedFeature).ToList();

        if (relevantRuleTexts == null || relevantRuleTexts.Count == 0)
        {
            return null;
        }

        return relevantRuleTexts[Random.Range(0, relevantRuleTexts.Count)].text;
    }
}
