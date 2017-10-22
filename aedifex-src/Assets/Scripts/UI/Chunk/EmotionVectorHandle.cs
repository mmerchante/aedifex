using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EmotionVectorHandle : MonoBehaviour, IDragHandler, IPointerClickHandler
{
    public RectTransform RectTransform { get; protected set; }

    private RectTransform ContainerTransform;
    private Vector3[] corners = new Vector3[4];
    private EmotionChunkEditor editor;

    public void Awake()
    {
        this.RectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(EmotionChunkEditor editor, RectTransform container)
    {
        this.editor = editor;
        this.ContainerTransform = container;
    }

    public void SetPositionFromEmotion(EmotionVector v)
    {
        Vector3 offset = new Vector3(Mathf.Cos(v.angle), Mathf.Sin(v.angle), 0f) * ContainerTransform.rect.width * v.intensity;
        this.RectTransform.position = ContainerTransform.position + offset;
    }

    public EmotionVector EvaluateVector()
    {
        Vector3 diff = RectTransform.position - ContainerTransform.position;
        float angle = Mathf.Atan2(diff.y, diff.x);

        EmotionVector v = new EmotionVector();
        v.angle = angle;
        v.intensity = diff.magnitude / ContainerTransform.rect.size.x;       
        return v;
    }

    public void OnDrag(PointerEventData eventData)
    {
        ContainerTransform.GetWorldCorners(corners);

        Vector2 newPos = new Vector2(RectTransform.position.x, RectTransform.position.y) + eventData.delta;

        if(RectTransformUtility.RectangleContainsScreenPoint(ContainerTransform, newPos))
            RectTransform.anchoredPosition += eventData.delta;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            editor.RemoveHandle(this); 
    }
}