using UnityEngine;
using System.Collections;

public class RotateOverAxis : MonoBehaviour 
{
	public Vector3 axis = Vector3.up;
	public float velocity = 10f;
	public bool preserveOriginalRotation = false;
	public bool local = false;
    public bool realtime = false;
    public bool random = false;

	private Transform trans;
	private Quaternion originalRotation;
    private float time;

	public void Start()
	{
        if (random)
            axis = Random.onUnitSphere;

        this.time = 0f;
		this.trans = this.transform;
		this.originalRotation = preserveOriginalRotation ? (local ? this.trans.localRotation : this.trans.rotation) : Quaternion.identity; // Yeah...
	}

	public void Update () 
	{
		this.time += realtime ? Time.unscaledDeltaTime : Time.deltaTime;

		Quaternion rot = originalRotation * Quaternion.Euler(axis * time * velocity);
	
		if(local)
			this.trans.localRotation = rot;
		else 
			this.trans.rotation = rot;
	}
}
