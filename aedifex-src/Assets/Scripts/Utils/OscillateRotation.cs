using UnityEngine;
using System.Collections;

public class OscillateRotation : MonoBehaviour 
{
    public Vector3 axis = Vector3.up;
    public float amplitudeEuler = 10f;

    public float rotationVelocity = 1f;
    public bool preserveOriginalRotation = false;
    public bool local = false;

    public bool realtime = false;
    public bool randomTimeOffset = false;

    private Transform trans;

    private Quaternion originalRotation;

    private float timeCounter;

    public void Start()
    {
        this.trans = this.transform;
        this.originalRotation = preserveOriginalRotation ? (local ? this.trans.localRotation : this.trans.rotation) : Quaternion.identity; // Yeah...
        this.timeCounter = randomTimeOffset ? Random.value * Mathf.PI * 2f : 0f;
    }

    public void Update () 
    {
        this.timeCounter += (realtime ? Time.unscaledDeltaTime: Time.deltaTime) * rotationVelocity;

        Vector3 nAxis = axis.normalized;

        Vector3 rotationAxis = new Vector3(nAxis.x * Mathf.Sin(timeCounter), nAxis.y * Mathf.Cos(timeCounter), nAxis.z * Mathf.Sin(timeCounter));
        Quaternion rot = originalRotation * Quaternion.Euler(rotationAxis * amplitudeEuler);

        if(local)
            this.trans.localRotation = rot;
        else 
            this.trans.rotation = rot;
    }
}