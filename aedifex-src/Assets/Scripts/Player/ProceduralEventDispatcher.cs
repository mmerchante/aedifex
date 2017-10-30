using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmotionEventGroup
{
    public List<EmotionEvent> events = new List<EmotionEvent>();
}

public interface ProceduralEventListener
{
    void OnEventGroupDispatch(EmotionEventGroup g);
    void OnEventDispatch(EmotionEvent e);
}

/// <summary>
/// This event dispatcher subdivides time into steps, and accumulates events on each
/// step. It then dispatches events based on the current time, but can be queried for
/// future information to make interesting decisions.
/// The reason for this is that although separate events are interesting, event groups have 
/// a lot more coherency and information; however, we may want to also look for event groups 
/// in a time window to make camera decisions (which group is the most important? etc)
/// </summary>
public class ProceduralEventDispatcher : MonoBehaviour
{
    public int eventResolutionPerBeat = 8; // The minimum time to accumulate events

    private EmotionEventGroup[] eventGroups = null;
    private Queue<EmotionEvent> currentQueue = new Queue<EmotionEvent>();

    private List<ProceduralEventListener> listeners = new List<ProceduralEventListener>();

    private float timeCounter = 0f;
    private float timeResolution = 1f;

    public void Initialize()
    {
        timeResolution = ProceduralEngine.Instance.EmotionEngine.BeatDuration / (float) eventResolutionPerBeat;
        GatherEvents();
    }

    public int GetGroupIndexForNormalizedTime(float t)
    {
        return Mathf.Clamp(Mathf.FloorToInt(t / (timeResolution / ProceduralEngine.Instance.Duration)), 0, eventGroups.Length - 1);
    }

    protected void GatherEvents()
    {
        int groupsCount = Mathf.CeilToInt(ProceduralEngine.Instance.Duration / timeResolution);
        eventGroups = new EmotionEventGroup[groupsCount];

        Queue<EmotionEvent> queue = ProceduralEngine.Instance.EmotionEngine.BuildEventQueue();

        while(queue.Count > 0)
        {
            EmotionEvent e = queue.Dequeue();

            int index = GetGroupIndexForNormalizedTime(e.timestamp);

            if (eventGroups[index] == null)
                eventGroups[index] = new EmotionEventGroup();

            eventGroups[index].events.Add(e);
        }
    }

    public List<EmotionEventGroup> GetFutureEventGroups(float timeOffsetNormalized)
    {
        List<EmotionEventGroup> list = new List<EmotionEventGroup>();

        int startGroup = GetGroupIndexForNormalizedTime(ProceduralEngine.Instance.CurrentTimeNormalized) + 1;
        int endGroup = GetGroupIndexForNormalizedTime(ProceduralEngine.Instance.CurrentTimeNormalized + timeOffsetNormalized);

        if(startGroup != endGroup && startGroup < endGroup)
        {
            for (int i = startGroup; i <= endGroup; ++i)
                list.Add(eventGroups[i]);
        }

        return list;
    }
    
    public void UpdateEvents(float currentTimeNormalized)
    {
        // Event group dispatch
        if (timeCounter >= timeResolution)
        {
            timeCounter = 0f;

            int currentGroup = GetGroupIndexForNormalizedTime(currentTimeNormalized);
            EmotionEventGroup g = eventGroups[currentGroup];

            if(g != null)
            {
                foreach (ProceduralEventListener l in listeners)
                    l.OnEventGroupDispatch(g);

                // Add all new future events to the queue
                foreach (EmotionEvent e in g.events)
                    currentQueue.Enqueue(e);
            
                Debug.Log("Dispatched a group with " + g.events.Count + " events. Index: " + currentGroup);
            }
        }
        else
        {
            timeCounter += Time.deltaTime;
        }

        // Dispatch actual events
        while(currentQueue.Count > 0 && currentQueue.Peek().timestamp < currentTimeNormalized)
        {
            // Consume it
            EmotionEvent e = currentQueue.Dequeue();

            foreach (ProceduralEventListener l in listeners)
                l.OnEventDispatch(e);

            Debug.Log("Dispatched event: " + e.ToString() + " at time " + currentTimeNormalized);
        }
    }
}
