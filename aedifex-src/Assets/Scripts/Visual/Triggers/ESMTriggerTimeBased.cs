using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Will trigger if the state time is greater than a threshold
/// </summary>
public class ESMTriggerTimeBased : EmotionStateMachineTrigger
{
    public float minTime = 1f;
    public float globalTimeNormalized = 0f;

    public override bool Evaluate(EmotionStateMachineState source, EmotionStateMachineState target, EmotionStateMachineData data)
    {
        return source.StateTime >= minTime || ProceduralEngine.Instance.CurrentTimeNormalized > globalTimeNormalized;
    }
}