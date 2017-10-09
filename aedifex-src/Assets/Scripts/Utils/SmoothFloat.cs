using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Simple container to simplify smooths
public class SmoothFloat
{
	private float value;
	private float velocity;
	private float target;

	private float min;
	private float max;
	private float maxVelocity;

	private bool overshoot;
	private bool clamp = true;

	public float OvershootPercent = .05f;

    public SmoothFloat (float value) : this(value, float.MinValue, float.MaxValue, false)
    {
        
    }

	public SmoothFloat (float value, float min, float max, bool overshoot = true)
	{
		this.value = value;
		this.target = value;
		this.velocity = 0f;
		this.min = min;
		this.max = max;
		this.overshoot = overshoot;

		ClampValue();
	}

	public float Update(float smoothTime, float delta)
	{
		if (delta == 0)
		{
			Debug.LogError("Delta can't be 0 in SmoothDamp");
			delta = Time.unscaledDeltaTime;
		}
		this.value = Mathf.SmoothDamp(this.value, target, ref velocity, smoothTime, float.PositiveInfinity, delta);
		ClampValue();
		return this.value;
	}

	public void ClearVelocity()
	{
		this.velocity = 0f;
	}

	private void ClampValue()
	{
		if(clamp)
		{
			if(overshoot)
			{
				float overMax = max + max * OvershootPercent;
				float overMin = min - min * OvershootPercent;

				float overMaxTarget = max + max * OvershootPercent * 5f;
				float overMinTarget = min - min * OvershootPercent * 5f;

				this.target = Mathf.Clamp(this.target, overMinTarget, overMaxTarget);

				// Overshooting!
				if((value > overMax || value < overMin ))
					this.target = Mathf.Clamp(this.target, this.min, this.max);
			}
			else
			{
				this.value = Mathf.Clamp(this.value, this.min, this.max);
				this.target = Mathf.Clamp(this.target, this.min, this.max);
			}
		}
	}

	public bool Clamp { get { return clamp; } set { this.clamp = value; ClampValue(); } }
	public float Min { get { return min; } set { this.min = value; ClampValue(); } }
	public float Max { get { return max; } set { this.max = value; ClampValue(); } }
	public float Target { get { return target; } set { this.target = value; ClampValue(); } }
	public float Value { get { return value; } set { this.value = value; velocity = 0f; ClampValue(); } }
	public float Velocity { get { return velocity; } set { this.velocity = value; } }
}