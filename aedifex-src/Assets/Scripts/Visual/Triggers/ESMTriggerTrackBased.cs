using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Will trigger if specific track has already started or not, etc.
/// </summary>
public class ESMTriggerTrackBased : EmotionStateMachineTrigger
{
    public TrackCategory FilterByCategory = TrackCategory.None;

    public override bool Evaluate(EmotionStateMachineState source, EmotionStateMachineState target, EmotionStateMachineData data)
    {
        // TODO: We need to specify track information on runtime
        return false;
    }
}
