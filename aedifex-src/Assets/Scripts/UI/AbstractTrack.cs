using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbstractTrack : MonoBehaviour
{
    public string TrackName { get; set; }

    protected Color trackColor;
    protected RectTransform rect;
    protected float trackDuration;
    protected float zoom;
    protected float offset;    

    public void Awake()
    {
        this.rect = GetComponent<RectTransform>();
        this.TrackName = "AbstractTrack";
        this.zoom = 1f;
        this.offset = 0f;
        this.trackColor = Color.white;
        OnAwake();
    }

    protected virtual void OnAwake()
    {
    }
    
    public void UpdateTrack(float zoom, float offset)
    {
        this.zoom = zoom;
        this.offset = offset;
        OnUpdateTrack();
    }
    
    protected virtual void OnUpdateTrack()
    {
    }
}