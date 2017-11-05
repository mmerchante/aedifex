using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Very simple for now, we can overdesign it later
public struct CompositionSettings
{
    public Vector2 screenTarget;
    public float deadZoneSize;
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

    private List<InterestPoint> interestPoints = new List<InterestPoint>();
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
        this.interestPoints = strategy.InterestPoints;

        UpdateTransform();
    }

    public void SetNoiseParameters(float amplitude, float noiseFrequency)
    {
        this.NoiseAmplitude = amplitude;
        this.NoiseFrequency = noiseFrequency;
        this.lowFreqRandom = new LowFrequencyRandom(noiseFrequency);
    }

    public void AddInterestPoint(InterestPoint p)
    {
        this.interestPoints.Add(p);
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
        return startPosition;
    }

    protected Quaternion EvaluateTargetRotation()
    {
        if (interestPoints.Count > 0)
            return GetViewDirectionForInterestPoint(interestPoints[0]);

        return Quaternion.identity;
    }

    protected Quaternion GetViewDirectionForInterestPoint(InterestPoint p)
    {
        Vector3 pos = transform.position;
        Vector3 target = p.transform.position;

        Vector3 dir = (target - pos).normalized;
        Quaternion lookAt = Quaternion.LookRotation(-dir, Vector3.up);

        float farClip = 500f;

        float aspect = Screen.width / Screen.height;
        Matrix4x4 viewMatrix = Matrix4x4.TRS(pos, lookAt, Vector3.one).inverse;
        Matrix4x4 projMatrix = Matrix4x4.Perspective(EvaluateTargetFieldOfView(), aspect, .01f, farClip);

        Matrix4x4 viewProj = projMatrix * viewMatrix;

        Vector4 ndcPos = viewProj * new Vector4(target.x, target.y, target.z, 1f);
        ndcPos /= ndcPos.w;

        Vector2 screenPos = new Vector2(ndcPos.x * .5f + .5f, ndcPos.y * .5f + .5f);
        Vector2 screenDifference = CompositionSettings.screenTarget - screenPos;

        // If the object is outside our screen target, force look at it
        if (screenDifference.magnitude > CompositionSettings.deadZoneSize)
        {
            // If the object is _very_ far, first try to get to look at it
            if (screenDifference.magnitude > 1f)
                return lookAt;

            Vector2 screenTarget = CompositionSettings.screenTarget * 2f - Vector2.one;

            float distance = Mathf.Clamp01(Vector3.Distance(pos, target) / farClip);
            Matrix4x4 invViewProj = viewProj.inverse;
            Vector4 targetNdcPos = new Vector4(screenTarget.x, screenTarget.y, distance, 1f) * farClip;

            Vector4 vsPos = invViewProj * targetNdcPos;
            vsPos /= vsPos.w;

            Vector3 newTargetPosition = new Vector3(vsPos.x, vsPos.y, vsPos.z);
            dir = (newTargetPosition - pos).normalized;
            return Quaternion.LookRotation(dir, Vector3.up);
        }

        return transform.rotation;
    }
}