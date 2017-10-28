using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveformTrack : AbstractTrack
{
    private SimpleAudioVisualizer visualizer;
    private float[] waveform;

    protected override void OnAwake()
    {
        this.visualizer = this.gameObject.AddComponent<SimpleAudioVisualizer>();
    }

    public void Initialize(float[] waveform, int downsample, Color trackColor)
    {
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
        rect.GetWorldCorners(corners);
        this.visualizer.uiRect = new Rect(corners[0].x + 2.5f, corners[0].y + rect.rect.height * .5f, rect.rect.width - 5f, rect.rect.height * .85f);
        this.visualizer.lineColor = TrackColor;
    }
}