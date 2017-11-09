using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesintegrateOverTime : MonoBehaviour
{
    public Material material;
    public Renderer[] meshRenderers;
    
    public float duration = 1f;
    public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private float currentTime = 0f;

    [ContextMenu ("Assign child renderers")]
    public void AssignAllMeshes()
    {
        this.meshRenderers = GetComponentsInChildren<Renderer>();
    }

    public void Awake()
    {
        material = new Material(material);

        foreach (Renderer r in meshRenderers)
            r.sharedMaterial = material;
    }

    public void Update()
    {
        currentTime += Time.deltaTime;
        float t = Mathf.Clamp01(currentTime / duration);
        material.SetFloat("_Dissolve", curve.Evaluate(t));
    }
}
