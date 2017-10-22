using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrackEditor : MonoBehaviour
{
    public Button addTrackButton;

    public EmotionChunkEditor emotionChunkEditor;

    public TrackHeader headerPrefab;
    public WaveformTrack waveformTrackPrefab;
    public EmotionTrack emotionTrackPrefab;

    public RectTransform headerContainer;

    private List<AbstractTrack> tracks = new List<AbstractTrack>();
    private RectTransform rect;
    private float duration;

    private UITimeline timeline;

    public void Awake()
    {
        this.rect = GetComponent<RectTransform>();
        this.addTrackButton.onClick.AddListener(OnAddTrackButtonClicked);
    }

    protected void OnAddTrackButtonClicked()
    {
        InstantiateEmotionTrack();
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
        EmotionTrack track = InstantiateTrack<EmotionTrack>(emotionTrackPrefab, "Emotion track");
        track.Initialize(timeline);
        return track;
    }

    public WaveformTrack InstantiateWaveformTrack(float[] samples, int downsample, Color trackColor, string name)
    {
        WaveformTrack track = InstantiateTrack<WaveformTrack>(waveformTrackPrefab, name);
        track.Initialize(samples, downsample, trackColor);
        return track;
    }

    // Note: it doesn't initialize it!
    public T InstantiateTrack<T>(T trackPrefab, string name) where T: AbstractTrack
    {
        T track = GameObject.Instantiate<T>(trackPrefab);
        tracks.Add(track);
        track.transform.SetParent(this.transform);
        track.TrackName = name;

        TrackHeader header = InstantiateHeader();
        header.Initialize(track);

        return track;
    }

    private TrackHeader InstantiateHeader()
    {
        TrackHeader h = GameObject.Instantiate<TrackHeader>(headerPrefab);
        h.transform.SetParent(headerContainer);
        return h;
    }

    public ITrackChunkEditor<T> GetChunkEditor<T>()
    {
        if (typeof(T).IsAssignableFrom(typeof(EmotionData)))
            return (ITrackChunkEditor<T>) emotionChunkEditor;

        return null;
    }
}