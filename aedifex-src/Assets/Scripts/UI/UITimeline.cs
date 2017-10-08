using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent (typeof(RectTransform))]
public class UITimeline : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    // In seconds
    public float CurrentIndicator { get; protected set; }
    public float Duration { get; protected set; }
    public float CurrentIndicatorNormalized { get { return CurrentIndicator / Duration; } }
    
    public float PanOffset { get; protected set; }
    public float PanOffsetNormalized { get { return PanOffset / Duration; } }
    public float Zoom { get; protected set; }

    public RectTransform currentTimeIndicator;
    public SimpleAudioVisualizer[] signalVisualizers;

    private RectTransform rect;

    public void Awake()
    {
        this.Duration = 1f;
        this.rect = GetComponent<RectTransform>();

        Initialize();
    }

    public void Initialize()
    {
        this.Zoom = 1f;
        this.PanOffset = 0f;

        // Add a blank image
        Image image = this.gameObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;
    }

    public void Update()
    {
        for (int i = 0; i < signalVisualizers.Length; i++)
        {
            signalVisualizers[i].Offset = PanOffset;
            signalVisualizers[i].Zoom = Zoom;
            signalVisualizers[i].uiRect = new Rect(rect.anchoredPosition.x, (i+1) * 150f, rect.rect.width, 120f);
        }

        float indicatorOffset = (CurrentIndicatorNormalized - PanOffset) * Zoom;
        currentTimeIndicator.anchoredPosition = new Vector2(indicatorOffset * rect.rect.width, 0f);
    }

    public bool IsPanning()
    {
        return Input.GetKey(KeyCode.Space) && Input.GetMouseButton(0);
    }

    public bool IsZooming()
    {
        return Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0);
    }

    protected Vector2 ScreenToNormalizedPosition(Vector2 position, bool delta = false)
    {
        if(!delta)
            position -= rect.anchoredPosition;
        return Vector2.Scale(position, new Vector2(1f / rect.rect.width, 1f / rect.rect.height));
    }

    protected void SetCurrentOffsetNormalized(float offset)
    {
        this.CurrentIndicator = Mathf.Clamp(offset * Duration, 0f, Duration);
    }

    protected void SetPanOffsetNormalized(float offset)
    {
        this.PanOffset = Mathf.Clamp(offset * Duration, 0f, Duration);
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
            this.Zoom = Mathf.Max(0.000001f, Zoom +  ScreenToNormalizedPosition(eventData.delta, true).x);
        }
        else
        {
            SetCurrentOffsetNormalized((ScreenToNormalizedPosition(eventData.position).x) / Zoom + PanOffset);
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsPanning() && !IsZooming())
            SetCurrentOffsetNormalized((ScreenToNormalizedPosition(eventData.position).x) / Zoom + PanOffset);
    }
}