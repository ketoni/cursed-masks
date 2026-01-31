using System;
using UnityEngine;

/// <summary> Shows a warning if the object reference is not set. </summary>
[AttributeUsage(AttributeTargets.Field)]
public class NullWarn : PropertyAttribute { }