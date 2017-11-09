using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DollyCameraStrategy : ProceduralCameraStrategy
{
    protected Vector3 movementDirection;
    private Vector3 mainInterestAxis;

    protected float speed;
    protected bool keepAttention;

    protected override void OnStart()
    {
        EmotionSpectrum currentEmotion = ProceduralEngine.Instance.GetCurrentEmotion();
        float expectation = currentEmotion.Dot(new EmotionSpectrum(EmotionVector.GetCoreEmotion(CoreEmotion.Anticipation)));
        
        camera.RotationDampingTime = .1f + Mathf.Lerp(.4f, 0f, Mathf.Clamp01(expectation - 2f));
        camera.PositionDampingTime = .1f + Mathf.Lerp(.4f, 0f, Mathf.Clamp01(expectation - 2f));
        camera.SetNoiseParameters(Mathf.Clamp(expectation * .4f, 0f, .25f), .75f);

        Vector3 boundsAxis = mainInterestPoint.AssociatedItemBounds.size.normalized;

        List<KeyValuePair<Vector3, float>> possibleDirections = new List<KeyValuePair<Vector3, float>>();

        // Chance of following the frustum average
        possibleDirections.Add(new KeyValuePair<Vector3, float>(mainInterestAxis, .5f));

        // Chance of picking a dolly direction based on the item boundaries
        possibleDirections.Add(new KeyValuePair<Vector3, float>(mainInterestPoint.transform.right * boundsAxis.x, boundsAxis.x));
        possibleDirections.Add(new KeyValuePair<Vector3, float>(mainInterestPoint.transform.up * boundsAxis.y, boundsAxis.y));
        possibleDirections.Add(new KeyValuePair<Vector3, float>(mainInterestPoint.transform.forward * boundsAxis.z, boundsAxis.z));

        // Chance of doing a dolly in/out
        float inOutDirection = ProceduralEngine.Instance.EmotionEngine.GetStructureAtTime(ProceduralEngine.Instance.CurrentTimeNormalized) == StructureType.Decreasing ? -1f : 1f;
        possibleDirections.Add(new KeyValuePair<Vector3, float>(GetForward() * inOutDirection, .5f));

        movementDirection = ProceduralEngine.SelectRandomWeighted(possibleDirections, x => x.Value).Key.normalized;
        keepAttention = ProceduralEngine.RandomRange(0f, 1f) > .5f; // TODO: associate this with the rotation smoothness/lag, and with emotions (e.g. sadness lags, expectation keeps)
    }

    protected override bool FindCameraPosition()
    {
        float smoothEnergy = ProceduralEngine.Instance.EmotionEngine.GetSmoothEnergy(ProceduralEngine.Instance.CurrentTimeNormalized) / ProceduralEngine.Instance.EmotionEngine.MaxEnergy;
        float distMultiplier = 1f + smoothEnergy;
        return FindCameraPosition(15.5f * distMultiplier, 75f * distMultiplier);
    }

    public override float Evaluate(EmotionEvent e, List<InterestPoint> frustumPoints, float frustumImportanceAccumulation)
    {
        // TODO: Ideally, we would do PCA 
        // to find the main axis. For now,
        // we just use the direction towards the average, in viewspace.
        Vector3 vsAvg = Vector3.zero;
        Matrix4x4 viewMatrix = GetViewMatrix();

        foreach(InterestPoint p in frustumPoints)
            vsAvg += viewMatrix.MultiplyPoint(p.transform.position) * p.importance; // Guide it to most interesting things ;)

        // View space average, normalized, is essentially the direction
        // from the camera center to its ws avg
        vsAvg.z = 0f;
        vsAvg = vsAvg.normalized;

        Matrix4x4 invViewMatrix = GetCameraToWorldMatrix();
        mainInterestAxis = invViewMatrix.MultiplyVector(vsAvg);

        return base.Evaluate(e, frustumPoints, frustumImportanceAccumulation);
    }

    public override bool Propose(EmotionEvent e, InterestPoint p, float shotDuration)
    {
        speed = ProceduralEngine.Instance.EmotionEngine.GetSmoothEnergy(e.timestamp) * 4f / shotDuration;
        return base.Propose(e, p, shotDuration);
    }

    protected override void OnUpdateStrategy()
    {
        EmotionSpectrum currentEmotion = ProceduralEngine.Instance.GetCurrentEmotion();
        
        CameraPosition = Vector3.Lerp(initialPosition, initialPosition + movementDirection * speed * shotDuration, CameraTimeNormalized);

        if (keepAttention)
            CameraRotation = GetViewDirectionForInterestPoint(mainInterestPoint, Composition);
    }
}