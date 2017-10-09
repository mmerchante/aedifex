using UnityEngine;
using System.Collections;

public class OscillatePosition : MonoBehaviour 
{
	public Vector3 axis = Vector3.up;

	public float amplitude = 1f;
	public float frequency = 1f;

    public bool guiTime = false;
    public bool randomTimeOffset = false;
    public Vector3 offset = Vector3.zero;

	private Transform trans;
	private Vector3 originalPosition;

	private float timeCounter = 0f;

	void Start () 
	{
		this.trans = this.transform;
		this.originalPosition = this.trans.localPosition;
		this.axis.Normalize();
        this.timeCounter = randomTimeOffset ? Random.value * Mathf.PI * 2f : 0f;
        Update();
	}

	void Update () 
	{
        this.timeCounter += guiTime ? Time.unscaledDeltaTime : Time.deltaTime;
        this.trans.localPosition = originalPosition + axis * amplitude * Mathf.Cos(timeCounter * frequency) + offset;
	}
}
