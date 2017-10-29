using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO:
// - Filters that use reappearing patterns to generate surprise
// - Or repeating patterns to generate anticipation
public interface EmotionFilter
{
    EmotionSpectrum Filter(float normalizedTime, EmotionSpectrum current);
}

public struct EmotionalEvent
{
    public enum Type
    {
        None = 0,
        Repetition = 1,
        Surprise = 2,
        Climax = 3, // Max
        Saddle = 4, // Min
    }
}

public class EmotionEngine
{
    private DataContainer container;
    private float[] audioSignal = null;
    private List<EmotionFilter> filters = new List<EmotionFilter>();

    private EmotionSpectrum[] emotionalSignal = null;
    private EmotionSpectrum[] emotionalDerivativeSignal = null;

    private int downsampleRate = 1;

    public void Initialize(float[] audioSignal, DataContainer container, int downsampleRate)
    {
        this.downsampleRate = downsampleRate;
        this.container = container;
        this.audioSignal = audioSignal;
    }

    public EmotionSpectrum GetSpectrum(float normalizedT)
    {
        int index = Mathf.Clamp((int) (normalizedT * emotionalSignal.Length), 0, emotionalSignal.Length);
        return emotionalSignal[index];
    }

    protected void PreloadChunks()
    {
        foreach (TrackData track in container.tracks)
             foreach (TrackChunkData chunk in track.chunks)
                chunk.Preload();
    }

    // This is very expensive!
    // I'm not worrying about performance right now
    public void Precompute()
    {
        PreloadChunks();
        CacheEmotionSignal();
        // TODO:
        // - Find recurring patterns
        // - Find max/mins
        PrecomputeExtremes();
        // - Add emotion based on appearing/disappearing patterns
        // - Find overall average emotion
        // - Find conflict/dissonance
        // - Find climax, if there's one
        // - Associate tracks with visual elements (is this responsibility of this class?)
        // - Maybe: cache emotion signal
    }

    // TODO: if we need more performance, multithread this
    protected void CacheEmotionSignal()
    {
        int sampleCount = audioSignal.Length / downsampleRate; // We don't need very high precision

        this.emotionalSignal = new EmotionSpectrum[sampleCount];
        this.emotionalDerivativeSignal = new EmotionSpectrum[sampleCount];

        float dt = 1f / (float)sampleCount;
        for (int i = 0; i < sampleCount; ++i)
        {
            emotionalSignal[i] = Compute(i * dt);

            if (i == 0)
                emotionalDerivativeSignal[i] = new EmotionSpectrum();
            else
                emotionalDerivativeSignal[i] = (emotionalSignal[i] - emotionalSignal[i - 1]) / dt;
        }
    }

    protected void PrecomputeExtremes()
    {
        int sampleCount = audioSignal.Length / downsampleRate; // We don't need very high precision

        float dt = 1f / (float) sampleCount;

        float minTime = 0f;
        float maxTime = 0f;
        float min = float.MaxValue;
        float max = 0f;

        for(int i = 0; i < sampleCount; ++i)
        {
            float t = i * dt;
            float intensity = emotionalSignal[i].GetTotalEnergy();// TODO: * (1f + signalContribution);

            if (intensity < min)
            {
                min = intensity;
                minTime = t;
            }

            if(intensity > max)
            {
                max = intensity;
                maxTime = t;
            }
        }

        // TODO: Align min/max times to beat
    }

    protected void PrecomputeNewEvents()
    {
    }
    
    public EmotionSpectrum Compute(float normalizedTime)
    {
        List<TrackChunkData> chunks = GetChunksAtTime(normalizedTime);
        EmotionSpectrum result = new EmotionSpectrum();

        foreach (TrackChunkData chunk in chunks)
            result += chunk.Evaluate(normalizedTime);

        foreach (EmotionFilter f in filters)
            result = f.Filter(normalizedTime, result);

        return result;
    }

    public List<TrackChunkData> GetChunksAtTime(float normalizedTime)
    {
        List<TrackChunkData> list = new List<TrackChunkData>();

        foreach (TrackData track in container.tracks)
        {
            if (track.trackType == TrackType.Emotion)
            {
                foreach (TrackChunkData chunk in track.chunks)
                {
                    if (chunk.start <= normalizedTime && chunk.end >= normalizedTime)
                        list.Add(chunk);
                }
            }
        }

        return list;
    }
}
