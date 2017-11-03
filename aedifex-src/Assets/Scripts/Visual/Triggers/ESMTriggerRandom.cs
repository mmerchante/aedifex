using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ESMTriggerRandom : EmotionStateMachineTrigger
{
    // Note that this random is going to be evaluated on every frame!
    public float randomThreshold = 0f;

    public override bool Evaluate(EmotionStateMachineState source, EmotionStateMachineState target, EmotionStateMachineData data)
    {
        return ProceduralEngine.Instance.RNG.NextDouble() > randomThreshold;
    }
}
