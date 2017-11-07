using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CoreEmotion
{
    Joy = 0,
    Trust = 1,
    Fear = 2,
    Surprise = 3,
    Sadness = 4,
    Disgust = 5,
    Anger = 6,
    Anticipation = 7
}

[System.Serializable]
public class EmotionVector
{
    public float angle;
    public float intensity;

    public static float GetAngleForCoreEmotion(CoreEmotion e)
    {
        float quarterPI = Mathf.PI / 4f;

        switch (e)
        {
            case CoreEmotion.Joy:
                return quarterPI * 2f;
            case CoreEmotion.Trust:
                return quarterPI * 1f;
            case CoreEmotion.Fear:
                return 0f;
            case CoreEmotion.Surprise:
                return quarterPI * 7f;
            case CoreEmotion.Sadness:
                return quarterPI * 6f;
            case CoreEmotion.Disgust:
                return quarterPI * 5f;
            case CoreEmotion.Anger:
                return quarterPI * 4f;
            case CoreEmotion.Anticipation:
                return quarterPI * 3f;
        }

        return 0f;
    }

    public static EmotionVector GetCoreEmotion(CoreEmotion e)
    {
        return new EmotionVector(GetAngleForCoreEmotion(e), 1f);
    }

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

    // Note that this is dG/dTheta, not dG/dTime
    public float EvaluateDerivative(float t)
    {
        return EvaluateDG(t) + EvaluateDG(t - 2f * Mathf.PI) + EvaluateDG(t + 2f * Mathf.PI);
    }

    // A simple gaussian centered over _angle_ for now
    protected float EvaluateG(float t)
    {
        float sigma = Mathf.PI / 20f;
        return intensity * Mathf.Exp(-Mathf.Pow(t - angle, 2f) / (2f * sigma * sigma)) / (sigma * Mathf.Sqrt(2f * Mathf.PI));
    }

    protected float EvaluateDG(float t)
    {
        float sigma = Mathf.PI / 20f;
        float x = t - angle;
        return intensity * x * (-1f / (sigma * sigma * sigma * Mathf.Sqrt(2f * Mathf.PI))) * Mathf.Exp(-1f * Mathf.Pow(x, 2f) / (2f * sigma * sigma));
    }
}

// For now, a dummy container -- I added methods in case we change
// the inner structure later
[System.Serializable]
public class EmotionData
{
    public List<EmotionVector> vectors = new List<EmotionVector>();
    public float intensityMultiplier = 1f;

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
    
    public float Evaluate(float t, bool normalize = false)
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

        if(normalize)
            return result / normalization;

        return result * intensityMultiplier;// / normalization;
    }

    public EmotionSpectrum GetSpectrum()
    {
        return new EmotionSpectrum(this);
    }
}

/// <summary>
/// A histogram of the emotion wheel. Has arithmetic operations to simplify math and compose results.
/// Note that this container is heavy and any big operations will take some time.
/// </summary>
public class EmotionSpectrum
{
    public const int SAMPLE_COUNT = 180;

    private float[] samples = new float[SAMPLE_COUNT];

    public EmotionSpectrum()
    {
    }

    // This is the evaluated result of the sum of a set of gaussians.
    public EmotionSpectrum(EmotionData gaussians)
    {
        for (int i = 0; i < SAMPLE_COUNT; ++i)
        {
            float t = i / (float)SAMPLE_COUNT;
            samples[i] = gaussians.Evaluate(t * 2f * Mathf.PI);
        }
    }

    // This is the evaluated result of a single gaussian
    public EmotionSpectrum(EmotionVector vector)
    {
        EmotionData gaussians = new EmotionData();
        gaussians.AddVector(vector);

        for (int i = 0; i < SAMPLE_COUNT; ++i)
        {
            float t = i / (float)SAMPLE_COUNT;
            samples[i] = gaussians.Evaluate(t * 2f * Mathf.PI);
        }
    }

    public float this[float angle]
    {
        get
        {
            angle = Mathf.Repeat(angle, 2f * Mathf.PI);
            int i = Mathf.FloorToInt((angle / (2f * Mathf.PI)) * SAMPLE_COUNT);
            i = Mathf.Clamp(i, 0, SAMPLE_COUNT);
            return samples[i];
        }
        set
        {
            angle = Mathf.Repeat(angle, 2f * Mathf.PI);
            int i = Mathf.FloorToInt((angle / (2f * Mathf.PI)) * SAMPLE_COUNT);
            i = Mathf.Clamp(i, 0, SAMPLE_COUNT);
            samples[i] = value;
        }
    }

    public static EmotionSpectrum Lerp(EmotionSpectrum a, EmotionSpectrum b, float t)
    {
        return a * (1f - t) + b * t;
    }

    // Basically the integral
    public float GetTotalEnergy()
    {
        float r = 0f;
        float binLength = 2f * Mathf.PI / (float)SAMPLE_COUNT;

        for (int i = 0; i < SAMPLE_COUNT; ++i)
            r += samples[i];

        return r * binLength;
    }
    
    public float Dot(EmotionSpectrum s)
    {
        float r = 0f;

        for (int i = 0; i < SAMPLE_COUNT; ++i)
            r += samples[i] * s.samples[i];

        return r;
    }

    public static EmotionSpectrum operator +(EmotionSpectrum a, EmotionSpectrum b)
    {
        EmotionSpectrum r = new EmotionSpectrum();

        for (int i = 0; i < SAMPLE_COUNT; ++i)
            r.samples[i] = a.samples[i] + b.samples[i];

        return r;
    }

    public static EmotionSpectrum operator -(EmotionSpectrum a, EmotionSpectrum b)
    {
        EmotionSpectrum r = new EmotionSpectrum();

        for (int i = 0; i < SAMPLE_COUNT; ++i)
            r.samples[i] = a.samples[i] - b.samples[i];

        return r;
    }

    public static EmotionSpectrum operator *(EmotionSpectrum a, float scalar)
    {
        EmotionSpectrum r = new EmotionSpectrum();

        for (int i = 0; i < SAMPLE_COUNT; ++i)
            r.samples[i] = a.samples[i] * scalar;

        return r;
    }

    public static EmotionSpectrum operator /(EmotionSpectrum a, float scalar)
    {
        EmotionSpectrum r = new EmotionSpectrum();

        for (int i = 0; i < SAMPLE_COUNT; ++i)
            r.samples[i] = a.samples[i] / scalar;

        return r;
    }

    public static EmotionSpectrum operator *(EmotionSpectrum a, EmotionSpectrum b)
    {
        EmotionSpectrum r = new EmotionSpectrum();

        for (int i = 0; i < SAMPLE_COUNT; ++i)
            r.samples[i] = a.samples[i] * b.samples[i];

        return r;
    }

    public static EmotionSpectrum operator /(EmotionSpectrum a, EmotionSpectrum b)
    {
        EmotionSpectrum r = new EmotionSpectrum();

        for (int i = 0; i < SAMPLE_COUNT; ++i)
            r.samples[i] = a.samples[i] / b.samples[i];

        return r;
    }
}