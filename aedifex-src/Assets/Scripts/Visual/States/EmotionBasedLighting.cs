using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmotionBasedLighting : EmotionBehavior
{
    public float minIntensity = 1f;
    public float maxIntensity = 2f;

    public float globalMultiplier = 1f;
    public float trackMultiplier = 1f;

    public Gradient colorGradient = new Gradient();
    public AnimationCurve responseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public float smoothTime = 1f;

    private Light emotionLight;

    private float currentValue;
    private float currentValueVelocity;

    protected override void OnAwake()
    {
        emotionLight = GetComponent<Light>();
    }

    protected override void OnUpdate()
    {
        float t = Mathf.Clamp01((TrackEmotionIncidence * trackMultiplier + GlobalEmotionIncidence * globalMultiplier));
        t = responseCurve.Evaluate(t);

        currentValue = Mathf.SmoothDamp(currentValue, t, ref currentValueVelocity, smoothTime);

        t = Mathf.Clamp01(currentValue);
        emotionLight.color = colorGradient.Evaluate(t);
        emotionLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
    }
}