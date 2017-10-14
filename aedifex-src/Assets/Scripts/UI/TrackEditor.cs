using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackEditor : MonoBehaviour
{
    public WaveformTrack waveformTrackPrefab;
    public EmotionTrack emotionTrackPrefab;

    private List<AbstractTrack> tracks = new List<AbstractTrack>();
    private RectTransform rect;
    private float duration;

    private UITimeline timeline;

    public void Awake()
    {
        this.rect = GetComponent<RectTransform>();
    }

    public void Initialize(UITimeline timeline, float baseDuration)
    {
        this.timeline = timeline;
        this.duration = baseDuration;
    }

    public void UpdateTracks(float zoom, float offset)
    {
        foreach (AbstractTrack track in tracks)
            track.UpdateTrack(zoom, offset);
    }

    public EmotionTrack InstantiateEmotionTrack()
    {
        EmotionTrack track = InstantiateTrack<EmotionTrack>(emotionTrackPrefab);
        track.Initialize(timeline);
        return track;
    }

    public WaveformTrack InstantiateWaveformTrack(float[] samples, int downsample, Color trackColor)
    {
        WaveformTrack track = InstantiateTrack<WaveformTrack>(waveformTrackPrefab);
        track.Initialize(samples, downsample, trackColor);
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