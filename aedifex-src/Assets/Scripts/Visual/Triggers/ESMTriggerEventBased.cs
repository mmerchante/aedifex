using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ESMTriggerEventBased : EmotionStateMachineTrigger
{
    public bool isPlotPoint = false; // TODO
    public float minIntensity = 1f;

    public override bool Evaluate(EmotionStateMachineState source, EmotionStateMachineState target, EmotionStateMachineData data)
    {
        if(data.associatedEvent.type != EmotionEvent.EmotionEventType.None)
        {
            return data.associatedEvent.intensity > minIntensity;
        }

        return false;
    }
}