using UnityEngine;
using System.Collections;

public class DiscreteRotation : MonoBehaviour 
{
    public int discreteRotations = 12;
    public float cyclesPerSecond = 2;

    private Transform trans;
    private Quaternion startingRotation;

    private float time;

    public void Awake()
    {
        trans = transform;
        startingRotation = trans.localRotation;
    }

    public void Update()
    {
        time += Time.unscaledDeltaTime;

        time = Mathf.Repeat(time, 1f);

        float t = time * cyclesPerSecond * discreteRotations;
        int frame = (int) t;

        float rotation = frame * 2f * Mathf.PI / discreteRotations;

        trans.localRotation = startingRotation * Quaternion.AngleAxis(rotation * Mathf.Rad2Deg, Vector3.forward);
    }
}
