
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
    public bool requireSecond;
    public MaskFeature second;
    public MaskEffect effect;
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
    AddTime,
    AddSanity,
    LowCurse,
    MedCurse,
    HighCurse,
    Cheap,
    Cheaper,
    Valuable,
    Valuabler,
    PriceMultiplier,
    Key,
    NegateKey,
    NegateBless,
    RepeatCurse,
    CollectedCountValue,
    ClockStop,
    NoCleanse,
    SlowCleanse,
    Possessed,
}
