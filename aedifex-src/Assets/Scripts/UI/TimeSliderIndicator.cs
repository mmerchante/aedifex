using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeSliderIndicator : MonoBehaviour, PoolableObject<TimeSliderIndicator>
{
    public float IndicatorTime { get; protected set; }

    private RectTransform rectTrans;
    private Text text;

    public void Awake()
    {
        this.text = GetComponent<Text>();
        this.rectTrans = GetComponent<RectTransform>();
    }

    public void UpdateData(float time, float offset)
    {
        this.IndicatorTime = time;
        this.text.text = GetTimecode(time);
        this.rectTrans.anchoredPosition = new Vector2(offset, 0f);
    }

    protected string GetTimecode(float t)
    {
        float s = Mathf.Repeat(t, 60f);
        int m = (int)(t / 60f);
        return m.ToString() + ":" + s.ToString("0.0");
    }

    public void OnRetrieve(ExtendablePool<TimeSliderIndicator> pool)
    {
    }

    public void OnReturn()
    {
    }
}