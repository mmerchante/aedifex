using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Will trigger a transition if the target state has a greater emotional affinity than the source state.
/// </summary>
public class ESMTriggerEmotionShift : EmotionStateMachineTrigger
{
    public float globalThreshold = 0f;
    public float trackThreshold = 0f;

    public override bool Evaluate(EmotionStateMachineState source, EmotionStateMachineState target, EmotionStateMachineData data)
    {
        if (target.GlobalEmotionIncidence > source.GlobalEmotionIncidence)
            return (target.GlobalEmotionIncidence - source.GlobalEmotionIncidence) > globalThreshold;

        if (target.TrackEmotionIncidence > source.TrackEmotionIncidence)
            return (target.TrackEmotionIncidence - source.TrackEmotionIncidence) > globalThreshold;

        return false;
    }
}
