using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct EmotionStateMachineData
{
    public EmotionEvent associatedEvent;
    public EmotionEventGroup associatedEventGroup;
    public float timestamp;
}

public class EmotionStateMachine : MonoBehaviour, ProceduralEventListener
{
    public EmotionStateMachineState defaultBehaviour;

    private Dictionary<EmotionStateMachineState, List<EmotionStateMachineTransition>> transitionMap = new Dictionary<EmotionStateMachineState, List<EmotionStateMachineTransition>>();

    private EmotionStateMachineState currentState;
    private EmotionStateMachineTransition currentTransition;

    public void Awake()
    {
        EmotionStateMachineTransition[] transitions = GetComponentsInChildren<EmotionStateMachineTransition>();

        // Default state is empty
        transitionMap[defaultBehaviour] = new List<EmotionStateMachineTransition>();
        defaultBehaviour.ForceShow();
        currentState = defaultBehaviour;

        foreach (EmotionStateMachineTransition t in transitions)
        {
            if (!transitionMap.ContainsKey(t.sourceBehaviour))
                transitionMap[t.sourceBehaviour] = new List<EmotionStateMachineTransition>();

            transitionMap[t.sourceBehaviour].Add(t);
        }
    }

    protected void TriggerTransition(EmotionStateMachineTransition t)
    {
        t.StartTransition();
        currentTransition = t;
    }

    private void EvaluateTransitions(EmotionStateMachineData data)
    {
        List<EmotionStateMachineTransition> transitions = transitionMap[currentState];
        foreach (EmotionStateMachineTransition t in transitions)
        {
            if (t.Evaluate(data))
            {
                TriggerTransition(t);
                return;
            }
        }
    }

    public void Update()
    {
        if (currentTransition != null && currentTransition.HasFinished())
        {
            currentState = currentTransition.targetBehaviour;
            currentTransition = null;
        }
        else if(currentTransition == null)
        {
            EmotionStateMachineData data = new EmotionStateMachineData();
            data.associatedEvent = new EmotionEvent();
            data.associatedEvent.type = EmotionEvent.EmotionEventType.None;
            data.timestamp = ProceduralEngine.Instance.CurrentTimeNormalized;

            EvaluateTransitions(data);
        }
    }

    public void OnEventDispatch(EmotionEvent e)
    {
        EmotionStateMachineData data = new EmotionStateMachineData();
        data.associatedEvent = e;
        data.timestamp = e.timestamp;
        EvaluateTransitions(data);
    }

    public void OnEventGroupDispatch(EmotionEventGroup g)
    {
        EmotionStateMachineData data = new EmotionStateMachineData();
        data.associatedEventGroup = g;
        data.timestamp = ProceduralEngine.Instance.CurrentTimeNormalized;
        EvaluateTransitions(data);
    }
}
