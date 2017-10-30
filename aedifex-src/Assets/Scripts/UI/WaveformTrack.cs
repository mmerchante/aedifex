using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveformTrack : AbstractTrack
{
    private SimpleAudioVisualizer visualizer;
    private float[] waveform;

    protected override void OnAwake()
    {
        if(!visualizer)
            visualizer = this.gameObject.AddComponent<SimpleAudioVisualizer>();
    }

    public void InitializeWaveData(float[] waveform, int downsample, Color trackColor)
    {
        if (!visualizer)
            visualizer = this.gameObject.AddComponent<SimpleAudioVisualizer>();

        this.waveform = waveform;
        this.visualizer.Initialize(this.waveform, downsample);
        this.TrackColor = trackColor;
    }

    protected override void OnUpdateTrack()
    {
        this.visualizer.Zoom = zoom;
        this.visualizer.Offset = offset;
    }

    public void Update()
    {
        if (!timeline)
            return;

        rect.GetWorldCorners(corners);
        Rect container = new Rect(corners[0].x + 2.5f, corners[0].y + rect.rect.height * .5f, rect.rect.width - 5f, rect.rect.height * .85f);

        timeline.timelineContainerMask.GetWorldCorners(corners);
        Rect timelineRect = new Rect(corners[0].x, corners[0].y, timeline.timelineContainerMask.rect.width, timeline.timelineContainerMask.rect.height);

        bool hideWave = container.y + container.height < timelineRect.y || container.y > timelineRect.height + timelineRect.y;
        this.visualizer.enabled = !hideWave;

        if (container.y < timelineRect.y)
        {
            container.height -= timelineRect.y - container.y;
            container.y = timelineRect.y;
        }
        else
        {
            container.height = Mathf.Clamp(container.height, 0, Mathf.Abs(timelineRect.y + timelineRect.height - container.y));
        }

        this.visualizer.uiRect = container;
        this.visualizer.lineColor = TrackColor;
    }
}