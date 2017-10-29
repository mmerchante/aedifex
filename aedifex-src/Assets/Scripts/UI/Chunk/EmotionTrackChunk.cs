using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmotionTrackChunk : AbstractTrackChunk<EmotionData>, PoolableObject<EmotionTrackChunk>
{
    public void OnRetrieve(ExtendablePool<EmotionTrackChunk> pool)
    {
    }

    public void OnReturn()
    {
    }

    public override TrackChunkData GetChunkData()
    {
        TrackChunkData d = base.GetChunkData();
        d.startData = Data;
        d.endData = Data;
        d.type = ChunkType.Emotion;
        return d;
    }

    public override void InitializeFromSerializedData(UITimeline timeline, AbstractDataTrack<EmotionData> track, Rect container, TrackChunkData chunk)
    {
        base.InitializeFromSerializedData(timeline, track, container, chunk);
        this.Data = chunk.startData;
    }
}