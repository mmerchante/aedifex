using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EmotionVector
{
    public float angle;
    public float intensity;

    public static Color GetColorForAngle(float angle)
    {
        return Color.HSVToRGB(Mathf.Repeat(angle * .5f / Mathf.PI, 1f), 1f, 1f);
    }

    public EmotionVector(float angle, float intensity)
    {
        this.angle = angle;
        this.intensity = intensity;
    }

    public EmotionVector() : this(0f, 0f)
    {
    }

    // Triple evaluation is for the wrap-around of the function
    public float Evaluate(float t)
    {
        return EvaluateG(t) + EvaluateG(t - 2f * Mathf.PI) + EvaluateG(t + 2f * Mathf.PI);
    }

    // A simple gaussian centered over _angle_ for now
    protected float EvaluateG(float t)
    {
        float sigma = Mathf.PI / 20f;
        return intensity * Mathf.Exp(-Mathf.Pow(t - angle, 2f) / (2f * sigma * sigma)) / (sigma * Mathf.Sqrt(2f * Mathf.PI));
    }
}

// For now, a dummy container -- I added methods in case we change
// the inner structure later
[System.Serializable]
public class EmotionData
{
    public List<EmotionVector> vectors = new List<EmotionVector>();

    public EmotionData()
    {
    }

    public EmotionData(EmotionData other)
    {
        this.vectors.AddRange(other.vectors);
    }

    public void AddVector(EmotionVector v)
    {
        this.vectors.Add(v);
    }

    public void Clear()
    {
        this.vectors.Clear();
    }

    public void RemoveVector(EmotionVector v)
    {
        this.vectors.Remove(v);
    }
    
    public float Evaluate(float t)
    {
        float result = 0f;
        float normalization = 0f;

        // The triple evaluation is for the wrap-around
        foreach (EmotionVector v in vectors)
        {
            normalization += v.intensity;
            result += v.Evaluate(t);
        }

        if (normalization == 0f)
            normalization = 1f;

        return result / normalization;
    }
}
