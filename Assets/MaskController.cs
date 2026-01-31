using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class MaskController : MonoBehaviour
{
    public MaskStats stats;
    [InspectorReadOnly] public bool inspecting;
    [InspectorReadOnly] public bool cleansed; 

    public string AnalysisText => "Something";

    static Vector3 inspectOffset = new(0, 7, -3);

    private Vector3 axis; // Testing animation axis

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

    internal void Inspect()
    {
        inspecting = true;

        transform.DOLocalMove(transform.localPosition + inspectOffset, duration: 2f);
        transform.DOScale(endValue: 3f, duration: 3f);

        Debug.Log($"You are looking at a {(stats.cursed ? "cursed" : "not cursed")} mask!");
    }

}
