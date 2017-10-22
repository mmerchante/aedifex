using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleAudioVisualizer : MonoBehaviour
{
    public Color lineColor = Color.yellow;
    public Rect uiRect;

    [Range (0f, 1f)]
    public float Offset = 0f;
    public float Zoom = 1f;

    private int downsampleRate = 1024;
    private float[] downsampledData = null;
    private float[] rawData = null;

    private Material lineMaterial;

    protected void Awake()
    {
        this.lineMaterial = new Material(Shader.Find("Unlit/Color"));
    }

    public void Initialize(float[] rawData, int downsampleRate)
    {
        this.downsampleRate = downsampleRate;
        this.rawData = rawData;

        Downsample();
    }

    private void Downsample()
    {
        int count = this.rawData.Length / downsampleRate;

        this.downsampledData = new float[count];
        
        float maxValue = 0f;

        for (int i = 0; i < rawData.Length; i += downsampleRate)
        {
            if (i < rawData.Length && (i / downsampleRate) < count)
            {
                downsampledData[i / downsampleRate] = this.rawData[i];
                maxValue = Mathf.Max(maxValue, this.rawData[i]);
            }
        }

        if (maxValue == 0f)
            maxValue = 1f;

        for (int i = 0; i < downsampledData.Length; i++)
            downsampledData[i] /= maxValue;
    }

    public void OnGUI()
    {
        if (Event.current.type != EventType.Repaint)
            return;

        Zoom = Mathf.Max(Zoom, 0.0001f);
        lineMaterial.color = lineColor;

        if (downsampledData != null)
            DrawGraph(lineMaterial, uiRect, downsampledData, Offset, Zoom);
    }

    public static void DrawGraph(Material lineMaterial, Rect container, float[] keyframes, float offset, float zoom)
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

        int keyframeOffset = (int) (offset * keyframes.Length);
        float lineOffset = offset * container.width * zoom;
        
        for (int i = keyframeOffset + 1; i < keyframes.Length; i++)
        {
            float x = (i / (float)keyframes.Length) * container.width * zoom + container.x - lineOffset;

            // Stop if we draw outside bounds (TODO: find the index...)
            if (x > container.width + container.x)
                break;

            GL.Vertex(new Vector3(x, keyframes[i] * .5f * container.height + container.y, 0f));
        }

        GL.End();
        GL.PopMatrix();
    }
}
