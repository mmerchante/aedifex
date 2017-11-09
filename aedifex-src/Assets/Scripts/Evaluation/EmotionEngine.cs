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
        Sustain = 5,
    }

    public EmotionSpectrum associatedEmotion;
    public EmotionEventType type;
    public float timestamp;
    public float intensity; // The intensity will basically define the prioritization of events
    public int trackIndex;
    public int chunkIndex;
    public bool chunkDelimitsSegment; // Is this chunk a start/end of a long segment? Useful for the emotional state machine
    public int harmonicDifference;

    public override string ToString()
    {
        return "Event [" + type.ToString() + " | " + intensity + " | " + trackIndex + "]";
    }
}

public enum StructureType
{
    None = 0,
    Sustain = 1,
    Increasing = 2,
    Decreasing = 3,
}

public class EmotionEngine
{
    private DataContainer container;
    private float[] audioSignal = null;
    private List<EmotionFilter> filters = new List<EmotionFilter>();
    private List<EmotionEvent> events = new List<EmotionEvent>();

    private EmotionSpectrum[] emotionalSignal = null;
    private EmotionSpectrum[] emotionalDerivativeSignal = null;

    private EmotionSpectrum[] smoothEmotionSignal = null;

    private TrackData structuralTrack;

    // Public for the UI
    public float[] TotalEnergySignal { get; set; }
    public float[] SmoothEnergySignal { get; set; }

    private float TotalDuration { get; set; }
    public float BeatDuration { get; protected set; }
    public float MeasureDuration { get; protected set; }

    public float BeatDurationNormalized { get; protected set; }
    public float MeasureDurationNormalized { get; protected set; }

    public float MaxEnergy { get; protected set; }
    public float MinEnergy { get; protected set; }

    private int downsampleRate = 1;

    private Dictionary<int, TrackData> trackMap = new Dictionary<int, TrackData>();

    public void Initialize(float totalDuration, float[] audioSignal, DataContainer container, int downsampleRate)
    {
        this.downsampleRate = downsampleRate;
        this.container = container;
        this.audioSignal = audioSignal;

        this.TotalDuration = totalDuration;

        this.BeatDuration = 60f / (float)container.beatsPerMinute;
        this.BeatDurationNormalized = BeatDuration / TotalDuration;

        this.MeasureDuration = BeatDuration * container.beatsPerMeasure;
        this.MeasureDurationNormalized = MeasureDuration / TotalDuration;
    }

    public EmotionSpectrum GetSpectrum(float normalizedT)
    {
        int index = Mathf.Clamp((int) (normalizedT * emotionalSignal.Length), 0, emotionalSignal.Length - 1);
        return emotionalSignal[index];
    }

    public EmotionSpectrum GetSmoothSpectrum(float normalizedT)
    {
        int index = Mathf.Clamp((int)(normalizedT * smoothEmotionSignal.Length), 0, smoothEmotionSignal.Length - 1);
        return smoothEmotionSignal[index];
    }

    public EmotionSpectrum GetSpectrumDerivative(float normalizedT)
    {
        int index = Mathf.Clamp((int)(normalizedT * emotionalDerivativeSignal.Length), 0, emotionalDerivativeSignal.Length - 1);
        return emotionalDerivativeSignal[index];
    }

    public float GetTotalEnergy(float normalizedT)
    {
        int index = Mathf.Clamp((int)(normalizedT * TotalEnergySignal.Length), 0, TotalEnergySignal.Length - 1);
        return TotalEnergySignal[index];
    }

    public float GetSmoothEnergy(float normalizedT)
    {
        int index = Mathf.Clamp((int)(normalizedT * SmoothEnergySignal.Length), 0, SmoothEnergySignal.Length - 1);
        return SmoothEnergySignal[index];
    }

    public TrackData GetTrackByIndex(int index)
    {
        return container.tracks[index];
    }

    public TrackData GetTrackById(int id)
    {
        if(trackMap.ContainsKey(id))
            return trackMap[id];

        return null;
    }

    protected void PreloadChunks()
    {
        trackMap = new Dictionary<int, TrackData>();

        foreach (TrackData track in container.tracks)
        {
            trackMap[track.id] = track;

            foreach (TrackChunkData chunk in track.chunks)
                chunk.Preload();
            
            if (track.category == TrackCategory.Structure)
            {
                if (structuralTrack != null)
                    Debug.LogError("Duplicate structural track!");

                structuralTrack = track;
            }
        }
    }

    // This is very expensive!
    // I'm not worrying about performance right now
    public void Precompute()
    {
        PreloadChunks();
        PreAccumulateEvents();
        CacheEmotionSignal();
        PostAccumulateEvents();
        PrecomputeExtremes();
        FilterSignal();

        // Order events by timestamp
        events = events.OrderBy(x => x.timestamp).ToList();
    }
    
    /// <summary>
    /// Smooth the emotion signal a bit so we can find smooth features
    /// </summary>
    protected void FilterSignal()
    {
        int samples = emotionalSignal.Length;
        float window = BeatDurationNormalized * 4f;
        int sampleWindow = (int) (window * samples);
        int halfWindow = sampleWindow / 2;

        smoothEmotionSignal = new EmotionSpectrum[samples];
        SmoothEnergySignal = new float[samples];

        for(int x = 0; x < samples; ++x)
        {
            EmotionSpectrum avg = new EmotionSpectrum();

            // No time for gaussian ;)
            for (int dx = -halfWindow; dx <= halfWindow; ++dx)
                if (x + dx >= 0 && x + dx < samples)
                    avg += emotionalSignal[x + dx];

            smoothEmotionSignal[x] = avg / sampleWindow;
            SmoothEnergySignal[x] = smoothEmotionSignal[x].GetTotalEnergy();
        }
    }

    public EmotionEvent GetFirstEvent()
    {
        return events[0];
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

                if (track == structuralTrack)
                {
                    foundEvents.Add(GetEventFromStructuralTrack(track, chunk, t, c));
                }
                else
                {
                    EmotionEvent startEvent = GetEventFromChunkStart(track, chunk, t, c);
                    EmotionEvent endEvent = GetEventFromChunkEnd(track, chunk, t, c);

                    foundEvents.Add(startEvent);
                    foundEvents.Add(endEvent);
                }
            }
        }

        events.AddRange(foundEvents);
        
        foreach (EmotionEvent e in events)
            if (float.IsNaN(e.intensity))
                Debug.LogError("WHAAT" + e);
    }

    public EmotionEvent GetEventFromStructuralTrack(TrackData track, TrackChunkData chunk, int trackIndex, int chunkIndex)
    {
        EmotionEvent e = new EmotionEvent();
        e.trackIndex = trackIndex;
        e.chunkIndex = chunkIndex;
        e.associatedEmotion = chunk.startData.GetSpectrum();
        e.chunkDelimitsSegment = false;
        e.harmonicDifference = 0;

        // We only care about the ending of this chunk (for now)
        e.timestamp = chunk.end;
        e.intensity = chunk.GetIntensity(chunk.end);
        
        if (chunk.intensityCurve == IntensityCurve.LinearIncreasing)
        {
            // Buildup
            e.type = EmotionEvent.EmotionEventType.LocalMaximum;
        }
        else if(chunk.intensityCurve == IntensityCurve.LinearDecreasing)
        {
            // Decreasing
            e.type = EmotionEvent.EmotionEventType.LocalMinimum;
        }
        else
        {
            // Structure didn't change (repetition, etc)
            e.type = EmotionEvent.EmotionEventType.Sustain;
        }

        return e;
    }

    public EmotionEvent GetEventFromChunkEnd(TrackData track, TrackChunkData chunk, int trackIndex, int chunkIndex)
    {
        EmotionEvent e = new EmotionEvent();
        e.trackIndex = trackIndex;
        e.chunkIndex = chunkIndex;
        e.type = EmotionEvent.EmotionEventType.End;
        e.timestamp = chunk.end;
        e.intensity = GetIntensityForChunkEndEvent(track, chunk, chunkIndex);
        e.associatedEmotion = chunk.startData.GetSpectrum(); // TODO: expectation/surprise
        e.chunkDelimitsSegment = IsChunkSegmentDelimiter(track, chunk, chunkIndex, e.type);
        e.harmonicDifference = 0;

        if (chunkIndex > 0)
            e.harmonicDifference = Mathf.Abs(chunk.harmonySequenceNumber - track.chunks[chunkIndex - 1].harmonySequenceNumber);
         
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
        e.associatedEmotion = chunk.startData.GetSpectrum(); // TODO: expectation/surprise 
        e.chunkDelimitsSegment = IsChunkSegmentDelimiter(track, chunk, chunkIndex, e.type);
        e.harmonicDifference = 0;
        
        if (chunkIndex > 0)
            e.harmonicDifference = Mathf.Abs(chunk.harmonySequenceNumber - track.chunks[chunkIndex - 1].harmonySequenceNumber);

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
        float intensity = chunk.GetIntensity(chunk.end);
        
        if (chunkIndex < track.chunks.Count - 1)
        {
            TrackChunkData nextChunk = track.chunks[chunkIndex + 1];
            
            float timeDifference = nextChunk.start - chunk.end;
            float falloff = MeasureDurationNormalized * 4f;

            // If this is the last chunk in some time, make its ending more impactful
            if (timeDifference > falloff)
                intensity *= 2f;
        }

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

        float intensity = chunk.GetIntensity(chunk.start);
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
        this.TotalEnergySignal = new float[sampleCount];

        float dt = 1f / (float)sampleCount;
        for (int i = 0; i < sampleCount; ++i)
        {
            emotionalSignal[i] = Compute(i * dt);
            TotalEnergySignal[i] = emotionalSignal[i].GetTotalEnergy();

            if (i == 0)
                emotionalDerivativeSignal[i] = new EmotionSpectrum();
            else
                emotionalDerivativeSignal[i] = (emotionalSignal[i] - emotionalSignal[i - 1]) / dt;

        }
    }

    protected void PrecomputeExtremes()
    {
        //float minTime = 0f;
        //float maxTime = 0f;
        float min = float.MaxValue;
        float max = 0f;

        int samples = TotalEnergySignal.Length;
        float dt = 1f / (float) samples;

        for(int i = 0; i < samples; ++i)
        {
            float intensity = TotalEnergySignal[i];

            if (intensity < min)
            {
                min = intensity;
                //minTime = t;
            }

            if(intensity > max)
            {
                max = intensity;
                //maxTime = t;
            }
        }

        MinEnergy = min;
        MaxEnergy = max;
    }

    protected void PrecomputeNewEvents()
    {
        // TODO: conflict
    }

    public TrackChunkData GetCurrentStructureData(float normalizedTime)
    {
        if (structuralTrack == null)
            return null;

        foreach (TrackChunkData chunk in structuralTrack.chunks)
        {
            if (chunk.start <= normalizedTime && chunk.end >= normalizedTime)
                return chunk;
        }

        return null;
    }

    public StructureType GetStructureAtTime(float normalizedTime)
    {
        if (structuralTrack == null)
            return StructureType.None;

        foreach (TrackChunkData chunk in structuralTrack.chunks)
        {
            if (chunk.start <= normalizedTime && chunk.end >= normalizedTime)
            {
                switch (chunk.intensityCurve)
                {
                    case IntensityCurve.Invariant:
                        return StructureType.Sustain;
                    case IntensityCurve.LinearIncreasing:
                        return StructureType.Increasing;
                    case IntensityCurve.LinearDecreasing:
                        return StructureType.Decreasing;
                    case IntensityCurve.SmoothSpike:
                        return StructureType.Sustain;
                }
            }
        }

        return StructureType.None;
    }

    // This evaluation has no post process (TODO: in the future make a cache per track?)
    public EmotionSpectrum EvaluateTrack(TrackData track, float normalizedTime)
    {
        EmotionSpectrum result = new EmotionSpectrum();

        int lastChunkIndex = 0;

        for (int i = 0; i < track.chunks.Count; ++i)
        {
            TrackChunkData chunk = track.chunks[i];

            if (chunk.start <= normalizedTime && chunk.end >= normalizedTime)
            {
                result += chunk.Evaluate(normalizedTime);
                lastChunkIndex = i;
            }
        }

        // First chunk is super important!
        if (lastChunkIndex == 0)
            result *= 2f;

        return result;
    }

    public EmotionSpectrum Compute(float normalizedTime)
    {
        List<TrackChunkData> chunks = GetChunksAtTime(normalizedTime);
        EmotionSpectrum result = new EmotionSpectrum();

        foreach (TrackChunkData chunk in chunks)
            if (chunk.start <= normalizedTime && chunk.end >= normalizedTime)
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

        return maxEmotion;
    }
}
