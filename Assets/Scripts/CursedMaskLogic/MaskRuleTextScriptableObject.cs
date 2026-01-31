using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Mask Rule Text", menuName = "New Mask Rule Text")]
public class MaskRuleTextScriptableObject : ScriptableObject
{
    public MaskRule rule;
    public string text;
}
