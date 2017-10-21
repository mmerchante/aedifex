using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmotionChunkEditor : TrackChunkEditor<EmotionData>
{
    public RectTransform emotionShapeRect;
    public float innerRadius = .1f;
    public float outerRadius = .4f;

    private Material lineMaterial;
    private Vector3[] shapeCorners = new Vector3[4];
     
    protected override void Awake()
    {
        base.Awake();
        this.lineMaterial = new Material(Shader.Find("Unlit/Color"));
    }

    public override void Update()
    {
        base.Update();

        if (Input.GetKeyDown(KeyCode.T))
            Chunk.Data.AddVector(new EmotionVector(Random.value * Mathf.PI * 2f, Random.value));
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
        GL.Begin(GL.LINES);

        // Transform container
        container.x /= Screen.width; 
        container.width /= Screen.width;
        container.y /= Screen.height;
        container.height /= Screen.height;

        Vector3 scale = new Vector3(container.size.x, container.size.y, 0f);
        Vector3 offset = new Vector3(container.x, container.y, 0f) + scale * .5f;

        int subdivisions = 360;

        float width = Mathf.Abs(outerRadius - innerRadius);

        for(int i = 0; i < subdivisions; i++)
        {
            float t = i / (float)subdivisions;
            float angle = t * Mathf.PI * 2f;
            float r = Chunk.Data.Evaluate(angle) * width + innerRadius;

            float prevT = (i - 1) / (float)subdivisions;
            float prevAngle = prevT * Mathf.PI * 2f;
            float prevR = Chunk.Data.Evaluate(prevT * Mathf.PI * 2f) * width + innerRadius;

            Vector3 point = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * r;
            Vector3 prevPoint = new Vector3(Mathf.Cos(prevAngle), Mathf.Sin(prevAngle), 0f) * prevR;

            GL.Vertex(Vector3.Scale(prevPoint, scale) + offset);
            GL.Vertex(Vector3.Scale(point, scale) + offset);
        }

        GL.End();
        GL.PopMatrix();
    }
}
