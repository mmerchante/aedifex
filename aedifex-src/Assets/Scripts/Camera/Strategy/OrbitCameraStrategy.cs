using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitCameraStrategy : ProceduralCameraStrategy
{
    private float initialAngle;
    private float orbitAmplitude;

    private float initialRadius;
    private float endRadiusPercentage;
    private float direction;
    
    protected override void OnStart()
    {
        this.initialRadius = (initialPosition - mainInterestPoint.transform.position).magnitude;

        Vector3 toTarget = (initialPosition - mainInterestPoint.transform.position).normalized;
        initialAngle = Mathf.Atan2(toTarget.z, toTarget.x) + Mathf.PI;
        orbitAmplitude = Mathf.PI / 8f;

        endRadiusPercentage = ProceduralEngine.RandomRange(.8f, 1.2f); // A small zoom in/out

        this.direction = ProceduralEngine.RandomRange(0f, 1f) > .5f ? 1f : -1f;
    }

    public override bool Propose(EmotionEvent e, InterestPoint p, float shotDuration)
    {
        return base.Propose(e, p, shotDuration);
    }

    public override float Evaluate(EmotionEvent e, List<InterestPoint> frustumPoints, float frustumImportanceAccumulation)
    {
        return base.Evaluate(e, frustumPoints, frustumImportanceAccumulation);
    }
    
    protected override void OnUpdateStrategy()
    {
        float angle = initialAngle + orbitAmplitude * CameraTimeNormalized * direction;

        float r = Mathf.Lerp(initialRadius, initialRadius * endRadiusPercentage, CameraTimeNormalized);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * r;

        CameraPosition = mainInterestPoint.transform.position + offset;
        CameraRotation = GetViewDirectionForInterestPoint(mainInterestPoint, Composition);
    }
}
