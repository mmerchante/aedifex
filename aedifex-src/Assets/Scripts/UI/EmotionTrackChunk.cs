using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmotionData
{
    public float emotionAngle;
    public float emotionIntensity;
}

public class EmotionTrackChunk : AbstractTrackChunk<EmotionData>, PoolableObject<EmotionTrackChunk>
{
    public void OnRetrieve(ExtendablePool<EmotionTrackChunk> pool)
    {
    }

    public void OnReturn()
    {
    }
}