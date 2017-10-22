using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EmotionChunkEditor : TrackChunkEditor<EmotionData>, IPointerClickHandler
{
    public EmotionVectorHandle emotionHandlePrefab;
    public RectTransform emotionShapeRect;
    public float innerRadius = .1f;
    public float outerRadius = .4f;

    private Material lineMaterial;
    private Vector3[] shapeCorners = new Vector3[4];
    private List<EmotionVectorHandle> handles = new List<EmotionVectorHandle>();
     
    protected override void Awake()
    {
        base.Awake();
        this.lineMaterial = new Material(Shader.Find("Unlit/LineShader"));
    }

    protected override void OnInitialize()
    {
        foreach (EmotionVectorHandle h in handles)
            GameObject.Destroy(h.gameObject);

        handles.Clear();

        foreach(EmotionVector v in Chunk.Data.vectors)
        {
            EmotionVectorHandle handle = InstantiateHandle();
            handle.SetPositionFromEmotion(v);
        }
    }

    public override void Update()
    {
        base.Update();

        Chunk.Data.Clear();

        foreach (EmotionVectorHandle h in handles)
            Chunk.Data.AddVector(h.EvaluateVector());
    }

    public void OnGUI()
    {
        emotionShapeRect.GetWorldCorners(shapeCorners);
        DrawEmotionShape(new Rect(shapeCorners[0], shapeCorners[2] - shapeCorners[0]), Color.magenta);
    }

    protected void DrawEmotionShape(Rect container, Color color)
    {
        GL.PushMatrix();
        lineMaterial.SetPass(0);
        GL.LoadOrtho();
        GL.Begin(GL.LINE_STRIP);
        
        // Transform container
        container.x /= Screen.width; 
        container.width /= Screen.width;
        container.y /= Screen.height;
        container.height /= Screen.height;

        Vector3 scale = new Vector3(container.size.x, container.size.y, 0f);
        Vector3 offset = new Vector3(container.x, container.y, 0f) + scale * .5f;

        int subdivisions = 360;

        float width = Mathf.Abs(outerRadius - innerRadius);

        for(int i = 0; i < subdivisions + 1; i++)
        {
            float t = i / (float)subdivisions;
            float angle = t * Mathf.PI * 2f;
            float r = Chunk.Data.Evaluate(angle) * width + innerRadius;

            GL.Color(Color.HSVToRGB(t, 1f, 1f));
            
            Vector3 point = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * r;
            GL.Vertex(Vector3.Scale(point, scale) + offset);
        }

        GL.End();
        GL.PopMatrix();
    }

    private EmotionVectorHandle InstantiateHandle()
    {
        EmotionVectorHandle handle = GameObject.Instantiate<EmotionVectorHandle>(emotionHandlePrefab);
        handle.RectTransform.SetParent(rect, true);
        handle.Initialize(this, emotionShapeRect);
        handles.Add(handle);
        return handle;
    }

    public void RemoveHandle(EmotionVectorHandle h)
    {
        GameObject.Destroy(h.gameObject);
        handles.Remove(h);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(emotionShapeRect, eventData.position))
        {
            EmotionVectorHandle handle = InstantiateHandle();
            handle.RectTransform.position = new Vector3(eventData.position.x, eventData.position.y, 0f);
        }
    }
}
