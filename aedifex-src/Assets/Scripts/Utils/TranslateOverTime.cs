using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TranslateOverTime : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float minSpeed = 1f;
    public float maxSpeed = 2f;

    private float speed;

    private void Awake()
    {
        speed = Random.Range(minSpeed, maxSpeed);
    }

    public void Update()
    {
        this.transform.position += axis * speed * Time.deltaTime;
    }
}
