using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EmotionVisualizer : MonoBehaviour
{
    public float innerRadius = .1f;
    public float outerRadius = .4f;

    private EmotionEngine engine;
    private Material lineMaterial;
    private RectTransform rect;
    private Vector3[] shapeCorners = new Vector3[4];
    private UITimeline timeline;
    private EmotionSpectrum currentSpectrum = new EmotionSpectrum();

    protected  void Awake()
    {
        this.lineMaterial = new Material(Shader.Find("Unlit/LineShader"));
        this.rect = GetComponent<RectTransform>();
    }

    public void Initialize(UITimeline timeline, float[] samples, DataContainer container)
    {
        this.timeline = timeline;
        engine = new EmotionEngine();
        engine.Initialize(timeline.Duration, samples, container, 1024);
        engine.Precompute();
    }

    public EmotionEngine GetEmotionEngine()
    {
        return engine;
    }

    public void OnGUI()
    {
        if (Event.current.type != EventType.Repaint)
            return;

        if (engine != null)
        {
            EmotionSpectrum spectrum = engine.GetSpectrum(timeline.CurrentIndicatorNormalized);
            currentSpectrum = EmotionSpectrum.Lerp(currentSpectrum, spectrum, .3f);

            rect.GetWorldCorners(shapeCorners);
            DrawEmotionShape(new Rect(shapeCorners[0], shapeCorners[2] - shapeCorners[0]), Color.magenta, currentSpectrum);
        }
    }

    protected void DrawEmotionShape(Rect container, Color color, EmotionSpectrum spectrum)
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

        int subdivisions = EmotionSpectrum.SAMPLE_COUNT;

        float width = Mathf.Abs(outerRadius - innerRadius);

        for (int i = 0; i < subdivisions + 1; i++)
        {
            float t = i / (float)subdivisions;
            float angle = t * Mathf.PI * 2f;
            float r = spectrum[angle] * width + innerRadius;

            GL.Color(EmotionVector.GetColorForAngle(angle));

            Vector3 point = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * r;
            GL.Vertex(Vector3.Scale(point, scale) + offset);
        }

        GL.End();
        GL.PopMatrix();
    }

}
