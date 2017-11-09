using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PostProcessing;

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

    private PostProcessingProfile postProfile;
    private float currentTime;

    public void Awake()
    {
        PositionDampingTime = 0f;
        RotationDampingTime = 0f;
        SetNoiseParameters(0f, 1f);
    }

    public void InitializeCamera(ProceduralCameraStrategy strategy, PostProcessingProfile postProfile)
    {
        this.postProfile = postProfile;
        this.strategy = strategy;

        UpdateTransform();
    }

    public void SetNoiseParameters(float amplitude, float noiseFrequency)
    {
        this.NoiseAmplitude = amplitude;
        this.NoiseFrequency = noiseFrequency;
        this.lowFreqRandom = new LowFrequencyRandom(noiseFrequency);
    }

    public void Update()
    {
        if (strategy != null)
        {
            strategy.UpdateStrategy();
            UpdateTransform();

            DepthOfFieldModel dof = postProfile.depthOfField;
            DepthOfFieldModel.Settings s = dof.settings;
            s.focusDistance = EvaluateTargetFocalDistance();
            dof.settings = s;
            postProfile.depthOfField = dof;
        }
    }

    protected void UpdateTransform(bool force = false)
    {
        lowFreqRandom.Update(Time.deltaTime);

        smoothPosition.Target = EvaluateTargetPosition();
        smoothRotation.Target = EvaluateTargetRotation();

        smoothPositionNoise.Target = lowFreqRandom.GetInsideSphere() * NoiseAmplitude;
        smoothRotationNoise.Target = Quaternion.Slerp(smoothRotationNoise.Value, lowFreqRandom.GetRotation(), NoiseAmplitude * .25f);

        if (force)
        {
            smoothPosition.Value = EvaluateTargetPosition();
            smoothRotation.Value = EvaluateTargetRotation();

            smoothPositionNoise.Value = lowFreqRandom.GetInsideSphere() * NoiseAmplitude;
            smoothRotationNoise.Value = Quaternion.Slerp(smoothRotationNoise.Value, lowFreqRandom.GetRotation(), NoiseAmplitude * .25f);
        }

        // Smooth values take some time to get to speed, so lets keep the beginning linear
        float warmup = Mathf.Clamp01(currentTime);

        smoothPosition.Update(PositionDampingTime * warmup, Time.deltaTime);
        smoothRotation.Update(RotationDampingTime * warmup, Time.deltaTime);

        smoothPositionNoise.Update(NoiseFrequency * 5f * warmup, Time.deltaTime);
        smoothRotationNoise.Update(NoiseFrequency * 5f * warmup, Time.deltaTime);


        // Noise goes over everything else
        this.transform.position = smoothPosition.Value + smoothPositionNoise.Value;
        this.transform.rotation = smoothRotation.Value * smoothRotationNoise.Value;

        currentTime += Time.deltaTime;
    }

    // For DoF
    public float EvaluateTargetAperture()
    {
        return 0f;
    }

    // For DoF
    public float EvaluateTargetFocalDistance()
    {
        return strategy.GetFocalDistance();
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