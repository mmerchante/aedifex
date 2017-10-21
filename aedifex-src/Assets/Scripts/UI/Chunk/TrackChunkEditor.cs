using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface ITrackChunkEditor<T>
{
    void Initialize(AbstractTrackChunk<T> chunk);
}

public class TrackChunkEditor<T> : MonoBehaviour, ITrackChunkEditor<T>
{
    public Text titleText;
    public AbstractTrackChunk<T> Chunk { get; protected set; }

    protected RectTransform rect;

    private Vector3[] corners = new Vector3[4];
    
    protected virtual void Awake()
    {
        this.rect = GetComponent<RectTransform>();
        Hide();
    }

    public void Initialize(AbstractTrackChunk<T> chunk)
    {
        this.Chunk = chunk;
        this.Show();
    }

    public void UpdatePosition()
    {
        Chunk.RectTransform.GetWorldCorners(corners);
        Vector2 chunkPosition = corners[0];
        this.rect.anchoredPosition = chunkPosition - Vector2.up * 10f;
    }

    public virtual void Update()
    {
        this.titleText.text = Chunk.Name;
        UpdatePosition();
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}