using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ESMTriggerEnergyBased : EmotionStateMachineTrigger
{
    public float minEnergy = 1f;

    public override bool Evaluate(EmotionStateMachineState source, EmotionStateMachineState target, EmotionStateMachineData data)
    {
        return ProceduralEngine.Instance.GetCurrentEmotion().GetTotalEnergy() > minEnergy;
    }
}