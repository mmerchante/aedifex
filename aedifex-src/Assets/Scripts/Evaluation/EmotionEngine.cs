using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// TODO:
// - Filters that use reappearing patterns to generate surprise
// - Or repeating patterns to generate anticipation
public interface EmotionFilter
{
    EmotionSpectrum Filter(float normalizedTime, EmotionSpectrum current);
}

public struct EmotionEvent
{
    public enum EmotionEventType
    {
        None = 0,
        Start = 1,
        End = 2,
        LocalMaximum = 3,
        LocalMinimum = 4,
    }

    public EmotionSpectrum associatedEmotion;
    public EmotionEventType type;
    public float timestamp;
    public float intensity; // The intensity will basically define the prioritization of events
    public int trackIndex;
    public int chunkIndex;
    public bool chunkDelimitsSegment; // Is this chunk a start/end of a long segment? Useful for the emotional state machine
}

public class EmotionEngine
{
    private DataContainer container;
    private float[] audioSignal = null;
    private List<EmotionFilter> filters = new List<EmotionFilter>();
    private List<EmotionEvent> events = new List<EmotionEvent>();

    private EmotionSpectrum[] emotionalSignal = null;
    private EmotionSpectrum[] emotionalDerivativeSignal = null;

    private float TotalDuration { get; set; }
    public float BeatDuration { get; protected set; }
    public float MeasureDuration { get; protected set; }

    public float BeatDurationNormalized { get; protected set; }
    public float MeasureDurationNormalized { get; protected set; }

    private int downsampleRate = 1;

    public void Initialize(float totalDuration, float[] audioSignal, DataContainer container, int downsampleRate)
    {
        this.downsampleRate = downsampleRate;
        this.container = container;
        this.audioSignal = audioSignal;

        this.TotalDuration = totalDuration;

        this.BeatDuration = 60f / (float)container.beatsPerMinute;
        this.BeatDurationNormalized = BeatDuration / TotalDuration;

        this.MeasureDuration = BeatDuration * container.beatsPerMeasure;
        this.MeasureDuration = MeasureDuration * TotalDuration;
    }

    public EmotionSpectrum GetSpectrum(float normalizedT)
    {
        int index = Mathf.Clamp((int) (normalizedT * emotionalSignal.Length), 0, emotionalSignal.Length - 1);
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
        // TODO:
        // - Find recurring patterns
        // - Find max/mins
        // - Find overall average emotion
        // - Find conflict/dissonance
        // - Associate tracks with visual elements (is this responsibility of this class? NO)

        PreloadChunks();
        PreAccumulateEvents();
        CacheEmotionSignal();
        PostAccumulateEvents();
        PrecomputeExtremes();

        Debug.Log("Found " + events.Count + " events");

        // Order events by timestamp
        events = events.OrderBy(x => x.timestamp).ToList();
    }

    public Queue<EmotionEvent> BuildEventQueue()
    {
        return new Queue<EmotionEvent>(events);
    }

    /// <summary>
    /// Events can be found at the beginning and at the end of the processing stage.
    /// At the beginning, chunks are analyzed and used as discrete indicators of events.
    /// At the end, a more complicated search runs, trying to look for local minima/maxima and plot points.
    /// </summary>
    protected void PreAccumulateEvents()
    {
        List<EmotionEvent> foundEvents = new List<EmotionEvent>();

        for(int t = 0; t < container.tracks.Count; t++)
        {
            TrackData track = container.tracks[t];

            for (int c = 0; c < track.chunks.Count; c++)
            {
                TrackChunkData chunk = track.chunks[c];

                EmotionEvent startEvent = GetEventFromChunkStart(track, chunk, t, c);
                EmotionEvent endEvent = GetEventFromChunkEnd(track, chunk, t, c);

                foundEvents.Add(startEvent);
                foundEvents.Add(endEvent);
            }
        }

        events.AddRange(foundEvents);
    }

    public EmotionEvent GetEventFromChunkEnd(TrackData track, TrackChunkData chunk, int trackIndex, int chunkIndex)
    {
        EmotionEvent e = new EmotionEvent();
        e.trackIndex = trackIndex;
        e.chunkIndex = chunkIndex;
        e.type = EmotionEvent.EmotionEventType.End;
        e.timestamp = chunk.end;
        e.intensity = GetIntensityForChunkStartEvent(track, chunk, chunkIndex);
        e.associatedEmotion = new EmotionSpectrum(); // TODO: expectation/surprise
        e.chunkDelimitsSegment = IsChunkSegmentDelimiter(track, chunk, chunkIndex, e.type);
        return e;
    }

    public EmotionEvent GetEventFromChunkStart(TrackData track, TrackChunkData chunk, int trackIndex, int chunkIndex)
    {
        EmotionEvent e = new EmotionEvent();
        e.trackIndex = trackIndex;
        e.chunkIndex = chunkIndex;
        e.type = EmotionEvent.EmotionEventType.Start;
        e.timestamp = chunk.start;
        e.intensity = GetIntensityForChunkStartEvent(track, chunk, chunkIndex);
        e.associatedEmotion = new EmotionSpectrum(); // TODO: expectation/surprise 
        e.chunkDelimitsSegment = IsChunkSegmentDelimiter(track, chunk, chunkIndex, e.type);
        return e;
    }

    protected bool IsChunkSegmentDelimiter(TrackData track, TrackChunkData chunk, int chunkIndex, EmotionEvent.EmotionEventType type)
    {
        float timeDifference = 0f;

        if(type == EmotionEvent.EmotionEventType.Start && chunkIndex > 0)
        {
            TrackChunkData previousChunk = track.chunks[chunkIndex - 1];
            timeDifference = Mathf.Abs(chunk.start - previousChunk.end);
        }
        else if(type == EmotionEvent.EmotionEventType.End && chunkIndex < track.chunks.Count - 1)
        {
            TrackChunkData nextChunk = track.chunks[chunkIndex + 1];
            timeDifference = Mathf.Abs(nextChunk.start - chunk.end);
        }

        float threshold = MeasureDurationNormalized * 4f;
        return timeDifference > threshold;
    }
    
    /// <summary>
    /// In contrast with GetIntensityForChunkStartEvent, this method only tries to find
    /// differences a priori, knowing, for example, that it may be the last chunk in some time, etc.
    /// Most of the cases will be covered by GetIntensityForChunkStartEvent.
    /// Start events will usually drive camera cuts, while end events will probably drive only the emotion state machine
    /// </summary>
    protected float GetIntensityForChunkEndEvent(TrackData track, TrackChunkData chunk, int chunkIndex)
    {
        float intensity = 1f;
        
        if (chunkIndex < track.chunks.Count - 1)
        {
            TrackChunkData nextChunk = track.chunks[chunkIndex + 1];
            
            float timeDifference = nextChunk.start - chunk.end;
            float falloff = MeasureDurationNormalized * 4f;

            // If this is the last chunk in some time, make its ending more impactful
            if (timeDifference > falloff)
                intensity *= 2f;
        }

        intensity *= chunk.GetIntensity(chunk.end);

        return intensity;
    }
    /// <summary>
    /// This method tries to define important aspects of how chunks are placed.
    /// Right now it checks simple patterns, but it can be extended to find interesting arrangements in the future
    /// </summary>
    protected float GetIntensityForChunkStartEvent(TrackData track, TrackChunkData chunk, int chunkIndex)
    {
        // First chunk in track, important
        if (chunkIndex == 0)
            return 2f;

        float intensity = 1f;
        TrackChunkData previousChunk = track.chunks[chunkIndex - 1];
        
        // We care about any change in variation, because it can visually drive something
        bool variationChanged = (previousChunk.isVariation != chunk.isVariation);
        float variationContribution = (variationChanged ? 2f : 1f);

        float timeDifference = chunk.start - previousChunk.end;
        float falloff = MeasureDurationNormalized * 2f;

        // Basically, the closest the chunk, the more impact it will generate
        // TODO: maybe add nonlinear impact responses?
        float timeImpact = 1f - Mathf.Clamp01(timeDifference / falloff);
        float timeImpactContribution = Mathf.Lerp(1f, 2f, timeImpact);

        // As harmony progresses, so does intensity. When it restarts, the impact is big
        // Roughly, standard 4 measure progressions will generate at most 2f
        int hDifference = Mathf.Abs(chunk.harmonySequenceNumber - previousChunk.harmonySequenceNumber);
        float harmonicProgressionContribution = Mathf.Pow(1.2f, hDifference);

        // Harmony is broken if timing is off
        if (timeImpact < .45f)
            harmonicProgressionContribution = 1f;

        // On the contrary, if a long time passed between chunks, we have a nostalgia effect
        float longFalloff = MeasureDurationNormalized * 12f;
        float memoryImpact = Mathf.Clamp01(timeDifference / longFalloff);

        if (memoryImpact > .5f)
            timeImpactContribution += (memoryImpact - .5f) * 2f;

        // If the actual intensities of the chunks are very different at this point, then it may also be important
        float intensityDifference = Mathf.Abs(chunk.GetIntensity(chunk.start) - previousChunk.GetIntensity(chunk.start));
        float intensityDiffContribution = Mathf.Lerp(1f, 2f, intensityDifference);

        // TODO in the future: do pattern matching to see if the specific arrangement of chunks changes or is stable,
        // by searching on a window of +-8 measures
        // A simple example: look for repetition in the past 8 measures, for every repeated chunk add some intensity (and also add expectation)

        return intensity * variationContribution * timeImpactContribution * harmonicProgressionContribution * intensityDiffContribution;
    }

    /// <summary>
    /// Events can be found at the beginning and at the end of the processing stage.
    /// At the beginning, chunks are analyzed and used as discrete indicators of events.
    /// At the end, a more complicated search runs, trying to look for local minima/maxima and plot points.
    /// </summary>
    protected void PostAccumulateEvents()
    {

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

    // Eh, this is a very simplistic way of approaching the problem
    public static CoreEmotion FindMainEmotion(EmotionSpectrum e)
    {
        CoreEmotion maxEmotion = CoreEmotion.Anger;
        float maxEmotionValue = 0f;
        // TODO: Add some fuzziness by picking the best 3 emotions found, or something

        foreach (CoreEmotion core in System.Enum.GetValues(typeof(CoreEmotion)))
        {
            float dot = new EmotionSpectrum(EmotionVector.GetCoreEmotion(core)).Dot(e);

            if(dot > maxEmotionValue)
            {
                maxEmotion = core;
                maxEmotionValue = dot;
            }
        }

        return CoreEmotion.Joy;
    }
}
