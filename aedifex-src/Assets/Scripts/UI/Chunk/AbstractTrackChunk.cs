using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AbstractTrackChunk<T> : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public string Name { get; set; } // By default, track name
    public float Position { get; set; }
    public float Width { get; set; }
    public bool Snap { get; set; }

    public Text text;
    public RectTransform textContainer;
    public Image chunkBackground;
    public Image resizeHandle;

    private float zoom;
    private float offset;
    public RectTransform RectTransform { get; protected set; }
    private Rect container;
    private UITimeline timeline;
    private AbstractDataTrack<T> track; // TODO: refactor to something less specific

    public T Data { get; protected set; }

    private bool resizing = false;

    public virtual TrackChunkData GetChunkData()
    {
        TrackChunkData d = new TrackChunkData();
        d.type = ChunkType.None;
        d.start = Snap ? GetSnappedPosition(Position) : Position;
        d.end = Snap ? GetSnappedPosition(Position + Width) : (Position + Width);
        return d;
    }

    public void Awake()
    {
        this.RectTransform = GetComponent<RectTransform>();
    }

    public virtual void Initialize(UITimeline timeline, AbstractDataTrack<T> track, Rect container, TrackChunkData chunk)
    {
        this.track = track;
        this.timeline = timeline;
        this.container = container;
        this.Position = chunk.start;
        this.Width = chunk.end - chunk.start;
        this.Snap = true; // For now...
        UpdatePosition();

        UpdateChunkName(track.TrackName);
        chunkBackground.color = track.TrackColor;
    }

    public void Initialize(UITimeline timeline, AbstractDataTrack<T> track, T data, Rect container, float position, float width)
    {
        this.track = track;
        this.timeline = timeline;
        this.container = container;
        this.Position = position;
        this.Width = width;
        this.Data = data;
        this.Snap = true; // For now...
        UpdatePosition();

        UpdateChunkName(track.TrackName);
        chunkBackground.color = track.TrackColor;
    }

    public void UpdateChunkName(string name)
    {
        this.Name = name;
        text.text = name;

        TextGenerator textGen = new TextGenerator();
        TextGenerationSettings generationSettings = text.GetGenerationSettings(text.rectTransform.rect.size);
        float width = textGen.GetPreferredWidth(name, generationSettings);
        textContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    public float GetSnappedPosition(float position)
    {
        int bpm = timeline.CurrentBPM;
        float secondsPerBeat = 60f / (float)bpm;
        return Mathf.Round(position * timeline.Duration / secondsPerBeat) * secondsPerBeat / timeline.Duration;
    }

    public void UpdateTrackChunk(float zoom, float offset)
    {      
        this.zoom = zoom;
        this.offset = offset;
        UpdatePosition();
    }    

    protected void UpdatePosition()
    {
        float effectivePosition = Position;
        float effectiveWidth = Width;

        if (Snap)
        {
            effectivePosition = GetSnappedPosition(Position);
            effectiveWidth = GetSnappedPosition(Position + Width) - effectivePosition;
        }

        this.RectTransform.anchoredPosition = new Vector2((effectivePosition - offset) * zoom * container.width, 0f);
        this.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, effectiveWidth * zoom * container.width);
    }

    public void Update()
    {
        UpdatePosition();
        UpdateChunkName(track.TrackName);
    }

    public Vector2 ScreenToNormalizedPosition(Vector2 position, bool delta = false)
    {
        if (!delta)
            position -= container.position;

        return Vector2.Scale(position, new Vector2(1f / container.width, 1f / container.height));
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle)
        {
            timeline.OnTimelineDrag(eventData);
            return;
        }

        if (resizing)
        {
            float nextWidth = Mathf.Clamp01(this.Width + ScreenToNormalizedPosition(eventData.delta, true).x / zoom);
            float minWidth = .001f;

            if(Snap)
            {
                int bpm = timeline.CurrentBPM;
                float secondsPerBeat = 60f / (float)bpm;
                minWidth = secondsPerBeat / timeline.Duration; // One beat minimum if it is snapped
            }

            // Min size is 1/1000th
            if (nextWidth > minWidth && track.CanPlaceTrackChunk(Position, nextWidth, this))
                this.Width = nextWidth;
        }
        else
        {
            float nextPosition = Mathf.Clamp01(this.Position + ScreenToNormalizedPosition(eventData.delta, true).x / zoom);

            if (track.CanPlaceTrackChunk(nextPosition, Width, this))
                this.Position = nextPosition;
        }
    }

    public bool CanResizeFromPosition(Vector2 p)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(resizeHandle.rectTransform, p);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        resizing = CanResizeFromPosition(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        resizing = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ITrackChunkEditor<T> editor = timeline.trackEditor.GetChunkEditor<T>();

        if(editor != null)
            editor.Initialize(track, this);
    }
}