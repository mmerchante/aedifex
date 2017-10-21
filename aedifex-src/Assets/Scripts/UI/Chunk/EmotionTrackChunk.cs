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
}