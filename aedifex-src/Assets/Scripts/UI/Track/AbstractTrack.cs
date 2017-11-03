using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbstractTrack : MonoBehaviour
{
    public string TrackName { get; set; }
    public Color TrackColor { get; protected set; }
    public TrackCategory TrackCategory { get; set; }
    public int TrackId { get; protected set; }

    protected UITimeline timeline;
    protected RectTransform rect;
    protected float trackDuration;
    protected float zoom;
    protected float offset;
    protected int bpm;
    protected int bpb;

    private Material lineMaterial;
    protected Vector3[] corners = new Vector3[4];

    public void Awake()
    {
        this.lineMaterial = new Material(Shader.Find("Unlit/Color"));

        this.rect = GetComponent<RectTransform>();
        this.TrackName = "New track";
        this.zoom = 1f;
        this.offset = 0f;
        this.bpm = 20;
        this.bpb = 4;
        this.TrackColor = Color.white;
        OnAwake();
    }

    protected virtual void OnAwake()
    {
    }

    public void Initialize(UITimeline timeline, int id)
    {
        this.TrackId = id;
        this.timeline = timeline;
        this.trackDuration = timeline.Duration;
        this.TrackColor = Random.ColorHSV(0f, 1f, 1f, 1f, .75f, 1f);
        OnInitialize();
    }

    protected virtual void OnInitialize()
    {
    }

    public float GetTrackHeight()
    {
        return rect.rect.height;
    }
    
    public void UpdateTrack(float zoom, float offset, int bpm, int bpb)
    {
        this.zoom = zoom;
        this.offset = offset;
        this.bpm = bpm;
        this.bpb = bpb;
        OnUpdateTrack();
    }

    public void OnGUI()
    {
        if (Event.current.type != EventType.Repaint)
            return;

        this.lineMaterial.color = TrackColor;
        rect.GetWorldCorners(corners);
        DrawBeatLines(new Rect(corners[0].x, corners[0].y + 2f, rect.rect.width, rect.rect.height - 2f));
    }

    protected void DrawBeatLines(Rect container)
    {
        if (!timeline)
            return;

        timeline.timelineContainerMask.GetWorldCorners(corners);
        Rect timelineRect = new Rect(corners[0].x, corners[0].y, timeline.timelineContainerMask.rect.width, timeline.timelineContainerMask.rect.height);

        if (container.y + container.height < timelineRect.y || container.y > timelineRect.height + timelineRect.y)
            return;

        if (container.y < timelineRect.y)
        {
            container.height -= timelineRect.y - container.y;
            container.y = timelineRect.y;
        }
        else
        {
            container.height = Mathf.Clamp(container.height, 0, Mathf.Abs(timelineRect.y + timelineRect.height - container.y));
        }
            
        GL.PushMatrix();
        lineMaterial.SetPass(0);
        GL.LoadOrtho();
        GL.Begin(GL.LINES);
        
        // Transform container
        container.x /= Screen.width;
        container.width /= Screen.width;
        container.y /= Screen.height;
        container.height /= Screen.height;

        double secondsPerTick = 60.0 / bpm;

        int ticks = (int)((trackDuration) / secondsPerTick);
        float scaledOffset = offset * container.width * zoom;

        for (int i = 0; i < ticks; ++i)
        {
            float t = (i / (float)ticks);
            float x = container.x + t * container.width * zoom - scaledOffset;

            if (x < container.x || x > container.width + container.x)
                continue;

            bool accent = i % bpb == 0;
            GL.Vertex(new Vector3(x, container.y, 0f));
            GL.Vertex(new Vector3(x, container.y + container.height * (accent ? 1f : .25f), 0f));
        }

        GL.End();
        GL.PopMatrix();
    }

    protected virtual void OnUpdateTrack()
    {
    }

    public virtual TrackData GetTrackData()
    {
        TrackData d = new TrackData();
        d.trackName = TrackName;
        d.trackType = TrackType.None;
        d.category = TrackCategory;
        d.id = TrackId;
        return d;
    }

    public virtual void LoadFromData(TrackData data)
    {
        TrackCategory = data.category;

        // If id is valid
        if(data.id > 0)
            TrackId = data.id;
    }
}