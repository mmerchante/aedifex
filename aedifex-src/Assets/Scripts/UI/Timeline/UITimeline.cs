using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// The timeline coordinates all user interaction with the UI,
/// and handles all UI events.
/// </summary>
[RequireComponent (typeof(RectTransform))]
public class UITimeline : MonoBehaviour
{
    private const float MIN_ZOOM = .1f;

    // In seconds
    public float CurrentIndicator { get; protected set; }
    public float CurrentIndicatorNormalized { get { return CurrentIndicator / Duration; } }
    public float Duration { get; protected set; }
    
    public float PanOffset { get; protected set; }
    public float PanOffsetNormalized { get { return PanOffset / Duration; } }
    public float Zoom { get; protected set; }

    public bool IsPlaying { get; protected set; }

    public BeatDetector audioEngine;
    public TimeSlider timeSlider;
    public TrackEditor trackEditor;

    public WaveformTrack minimapTrack;

    public AudioSource source;
    public RectTransform currentTimeIndicator;

    public Button saveButton;
    public Button loadButton;
    public Button playButton;

    private RectTransform rect;

    public void Awake()
    {
        this.Duration = 1f;
        this.rect = GetComponent<RectTransform>();
        this.playButton.onClick.AddListener(OnPlayButtonClicked);
        this.saveButton.onClick.AddListener(Save);
        this.loadButton.onClick.AddListener(Load);

        Initialize();
    }

    public void Load()
    {
        string[] files = SFB.StandaloneFileBrowser.OpenFilePanel("Open File", Application.dataPath, "json", false);

        if(files.Length == 1)
        {
            string path = files[0];
            string json = System.IO.File.ReadAllText(path);

            DataContainer container = JsonUtility.FromJson<DataContainer>(json);

            if (container != null)
                trackEditor.LoadFromTrackData(container.tracks);
        }
    }

    public void Save()
    {
        string path = SFB.StandaloneFileBrowser.SaveFilePanel("Open File", Application.dataPath, "song", "json");

        DataContainer container = new DataContainer();
        container.tracks = trackEditor.GetAllTrackData();

        if(!string.IsNullOrEmpty(path))
        {
            string json = JsonUtility.ToJson(container, true);
            System.IO.File.WriteAllText(path, json);
        }
    }

    protected void OnPlayButtonClicked()
    {
        if (!source || !source.clip)
            return;

        if (!IsPlaying)
            PlayTrack();
        else
            StopTrack();
    }

    protected void PlayTrack()
    {
        this.IsPlaying = true;
        JumpToNormalizedTime(CurrentIndicatorNormalized);
    }

    protected void StopTrack()
    {
        this.IsPlaying = false;
        this.source.Stop();
    }

    public void Initialize()
    {
        this.IsPlaying = false;
        this.Zoom = 1f;
        this.PanOffset = 0f;

        audioEngine.Initialize();
        timeSlider.Initialize(source.clip.length);
        trackEditor.Initialize(this, source.clip.length);

        minimapTrack.Initialize(audioEngine.Samples, 1024 * 8, Color.white);
        trackEditor.InstantiateWaveformTrack(audioEngine.Samples, 1024, Color.yellow, "Waveform");
        trackEditor.InstantiateWaveformTrack(audioEngine.BeatSamples, 1, Color.red, "Beat");
        
        // Add a blank image
        Image image = this.gameObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;
    }

    private void UpdateInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            OnPlayButtonClicked();
    }

    public void Update()
    {
        UpdateInput();
        UpdateZoom();

        if (IsPlaying)
            SetCurrentTimeIndicatorNormalized(source.time / source.clip.length);
        
        trackEditor.UpdateTracks(Zoom, PanOffset);

        timeSlider.Zoom = Zoom;
        timeSlider.Offset = PanOffsetNormalized;

        float indicatorOffset = (CurrentIndicatorNormalized - PanOffset) * Zoom;
        currentTimeIndicator.anchoredPosition = new Vector2(indicatorOffset * rect.rect.width, 0f);
    }

    public Rect GetTimelineRect()
    {
        return new Rect(rect.anchoredPosition, rect.rect.size);
    }

    protected void UpdateZoom()
    {
        this.Zoom = Mathf.Max(MIN_ZOOM, Zoom + Input.GetAxis("Mouse ScrollWheel"));
    }

    public bool IsPanning()
    {
        return (Input.GetKey(KeyCode.Space) && Input.GetMouseButton(0)) || Input.GetMouseButton(2);
    }

    public bool IsZooming()
    {
        return (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0)) || Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0f;
    }

    public Vector2 ScreenToNormalizedPosition(Vector2 position, bool delta = false)
    {
        if(!delta)
            position -= rect.anchoredPosition;

        return Vector2.Scale(position, new Vector2(1f / rect.rect.width, 1f / rect.rect.height));
    }

    protected void SetCurrentTimeIndicatorNormalized(float offset)
    {
        this.CurrentIndicator = Mathf.Clamp(offset * Duration, 0f, Duration);        
    }

    protected void SetPanOffsetNormalized(float offset)
    {
        this.PanOffset = Mathf.Clamp(offset * Duration, 0f, Duration);
    }

    public void JumpToNormalizedTime(float t)
    {
        this.source.time = Mathf.Clamp01(CurrentIndicatorNormalized) * source.clip.length * .99999f;

        // If the track finishes but we changed the time, we need to replay it
        if (IsPlaying && !source.isPlaying)
            source.Play();
    }
    
    public void OnTimelineDrag(PointerEventData eventData)
    {
        if(IsPanning())
        {
            // Negative drag UI philosophy
            SetPanOffsetNormalized(PanOffset - (ScreenToNormalizedPosition(eventData.delta, true).x / Zoom));
        }
        else if(IsZooming())
        {
            this.Zoom = Mathf.Max(MIN_ZOOM, Zoom +  ScreenToNormalizedPosition(eventData.delta, true).x);
        }
        else
        {
            SetCurrentTimeIndicatorNormalized((ScreenToNormalizedPosition(eventData.position).x) / Zoom + PanOffset);
            JumpToNormalizedTime(this.CurrentIndicatorNormalized);
        }
    }
    
    public void OnTimelinePointerDown(PointerEventData eventData)
    {
        if (!IsPanning() && !IsZooming())
        {
            SetCurrentTimeIndicatorNormalized((ScreenToNormalizedPosition(eventData.position).x) / Zoom + PanOffset);
            JumpToNormalizedTime(this.CurrentIndicatorNormalized);
            trackEditor.emotionChunkEditor.Hide();
        }
    }
}