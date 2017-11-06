﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Very simple for now, we can overdesign it later
public struct CompositionSettings
{
    public Vector2 screenTarget;
    public float deadZoneSize;
    public float fieldOfView;
}

/// <summary>
/// Base class for procedural cameras.
/// They are virtual, in the sense that they control a transform but not the actual camera.
/// The camera director chooses which one to interpolate towards.
/// </summary>
public class ProceduralCamera : MonoBehaviour
{
    public CompositionSettings CompositionSettings { get; protected set; }

    public float DutchAngle { get; set; }

    public float NoiseAmplitude { get; protected set; }
    public float NoiseFrequency { get; protected set; }

    public float PositionDampingTime { get; set; }
    public float RotationDampingTime { get; set; }

    protected SmoothVector3 smoothPosition = new SmoothVector3(Vector3.zero);
    protected SmoothQuaternion smoothRotation = new SmoothQuaternion(Quaternion.identity);

    private LowFrequencyRandom lowFreqRandom = new LowFrequencyRandom(1f);
    private SmoothVector3 smoothPositionNoise = new SmoothVector3(Vector3.zero);
    private SmoothQuaternion smoothRotationNoise = new SmoothQuaternion(Quaternion.identity);

    private ProceduralCameraStrategy strategy;

    private Vector3 startPosition = Vector3.zero;

    public void Awake()
    {
        PositionDampingTime = 0f;
        RotationDampingTime = 0f;
        SetNoiseParameters(0f, 1f);
    }

    public void InitializeCamera(ProceduralCameraStrategy strategy)
    {
        this.strategy = strategy;

        this.CompositionSettings = strategy.Composition;
        this.startPosition = strategy.CameraPosition;

        UpdateTransform();
    }

    public void SetNoiseParameters(float amplitude, float noiseFrequency)
    {
        this.NoiseAmplitude = amplitude;
        this.NoiseFrequency = noiseFrequency;
        this.lowFreqRandom = new LowFrequencyRandom(noiseFrequency);
    }

    public void LateUpdate()
    {
        if (strategy != null)
        {
            strategy.UpdateStrategy(this);
            UpdateTransform();
        }
    }

    protected void UpdateTransform()
    {
        lowFreqRandom.Update(Time.deltaTime);

        smoothPosition.Target = EvaluateTargetPosition();
        smoothRotation.Target = EvaluateTargetRotation();
        smoothPositionNoise.Target = lowFreqRandom.GetInsideSphere() * NoiseAmplitude;
        smoothRotationNoise.Target = Quaternion.Slerp(Quaternion.identity, lowFreqRandom.GetRotation(), NoiseAmplitude);

        smoothPosition.Update(PositionDampingTime, Time.deltaTime);
        smoothRotation.Update(RotationDampingTime, Time.deltaTime);
        smoothPositionNoise.Update(NoiseFrequency, Time.deltaTime);
        smoothRotationNoise.Update(NoiseFrequency, Time.deltaTime);

        // Noise goes over everything else
        this.transform.position = smoothPosition.Value + smoothPositionNoise.Value;
        this.transform.rotation = smoothRotation.Value;// * smoothRotationNoise.Value;
    }

    // For DoF
    public float EvaluateTargetAperture()
    {
        return 0f;
    }

    // For DoF
    public float EvaluateTargetFocalDistance()
    {
        return 1f;
    }

    public float EvaluateTargetFieldOfView()
    {
        return 60f;
    }

    protected Vector3 EvaluateTargetPosition()
    {
        return strategy.CameraPosition;
    }

    protected Quaternion EvaluateTargetRotation()
    {
        return strategy.CameraRotation;
    }   
}