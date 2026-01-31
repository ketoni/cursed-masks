using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class MaskController : MonoBehaviour
{
    public enum EffectType
    {
        Time,
        Sanity,
        Curse,
        Value,
        Key,
        Cleanse
    }

    public MaskStats stats;
    public List<MaskRuleTextScriptableObject> ruleTexts;

    [InspectorReadOnly] public bool inspecting;
    [InspectorReadOnly] public bool cleansed;
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

    public int CurseLevel { get; set; }
    public int Value { get; set; }
    public int BonusValue { get; set; }
    public bool IsKey { get; set; }
    public bool CanBeCleansed { get; set; }
    public bool SlowerCleansing { get; set; }

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

    private void InitStats()
    {
        CurseLevel = GetCombinedEffectValue(stats.rules, EffectType.Curse);
        CanBeCleansed = true;
    }

    private int GetCombinedEffectValue(List<MaskRule> rules, EffectType effectType)
    {
        foreach (MaskRule rule in rules)
        {
            switch (effectType)
            {
                case EffectType.Time:
                    break;
                case EffectType.Sanity:
                    break;
                case EffectType.Curse:
                    break;
                case EffectType.Value:
                    break;
                case EffectType.Key:
                    break;
                case EffectType.Cleanse:
                    break;
                default:
                    break;
            }
        }

        return 0;
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
