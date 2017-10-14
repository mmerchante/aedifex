using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class TimelineEventHandler : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    public UITimeline timeline;

    public void OnDrag(PointerEventData eventData)
    {
        timeline.OnTimelineDrag(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        timeline.OnTimelinePointerDown(eventData);
    }
}
