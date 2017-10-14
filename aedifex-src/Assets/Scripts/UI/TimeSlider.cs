using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeSlider : MonoBehaviour
{
    public float secondsPerTick = 1f;
    public int ticksPerAccent = 5;
    
    public float Offset { get; set; }
    public float Zoom { get; set; }
    public float BaseDuration { get; protected set; }

    public RectTransform indicatorContainer;
    public TimeSliderIndicator timeIndicatorPrefab;

    private Material lineMaterial;
    private RectTransform rect;

    private Vector3[] corners = new Vector3[4];
    private ExtendablePool<TimeSliderIndicator> indicatorPool;

    private List<TimeSliderIndicator> currentIndicators = new List<TimeSliderIndicator>();

    protected void Awake()
    {
        this.lineMaterial = new Material(Shader.Find("Unlit/Color"));
        this.lineMaterial.color = Color.gray;
        this.rect = GetComponent<RectTransform>();

        indicatorPool = new ExtendablePool<TimeSliderIndicator>(timeIndicatorPrefab, indicatorContainer);
        indicatorPool.SetInitialSize(30);
    }

    public void Initialize(float baseDuration)
    {
        this.BaseDuration = baseDuration;
    }

    public void OnGUI()
    {
        if (Event.current.type != EventType.Repaint)
            return;

        ticksPerAccent = Mathf.Max(3, ticksPerAccent);
        rect.GetWorldCorners(corners);
        DrawSlider(new Rect(corners[0].x, corners[0].y + 2f, rect.rect.width, rect.rect.height - 2f));
    }

    public void Update()
    {
        rect.GetWorldCorners(corners);
        DrawTimeIndicators(new Rect(corners[0].x, corners[0].y + 2f, rect.rect.width, rect.rect.height - 2f));
    }

    protected void DrawTimeIndicators(Rect container)
    {
        float z = Mathf.Max(1f, Zoom);
        int zoomScaling = (int)((int)(z / secondsPerTick) * secondsPerTick) + 1;

        int ticks = (int)((BaseDuration) / secondsPerTick) * zoomScaling;
        float offset = Offset * container.width * Zoom;

        int accents = ticks / ticksPerAccent;

        while (currentIndicators.Count < accents)
            currentIndicators.Add(indicatorPool.Retrieve());

        if (currentIndicators.Count >= accents)
        {
            List<TimeSliderIndicator> toReturn = new List<TimeSliderIndicator>();
            int toRemove = currentIndicators.Count - accents;

            for (int i = 0; i < toRemove; ++i)
            {
                toReturn.Add(currentIndicators[currentIndicators.Count - 1]);
                indicatorPool.Return(currentIndicators[currentIndicators.Count - 1]);
                currentIndicators.RemoveAt(currentIndicators.Count - 1);
            }
        }

        for (int i = 0; i < currentIndicators.Count; ++i)
        {
            TimeSliderIndicator indicator = currentIndicators[i];
            float t = Mathf.Clamp01(i / (float)accents);
            float x = t * container.width * Zoom - offset;
            indicator.UpdateData(BaseDuration * t, x);
        }
    }

    protected void DrawSlider(Rect container)
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

        float z = Mathf.Max(1f, Zoom);
        int zoomScaling = (int)((int)(z / secondsPerTick) * secondsPerTick) + 1;

        int ticks = (int)((BaseDuration) / secondsPerTick) * zoomScaling;
        float offset = Offset * container.width * Zoom;
        
        for(int i = 0; i < ticks; ++i)
        {
            float t = (i / (float)ticks);
            float x = container.x + t * container.width * Zoom - offset;

            if (x < container.x || x > container.width + container.x)
                continue;

            bool accent = i % ticksPerAccent == 0;
            GL.Vertex(new Vector3(x, container.y, 0f));
            GL.Vertex(new Vector3(x, container.y + container.height * (accent ? 1f : .25f), 0f));
        }

        GL.End();
        GL.PopMatrix();
    }
}
