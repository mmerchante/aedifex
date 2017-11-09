using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This behaviour is the entry point for elements that react discretely
/// based on the emotion state machine. Note that for continuous effects
/// (such as color per emotion) this is not ideal, just extend EmotionBehavior
/// </summary>
public class EmotionStateMachineState : EmotionBehavior
{
    [System.Serializable]
    public enum State
    {
        Disabled = 0,
        IntroTransition = 1,
        Enabled = 2,
        OutroTransition = 3,
    }

    public State CurrentState { get; protected set; }

    public float StateTime { get; protected set; }
    protected float transitionDuration = 0f;

    protected override void OnAwake()
    {
        StateTime = 0f;
        SwitchToState(State.Disabled, true);

        foreach (InterestPoint p in interestPoints)
            p.AssociateESMState(this);
    }

    public void ForceShow()
    {
        SwitchToState(State.Enabled, true);
    }

    public void Show(float transitionDuration)
    {
        this.transitionDuration = transitionDuration;
        SwitchToState(State.IntroTransition);
    }

    public void Hide(float transitionDuration)
    {
        this.transitionDuration = transitionDuration;
        SwitchToState(State.OutroTransition);
    }

    protected void SwitchToState(State s, bool force = false)
    {
        if (!force && CurrentState == s)
            return;

        StateTime = 0f;
        Debug.Log("Switching to state " + s + ", " + gameObject.name, this);

        State prevState = CurrentState;
        this.CurrentState = s;

        switch (CurrentState)
        {
            case State.Disabled:
                OnDisabled(prevState);
                break;
            case State.IntroTransition:
                OnIntroTransition(prevState);
                break;
            case State.Enabled:
                OnEnabled(prevState);
                break;
            case State.OutroTransition:
                OnOutroTransition(prevState);
                break;
        }
    }

    protected virtual void OnDisabled(State prevState)
    {
    }

    protected virtual void OnEnabled(State prevState)
    {
    }

    protected virtual void OnOutroTransition(State prevState)
    {
    }

    protected virtual void OnIntroTransition(State prevState)
    {
    }

    protected virtual void UpdateDisabled()
    {
    }

    protected virtual void UpdateIntro()
    {
    }

    protected virtual void UpdateEnabled()
    {
    }

    protected virtual void UpdateOutro()
    {
    }

    protected override void OnUpdate()
    {
        bool switchedState = false;

        switch (CurrentState)
        {
            case State.Disabled:
                UpdateDisabled();
                break;
            case State.IntroTransition:
                UpdateIntro();

                if (StateTime >= transitionDuration)
                {
                    SwitchToState(State.Enabled);
                    switchedState = true;
                }
                break;
            case State.Enabled:
                UpdateEnabled();
                break;
            case State.OutroTransition:
                UpdateOutro();

                if (StateTime >= transitionDuration)
                {
                    SwitchToState(State.Disabled);
                    switchedState = true;
                }
                break;
        }
        
        if (!switchedState)
            StateTime += Time.deltaTime;
    }
}
