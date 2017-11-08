using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmotionStateMachineTransition : MonoBehaviour
{
    public EmotionStateMachineState sourceBehaviour;
    public EmotionStateMachineState targetBehaviour;
    public float transitionDuration = .5f;

    private List<EmotionStateMachineTrigger> triggers;

    public void Awake()
    {
        this.triggers = new List<EmotionStateMachineTrigger>(GetComponents<EmotionStateMachineTrigger>());
    }

    public void StartTransition()
    {
        sourceBehaviour.Hide(transitionDuration);
        targetBehaviour.Show(transitionDuration);
    }

    public bool HasFinished()
    {
        return sourceBehaviour.CurrentState == EmotionStateMachineState.State.Disabled
            && sourceBehaviour.CurrentState == EmotionStateMachineState.State.Enabled;
    }

    public bool Evaluate(EmotionStateMachineData data)
    {
        bool canTrigger = true;

        foreach (EmotionStateMachineTrigger t in triggers)
            canTrigger = canTrigger && t.Evaluate(sourceBehaviour, targetBehaviour, data);

        return canTrigger;
    }
}