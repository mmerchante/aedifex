using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmotionVector
{
    public float angle;
    public float intensity;

    public EmotionVector(float angle, float intensity)
    {
        this.angle = angle;
        this.intensity = intensity;
    }

    public EmotionVector() : this(0f, 0f)
    {
    }
    
    // A simple gaussian centered over _angle_ for now
    public float Evaluate(float t)
    {
        // Wrap around
        if (angle > Mathf.PI && t < Mathf.PI)
            t += Mathf.PI * 2f;
        else if(angle < Mathf.PI && t > Mathf.PI)
            t = (t - Mathf.PI * 2f);

        float sigma = Mathf.PI / 10f;
        return intensity * Mathf.Exp(-Mathf.Pow(t - angle, 2f) / (2f * sigma * sigma)) / (sigma *  Mathf.Sqrt(2f * Mathf.PI));
    }
}

// For now, a dummy container -- I added methods in case we change
// the inner structure later
public class EmotionData
{
    public List<EmotionVector> vectors = new List<EmotionVector>();

    public void AddVector(EmotionVector v)
    {
        this.vectors.Add(v);
    }

    public void RemoveVector(EmotionVector v)
    {
        this.vectors.Remove(v);
    }
    
    public float Evaluate(float t)
    {
        float result = 0f;
        float normalization = 0f;

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
