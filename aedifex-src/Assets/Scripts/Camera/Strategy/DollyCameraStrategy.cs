using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DollyCameraStrategy : ProceduralCameraStrategy
{
    private Vector3 movementDirection;
    private Vector3 mainInterestAxis;

    private float speed;
    private bool keepAttention;

    protected override void OnStart()
    {
        camera.RotationDampingTime = .1f;
        camera.PositionDampingTime = .05f;

        Vector3 boundsAxis = mainInterestPoint.AssociatedItemBounds.size.normalized;

        List<KeyValuePair<Vector3, float>> possibleDirections = new List<KeyValuePair<Vector3, float>>();

        // Chance of following the frustum average
        possibleDirections.Add(new KeyValuePair<Vector3, float>(mainInterestAxis, .5f));

        // Chance of picking a dolly direction based on the item boundaries
        possibleDirections.Add(new KeyValuePair<Vector3, float>(mainInterestPoint.transform.right * boundsAxis.x, boundsAxis.x));
        possibleDirections.Add(new KeyValuePair<Vector3, float>(mainInterestPoint.transform.up * boundsAxis.y, boundsAxis.y));
        possibleDirections.Add(new KeyValuePair<Vector3, float>(mainInterestPoint.transform.forward * boundsAxis.z, boundsAxis.z));

        // Chance of doing a dolly in/out
        float inOutDirection = ProceduralEngine.Instance.EmotionEngine.GetCurrentStructure(ProceduralEngine.Instance.CurrentTimeNormalized) == StructureType.Decreasing ? -1f : 1f;
        possibleDirections.Add(new KeyValuePair<Vector3, float>(GetForward() * inOutDirection, .5f));

        movementDirection = ProceduralEngine.SelectRandomWeighted(possibleDirections, x => x.Value).Key.normalized;
        keepAttention = ProceduralEngine.RandomRange(0f, 1f) > .5f; // TODO: associate this with the rotation smoothness/lag, and with emotions (e.g. sadness lags, expectation keeps)
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
        float expectation = currentEmotion.Dot(new EmotionSpectrum(EmotionVector.GetCoreEmotion(CoreEmotion.Anticipation)));
        
        camera.SetNoiseParameters(Mathf.Clamp(expectation * .5f, 0f, .2f), .7f);
        CameraPosition = initialPosition + movementDirection * speed * CameraTimeNormalized;

        if (keepAttention)
            CameraRotation = GetViewDirectionForInterestPoint(mainInterestPoint, Composition);
    }
}