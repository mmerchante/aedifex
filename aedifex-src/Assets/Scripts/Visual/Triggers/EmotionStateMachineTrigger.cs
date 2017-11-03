using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmotionStateMachineTrigger : MonoBehaviour
{
    public virtual bool Evaluate(EmotionStateMachineState source, EmotionStateMachineState target, EmotionStateMachineData data)
    {
        return false;
    }
}