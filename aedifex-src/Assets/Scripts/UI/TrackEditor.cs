using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackEditor : MonoBehaviour
{
    public WaveformTrack waveformTrackPrefab;
    public AbstractTrack emotionTrackPrefab;

    private List<AbstractTrack> tracks = new List<AbstractTrack>();
    private RectTransform rect;
    private float duration;

    public void Awake()
    {
        this.rect = GetComponent<RectTransform>();
    }

    public void Initialize(float baseDuration)
    {
        this.duration = baseDuration;
    }

    public void UpdateTracks(float zoom, float offset)
    {
        foreach (AbstractTrack track in tracks)
            track.UpdateTrack(zoom, offset);
    }

    public WaveformTrack InstantiateWaveformTrack(float[] samples, int downsample)
    {
        WaveformTrack track = InstantiateTrack<WaveformTrack>(waveformTrackPrefab);
        track.Initialize(samples, downsample);
        return track;
    }

    // Note: it doesn't initialize it!
    public T InstantiateTrack<T>(T trackPrefab) where T: AbstractTrack
    {
        T track = GameObject.Instantiate<T>(trackPrefab);
        tracks.Add(track);
        track.transform.SetParent(this.transform);
        return track;
    }
}