using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveformTrack : AbstractTrack
{
    private SimpleAudioVisualizer visualizer;
    private float[] waveform;
    private Vector3[] corners = new Vector3[4];

    protected override void OnAwake()
    {
        this.visualizer = this.gameObject.AddComponent<SimpleAudioVisualizer>();
    }

    public void Initialize(float[] waveform, int downsample)
    {
        this.waveform = waveform;
        this.visualizer.Initialize(waveform, downsample);
    }

    protected override void OnUpdateTrack()
    {
        this.visualizer.Zoom = zoom;
        this.visualizer.Offset = offset;

        rect.GetWorldCorners(corners);
        this.visualizer.uiRect = new Rect(corners[0].x, corners[0].y + rect.rect.height * .5f, rect.rect.width, rect.rect.height * .85f);
    }
}