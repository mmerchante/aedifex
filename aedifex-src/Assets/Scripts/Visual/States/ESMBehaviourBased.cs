using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ESMBehaviourBased : EmotionStateMachineState
{
    public MonoBehaviour[] behaviours;

    protected override void OnEnabled(State prevState)
    {
        foreach (MonoBehaviour c in behaviours)
            c.enabled = true;
    }

    protected override void OnDisabled(State prevState)
    {
        foreach (MonoBehaviour c in behaviours)
            c.enabled = false;
    }
}