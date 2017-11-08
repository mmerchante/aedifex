using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesintegrateOverTime : MonoBehaviour
{
    public Renderer meshRenderer;
    public float duration = 1f;
    public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private float currentTime = 0f;

    private Material mat;

    public void Awake()
    {
        mat = meshRenderer.material; // Copy it
    }

    public void Update()
    {
        currentTime += Time.deltaTime;
        float t = Mathf.Clamp01(currentTime / duration);
        mat.SetFloat("_Dissolve", curve.Evaluate(t));
    }
}
