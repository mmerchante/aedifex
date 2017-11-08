using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ESMBehaviourBased : ESMSimpleState
{
    public MonoBehaviour[] behaviours;

    protected override void OnEnabled(State prevState)
    {
        base.OnEnabled(prevState);

        foreach (MonoBehaviour c in behaviours)
            c.enabled = true;
    }

    protected override void OnDisabled(State prevState)
    {
        base.OnDisabled(prevState);

        foreach (MonoBehaviour c in behaviours)
            c.enabled = false;
    }
}