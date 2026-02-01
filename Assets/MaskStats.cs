
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Mask", menuName = "Mask Stats")]
public class MaskStats : ScriptableObject
{
    public int baseValue;
    public bool hornsExist;
    public bool eyesExist;
    public bool earsExist;
    public bool noseExists;
    public bool mouthExists;
    public List<MaskRule> rules;

    // Add more...
}

[Serializable]
public struct MaskRule
{
    public MaskFeature first;

    /// <summary>
    /// If the second part is not required, it is disallowed.
    /// </summary>
    public bool requireSecond;

    public MaskFeature second;
    public MaskEffect effect;
    public string text;

    public bool RequireJustFirst => second == MaskFeature.None; 
}

public enum MaskFeature
{
    None,
    Horns,
    Eyes,
    Ears,
    Nose,
    Mouth,
}

public enum MaskEffect
{
    Key,
    LowCurse,
    MedCurse,
    HighCurse,
    Cheap,
    Cheaper,
    Valuable,
    Valuabler,
    PriceMultiplier,
    AddTime,
    AddSanity,
    NegateKey,
    NegateBless,
    RepeatCurse,
    CollectedCountValue,
    ClockStop,
    NoCleanse,
    SlowCleanse,
    Possessed,
}
