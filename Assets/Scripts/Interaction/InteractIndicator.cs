using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractIndicator : MonoBehaviour
{
    public Color indicationColor = Color.white; // Change this to the desired flash color
    public Renderer effectRenderer;

    private Color originalColor;

    public void Show()
    {
        originalColor = effectRenderer.material.color;
        effectRenderer.material.color = indicationColor;
    }

    public void Hide()
    {
        effectRenderer.material.color = originalColor;
    }
}