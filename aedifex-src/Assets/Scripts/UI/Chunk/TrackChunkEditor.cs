using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface ITrackChunkEditor<T>
{
    void Initialize(AbstractDataTrack<T> track, AbstractTrackChunk<T> chunk);
}

public class TrackChunkEditor<T> : MonoBehaviour, ITrackChunkEditor<T>
{
    public Text titleText;
    public Button copyChunkButton;
    public Button pasteChunkButton;
    public Button pasteAllChunksButton;

    public AbstractTrackChunk<T> Chunk { get; protected set; }
    public AbstractDataTrack<T> Track { get; protected set; }

    protected RectTransform rect;

    //private Vector3[] corners = new Vector3[4];

    private AbstractTrackChunk<T> ClipboardChunk = null;

    protected virtual void Awake()
    {
        this.rect = GetComponent<RectTransform>();
        this.copyChunkButton.onClick.AddListener(OnCopyChunk);
        this.pasteChunkButton.onClick.AddListener(OnPasteChunk);
        this.pasteAllChunksButton.onClick.AddListener(OnOverwriteAllChunks);
        Hide();
    }

    private void OnCopyChunk()
    {
        ClipboardChunk = Chunk;
    }

    private void OnPasteChunk()
    {
        if (Chunk != null && ClipboardChunk != null)
        {
            Chunk.CopyFromChunk(ClipboardChunk);
            OnInitialize(); 
        }
    }

    private void OnOverwriteAllChunks()
    {
        if (Chunk != null)
            Track.OverwriteAllChunks(Chunk);
    }

    public void Initialize(AbstractDataTrack<T> track, AbstractTrackChunk<T> chunk)
    {
        this.Track = track;
        this.Chunk = chunk;
        this.Show();
        OnInitialize();
    }

    protected virtual void OnInitialize()
    {
    }

    public void UpdatePosition()
    {
        //Chunk.RectTransform.GetWorldCorners(corners);
        //Vector2 chunkPosition = corners[0];
        //this.rect.anchoredPosition = chunkPosition - Vector2.up * 10f;
    }

    public virtual void Update()
    {
        if(!Chunk)
        {
            Hide();
            return;
        }

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