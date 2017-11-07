using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrackEditor : MonoBehaviour
{
    public Button addTrackButton;
    public GameObject trackInfoContainer;
    public Text selectedTrackNameLabel;
    public Dropdown selectedTrackCategoryDropdown;

    public EmotionChunkEditor emotionChunkEditor;

    public TrackHeader headerPrefab;
    public WaveformTrack waveformTrackPrefab;
    public EmotionTrack emotionTrackPrefab;

    public RectTransform headerContainer;

    private List<AbstractTrack> tracks = new List<AbstractTrack>();

    private UITimeline timeline;
    private AbstractTrack selectedTrack;

    public void Awake()
    {
        this.addTrackButton.onClick.AddListener(OnAddTrackButtonClicked);

        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

        foreach (string n in System.Enum.GetNames(typeof(TrackCategory)))
            options.Add(new Dropdown.OptionData(n));

        selectedTrackCategoryDropdown.options = options;
    }

    public void Update()
    {
        trackInfoContainer.SetActive(selectedTrack != null);

        if(selectedTrack != null)
        {
            selectedTrackNameLabel.text = selectedTrack.TrackName + "-" + selectedTrack.TrackId;
            selectedTrack.TrackCategory = (TrackCategory)selectedTrackCategoryDropdown.value;
        }
    }

    protected void OnAddTrackButtonClicked()
    {
        InstantiateEmotionTrack();
    }

    public void Initialize(UITimeline timeline, float baseDuration)
    {
        this.timeline = timeline;
    }

    public void UpdateTracks(float zoom, float offset, int bpm, int bpb)
    {
        foreach (AbstractTrack track in tracks)
            track.UpdateTrack(zoom, offset, bpm, bpb);
    }

    private int GetNextId()
    {
        int maxId = 0;

        foreach (AbstractTrack t in tracks)
            maxId = Mathf.Max(t.TrackId, maxId);

        return maxId + 1;
    }

    public EmotionTrack InstantiateEmotionTrack(string name = "Emotion Track")
    {
        EmotionTrack track = InstantiateTrack<EmotionTrack>(emotionTrackPrefab, name);
        track.Initialize(timeline, GetNextId());
        return track;
    }

    public WaveformTrack InstantiateWaveformTrack(float[] samples, int downsample, Color trackColor, string name)
    {
        WaveformTrack track = InstantiateTrack<WaveformTrack>(waveformTrackPrefab, name);
        track.Initialize(timeline, 0);
        track.InitializeWaveData(samples, downsample, trackColor);
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
        header.Initialize(this, track);

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

    // TODO: Clean everything before... no time for that! Just reset the app
    public void LoadFromTrackData(List<TrackData> data)
    {
        foreach(TrackData t in data)
        {
            if(t.trackType == TrackType.Emotion)
            {
                EmotionTrack track = InstantiateEmotionTrack(t.trackName);
                track.LoadFromData(t);
            }
        }
    }

    public List<TrackData> GetAllTrackData ()
    {
        List<TrackData> list = new List<TrackData>();

        foreach (AbstractTrack t in tracks)
            list.Add(t.GetTrackData());

        return list;
    }

    public void SelectTrack(AbstractTrack track)
    {
        this.selectedTrack = track;
        this.selectedTrackCategoryDropdown.value = (int)selectedTrack.TrackCategory;
    }
}