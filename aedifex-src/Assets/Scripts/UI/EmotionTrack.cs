using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EmotionTrack : AbstractTrack, IPointerClickHandler, IDragHandler
{
    public EmotionTrackChunk emotionChunkPrefab;

    private UITimeline timeline;
    private ExtendablePool<EmotionTrackChunk> chunkPool;
    private List<EmotionTrackChunk> chunks = new List<EmotionTrackChunk>();

    protected override void OnAwake()
    {
        this.chunkPool = new ExtendablePool<EmotionTrackChunk>(emotionChunkPrefab, this.transform);
        this.chunkPool.SetInitialSize(15);
    }

    public void Initialize(UITimeline timeline)
    {
        this.timeline = timeline;
    }

    protected override void OnUpdateTrack()
    {
        foreach (EmotionTrackChunk c in chunks)
            c.UpdateTrackChunk(zoom, offset);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle)
            return;

        EmotionTrackChunk chunk = chunkPool.Retrieve();
        Vector2 position = timeline.ScreenToNormalizedPosition(eventData.position);
        position.x = (position.x / zoom) + offset;
        chunk.Initialize(new EmotionData(), rect.rect, position.x, 35f);
        chunk.UpdateTrackChunk(zoom, offset);
        this.chunks.Add(chunk);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle)
            timeline.OnTimelineDrag(eventData);
    }
}
