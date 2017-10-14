using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbstractTrackChunk<T> : MonoBehaviour
{
    public string Name { get; set; } // By default, track name
    public float Position { get; set; }
    public float Width { get; set; }

    private float zoom;
    private float offset;
    private RectTransform rect;
    private Rect container;

    public T Data { get; protected set; }

    public void Awake()
    {
        this.rect = GetComponent<RectTransform>();
    }

    public void Initialize(T data, Rect container, float position, float width)
    {
        this.container = container;
        this.Position = position;
        this.Width = width;
        this.Data = data;
        OnInitialize();
        UpdatePosition();
    }

    protected virtual void OnInitialize()
    {
    }

    public void UpdateTrackChunk(float zoom, float offset)
    {
        this.zoom = zoom;
        this.offset = offset;
        UpdatePosition();
    }    

    protected void UpdatePosition()
    {
        this.rect.anchoredPosition = new Vector2((Position - offset) * zoom * container.width, 0f);
        this.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Width * zoom);
    }

    public void Update()
    {
        UpdatePosition();
    }
}
