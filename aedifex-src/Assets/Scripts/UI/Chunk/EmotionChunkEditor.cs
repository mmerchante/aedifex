using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EmotionChunkEditor : TrackChunkEditor<EmotionData>, IPointerClickHandler
{
    public Button clearButton;
    public Button closeButton;

    public Toggle variantToggle;
    public InputField harmonySequenceInputField;
    public InputField intensityInputField;
    public Dropdown curveDropdown;
    public RectTransform curveContainer;

    public Image trackColorImage;
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
        this.closeButton.onClick.AddListener(Hide);
        this.clearButton.onClick.AddListener(ClearAllHandles);

        InitializeDropdown();
    }

    protected void ClearAllHandles()
    {
        foreach (EmotionVectorHandle h in handles)
            GameObject.Destroy(h.gameObject);

        handles.Clear();
    }

    private void InitializeDropdown()
    {
        List <Dropdown.OptionData> options = new List<Dropdown.OptionData>();

        foreach (string n in System.Enum.GetNames(typeof(IntensityCurve)))
            options.Add(new Dropdown.OptionData(n));

        curveDropdown.options = options;
    }

    protected override void OnInitialize()
    {
        variantToggle.isOn = Chunk.IsVariation;
        harmonySequenceInputField.text = Chunk.HarmonySequenceNumber.ToString();
        curveDropdown.value = (int) Chunk.IntensityCurveType;
        intensityInputField.text = Chunk.Data.intensityMultiplier.ToString();

        ClearAllHandles();

        foreach(EmotionVector v in Chunk.Data.vectors)
        {
            EmotionVectorHandle handle = InstantiateHandle();
            handle.SetPositionFromEmotion(v);
        }
    }

    public override void Update()
    {
        base.Update();

        Chunk.IsVariation = variantToggle.isOn;
        Chunk.IntensityCurveType = (IntensityCurve) curveDropdown.value;

        float intensity = -1f;
        float.TryParse(intensityInputField.text, out intensity);

        if(intensity >= 0f)
            Chunk.Data.intensityMultiplier = intensity;

        int harmony = -1;
        int.TryParse(harmonySequenceInputField.text, out harmony);
        if (harmony != -1)
            Chunk.HarmonySequenceNumber = harmony;        

        trackColorImage.color = Track.TrackColor;
        Chunk.Data.Clear();

        foreach (EmotionVectorHandle h in handles)
            Chunk.Data.AddVector(h.EvaluateVector());
    }

    public void OnGUI()
    {
        emotionShapeRect.GetWorldCorners(shapeCorners);
        DrawEmotionShape(new Rect(shapeCorners[0], shapeCorners[2] - shapeCorners[0]));

        curveContainer.GetWorldCorners(shapeCorners);
        DrawIntensityCurve(new Rect(shapeCorners[0], shapeCorners[2] - shapeCorners[0]));
    }

    protected void DrawIntensityCurve(Rect container)
    {
        AnimationCurve curve = TrackChunkData.GetAnimationCurve(Chunk.IntensityCurveType);

        GL.PushMatrix();
        lineMaterial.SetPass(0);
        GL.LoadOrtho();
        GL.Begin(GL.LINE_STRIP);

        container.y += 2f;
        container.height -= 2f;

        // Transform container
        container.x /= Screen.width;
        container.width /= Screen.width;
        container.y /= Screen.height;
        container.height /= Screen.height;

        int subdivisions = 32;

        for (int i = 0; i < subdivisions; ++i)
        {
            float t = (i / (float)subdivisions);
            float y = container.y + curve.Evaluate(t) * container.height;
            float x = container.x + t * container.width;

            GL.Vertex(new Vector3(x, y, 0f));
        }

        GL.End();
        GL.PopMatrix();
    }

    protected void DrawEmotionShape(Rect container)
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
            float r = Chunk.Data.Evaluate(angle, true) * width + innerRadius;

            GL.Color(EmotionVector.GetColorForAngle(angle));
            
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
