using UnityEngine;
using System.Collections;

public class LowFrequencyRandom
{
	private float lastRandom;
	private Vector3 lastInsideSphere;
	private Quaternion rot;

	private float timeCounter = 0f;
	private float frequency = 1f;

	public LowFrequencyRandom(float frequency = 1f)
	{
		this.frequency = frequency;
	}

	public void Update(float delta)
	{ 
		bool reset = false;

		if(this.timeCounter > frequency)
		{
			lastRandom = Random.value;
			lastInsideSphere = Random.insideUnitSphere;
			rot = Random.rotation;
			reset = true;
		}

		this.timeCounter += delta;

		if(reset)
			timeCounter = 0f;
	}

	public Vector3 GetInsideSphere()
	{
		return lastInsideSphere;
	}

	public Quaternion GetRotation()
	{
		return rot;
	}

	public float GetValue()
	{
		return lastRandom;
	}
}