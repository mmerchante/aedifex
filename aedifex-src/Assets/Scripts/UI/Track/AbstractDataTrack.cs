using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

public abstract class AbstractDataTrack<T> : AbstractTrack, IPointerClickHandler, IDragHandler
{
    protected List<AbstractTrackChunk<T>> chunks = new List<AbstractTrackChunk<T>>();

    // Abstract methods
    protected abstract AbstractTrackChunk<T> InstanceChunk();
    protected abstract T GetDefaultData();
    public abstract T CopyData(T data);

    protected override void OnUpdateTrack()
    {
        foreach (AbstractTrackChunk<T> c in chunks)
            c.UpdateTrackChunk(zoom, offset);
    }

    public void RemoveChunk(AbstractTrackChunk<T> chunk)
    {
        chunks.Remove(chunk);
        GameObject.Destroy(chunk.gameObject);
    }

    public void OverwriteAllChunks(AbstractTrackChunk<T> chunk)
    {
        foreach (AbstractTrackChunk<T> c in chunks)
            c.CopyFromChunk(chunk);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle)
            return;

        Vector2 position = timeline.ScreenToNormalizedPosition(eventData.position);
        position.x = (position.x / zoom) + offset;

        float width = .05f / zoom;

        if (CanPlaceTrackChunk(position.x, width))
        {
            T lastData = GetDefaultData();

            AbstractTrackChunk<T> chunk = InstanceChunk();
            chunk.Initialize(timeline, this, lastData, timeline.GetTimelineRect(), position.x, width);

            if (chunks.Count > 0)
            {
                AbstractTrackChunk<T> lastClosest = chunks.FindLast(x => x.Position < position.x);

                if (lastClosest)
                    chunk.CopyFromChunk(lastClosest);
                else
                    chunk.CopyFromChunk(chunks[chunks.Count - 1]);
            }

            chunk.UpdateTrackChunk(zoom, offset);
            this.chunks.Add(chunk);
        }
    }

    public bool CanPlaceTrackChunk(float position, float width, AbstractTrackChunk<T> ignore = null)
    {
        if (position + width > 1f)
            return false;

        foreach (AbstractTrackChunk<T> c in chunks)
        {
            if (c != ignore)
            {
                if ((position + width > c.Position && position + width < c.Position + c.Width) ||
                    (position > c.Position && position < c.Position + c.Width))
                    return false;
            }
        }

        return true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle)
            timeline.OnTimelineDrag(eventData);
    }

    public override TrackData GetTrackData()
    {
        TrackData d = base.GetTrackData();

        foreach (AbstractTrackChunk<T> chunk in chunks)
            d.chunks.Add(chunk.GetChunkData());

        return d;
    }

    public override void LoadFromData(TrackData data)
    {
        base.LoadFromData(data);

        foreach (TrackChunkData c in data.chunks)
        {
            AbstractTrackChunk<T> chunk = InstanceChunk();
            chunk.InitializeFromSerializedData(timeline, this, timeline.GetTimelineRect(), c);
            chunk.UpdateTrackChunk(zoom, offset);
            this.chunks.Add(chunk);
        }
    }
}
