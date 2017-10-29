using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public AnimationCurve curve; // It is a bit wasteful in terms of size, but it is more flexible
    public bool isVariation;
    public int harmonySequenceNumber;
}

[System.Serializable]
public class TrackData
{
    public string trackId;
    public TrackType trackType;
    public List<TrackChunkData> chunks = new List<TrackChunkData>();
}

[System.Serializable]
public class DataContainer
{
    public List<TrackData> tracks = new List<TrackData>();
    public int beatsPerMinute = 100;
    public int beatsPerMeasure = 4;
}