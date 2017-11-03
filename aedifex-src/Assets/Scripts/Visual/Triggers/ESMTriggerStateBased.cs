using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A trigger that will pass when other elements have a specific state.
/// Useful for inter element relationships from different SMs
/// </summary>
public class ESMTriggerStateBased : EmotionStateMachineTrigger
{
    public List<EmotionStateMachineState> states = new List<EmotionStateMachineState>();
    public EmotionStateMachineState.State expectedState = EmotionStateMachineState.State.Enabled;

    public override bool Evaluate(EmotionStateMachineState source, EmotionStateMachineState target, EmotionStateMachineData data)
    {
        bool result = true;

        foreach (EmotionStateMachineState s in states)
            result = result && s.CurrentState == expectedState;

        return result;
    }
}