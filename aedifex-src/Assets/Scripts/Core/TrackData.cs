using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TrackCategory
{
    None = 0,
    MainMelody = 1,     // Anything that is core to the song
    Rythm = 2,          // Rythmic tracks are good for camera cuts
    Support = 3,        // Harmonic progressions, etc.
    Misc = 4,           // Noises, specific events, etc
    Structure = 5,      // Useful for easily knowing if the song is going up or down
}

public enum IntensityCurve
{
    Invariant = 0,
    LinearIncreasing = 1,
    LinearDecreasing = 2,
    SmoothSpike = 3, // Increases and then decreases
}

public enum TrackType
{
    None = 0,
    Waveform = 1,
    Emotion = 2
}

public enum ChunkType
{
    None = 0,
    Emotion = 1,
}

[System.Serializable]
public class TrackChunkData
{
    public ChunkType type;
    public float start;
    public float end;
    public EmotionData startData; // Eh, no time for abstraction
    public EmotionData endData;
    public IntensityCurve intensityCurve; // A bit rigid
    public bool isVariation;
    public int harmonySequenceNumber;

    [System.NonSerialized]
    private AnimationCurve effectiveIntensityCurve = null;

    public void Preload()
    {
        effectiveIntensityCurve = GetAnimationCurve(intensityCurve);
    }

    public float GetIntensity(float normalizedTime)
    {
        float localTime = Mathf.Clamp01((normalizedTime - start) / (end - start));
        return effectiveIntensityCurve.Evaluate(localTime);
    }

    public EmotionSpectrum Evaluate(float normalizedTime)
    {
        return startData.GetSpectrum() * GetIntensity(normalizedTime);
    }

    public static AnimationCurve GetAnimationCurve(IntensityCurve type)
    {
        switch (type)
        {
            case IntensityCurve.Invariant:
                return AnimationCurve.Linear(0f, 1f, 1f, 1f);
            case IntensityCurve.LinearIncreasing:
                return AnimationCurve.Linear(0f, 0f, 1f, 1f);
            case IntensityCurve.LinearDecreasing:
                return AnimationCurve.Linear(0f, 1f, 1f, 0f);
            case IntensityCurve.SmoothSpike:
                AnimationCurve c = new AnimationCurve();
                c.AddKey(0f, 0f);
                c.AddKey(.5f, 1f);
                c.AddKey(1f, 0f);
                return c;
        }

        return AnimationCurve.Linear(0f, 1f, 1f, 1f);
    }
}

[System.Serializable]
public class TrackData
{
    public int id; // 0 is invalid
    public string trackName;
    public TrackType trackType;
    public TrackCategory category;
    public List<TrackChunkData> chunks = new List<TrackChunkData>();

    public override string ToString()
    {
        return trackName + "(" + category + ", " + id + ")";
    }
}

[System.Serializable]
public class DataContainer
{
    public List<TrackData> tracks = new List<TrackData>();
    public int beatsPerMinute = 100;
    public int beatsPerMeasure = 4;
}