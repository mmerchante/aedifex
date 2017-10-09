using UnityEngine;
using System.Collections;

/// <summary>
/// Poolable particle system that returns to pool when it finished playing.
/// </summary>
[RequireComponent (typeof(ParticleSystem))]
[DisallowMultipleComponent]
public class PoolableParticles : MonoBehaviour, PoolableObject<PoolableParticles> 
{
	public float particleLength = -1f;
	public bool activateOnFirstFrame = false;

    public bool returnOnFinish = true;

	[HideInInspector]
	public ParticleSystem ps;

	protected float timeCounter = 0f;

	private ExtendablePool<PoolableParticles> pool;

	private bool running = false;

	public float CurrentParticleTime
	{
		get { return timeCounter; }
	}
	
	public void Awake()
	{
		this.ps = gameObject.GetComponent<ParticleSystem>();

		if(ps && particleLength < 0f)
			particleLength = this.ps.main.duration;
	}

    public bool Running
    {
        get { return running; }
    }
	
	// If particles are looped, use this method!
	public void InitializeParticles(float duration)
	{
		this.particleLength = duration;
		this.timeCounter = 0f;
	}

    public void ForceStop()
    {
        ps.Stop();
        ps.Clear();

        if(pool != null)
            pool.Return(this);
    }

	public void OnRetrieve(ExtendablePool<PoolableParticles> pool)
	{
		this.pool = pool;
		this.timeCounter = 0f;
		this.running = false;

		if(!activateOnFirstFrame)
		{
			ps.Clear();
			ps.Stop();
			ps.Play();
		}
	}

	protected virtual bool CanReturn()
	{
		return timeCounter > particleLength;
	}

	public void Update()
	{
        if(CanReturn() && pool != null && returnOnFinish)
			pool.Return(this);

		timeCounter += Time.deltaTime;

        if(!running && activateOnFirstFrame)
		{
			running = true;
			ps.Stop();
			ps.Clear();
			ps.Play();
		}
	}

	public void OnReturn()
	{
		ps.Clear();
		ps.Stop();
	}
}