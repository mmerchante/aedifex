using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ESMSimpleState : EmotionStateMachineState
{
    public GameObject container;

    protected override void OnDisabled(State prevState)
    {
        if(container)
            container.SetActive(false);
    }

    protected override void OnEnabled(State prevState)
    {
        if(container)
            container.SetActive(true);
    }
}
