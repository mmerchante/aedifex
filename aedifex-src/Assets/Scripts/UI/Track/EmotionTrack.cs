using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EmotionTrack : AbstractDataTrack<EmotionData>
{
    public EmotionTrackChunk emotionChunkPrefab;

    private ExtendablePool<EmotionTrackChunk> chunkPool;

    public override EmotionData CopyData(EmotionData data)
    {
        return new EmotionData(data);
    }

    protected override EmotionData GetDefaultData()
    {
        return new EmotionData();
    }

    protected override AbstractTrackChunk<EmotionData> InstanceChunk()
    {
        return chunkPool.Retrieve();
    }

    protected override void OnAwake()
    {
        this.chunkPool = new ExtendablePool<EmotionTrackChunk>(emotionChunkPrefab, this.transform);
        this.chunkPool.SetInitialSize(15);
    }

    public override TrackData GetTrackData()
    {
        TrackData d = base.GetTrackData();
        d.trackType = TrackType.Emotion;
        return d;
    }
}
