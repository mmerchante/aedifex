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
        movementDirection = mainInterestAxis;
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
            vsAvg += viewMatrix.MultiplyPoint(p.transform.position);

        // View space average, normalized, is essentially the direction
        // from the camera center to its ws avg
        vsAvg.z = 0f;
        vsAvg = vsAvg.normalized;

        Matrix4x4 invViewMatrix = GetInvViewMatrix();
        mainInterestAxis = invViewMatrix.MultiplyVector(vsAvg);

        return base.Evaluate(e, frustumPoints, frustumImportanceAccumulation);
    }

    public override bool Propose(EmotionEvent e, InterestPoint p, float shotDuration)
    {
        speed = 0f;// ProceduralEngine.Instance.EmotionEngine.GetSpectrum(e.timestamp).GetTotalEnergy() * 50f;
        return base.Propose(e, p, shotDuration);
    }

    protected override void OnUpdateStrategy()
    {
        CameraPosition = initialPosition + movementDirection * speed * CameraTimeNormalized;

        //if (keepAttention)
            CameraRotation = GetViewDirectionForInterestPoint(mainInterestPoint, Composition);
    }
}
