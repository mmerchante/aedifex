using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent (typeof(RectTransform))]
public class UITimeline : MonoBehaviour, IDragHandler, IPointerDownHandler
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

    public AudioSource source;
    public RectTransform currentTimeIndicator;

    public Button playButton;

    private RectTransform rect;

    public void Awake()
    {
        this.Duration = 1f;
        this.rect = GetComponent<RectTransform>();
        this.playButton.onClick.AddListener(OnPlayButtonClicked);

        Initialize();
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
        trackEditor.Initialize(source.clip.length);
        
        trackEditor.InstantiateWaveformTrack(audioEngine.Samples, 1024 * 4);
        trackEditor.InstantiateWaveformTrack(audioEngine.BeatSamples, 1);

        // Add a blank image
        Image image = this.gameObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;
    }

    public void Update()
    {
        UpdateZoom();

        if (IsPlaying)
            SetCurrentTimeIndicatorNormalized(source.time / source.clip.length);
        
        trackEditor.UpdateTracks(Zoom, PanOffset);

        timeSlider.Zoom = Zoom;
        timeSlider.Offset = PanOffsetNormalized;

        float indicatorOffset = (CurrentIndicatorNormalized - PanOffset) * Zoom;
        currentTimeIndicator.anchoredPosition = new Vector2(indicatorOffset * rect.rect.width, 0f);
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

    protected Vector2 ScreenToNormalizedPosition(Vector2 position, bool delta = false)
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
    
    public void OnDrag(PointerEventData eventData)
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
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsPanning() && !IsZooming())
        {
            SetCurrentTimeIndicatorNormalized((ScreenToNormalizedPosition(eventData.position).x) / Zoom + PanOffset);
            JumpToNormalizedTime(this.CurrentIndicatorNormalized);
        }
    }
}