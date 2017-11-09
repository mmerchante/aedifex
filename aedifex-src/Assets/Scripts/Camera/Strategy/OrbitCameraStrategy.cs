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
        orbitAmplitude = Mathf.PI / 8f;
        endRadiusPercentage = ProceduralEngine.RandomRange(.8f, 1.2f); // A small zoom in/out

        direction = ProceduralEngine.RandomRange(0f, 1f) > .5f ? 1f : -1f;

        camera.RotationDampingTime = .5f;
        camera.PositionDampingTime = .4f;
        camera.SetNoiseParameters(ProceduralEngine.RandomRange(0f, .2f), .75f);

        OnUpdateStrategy();
    }

    protected Vector3 CalculatePosition()
    {
        float angle = initialAngle + orbitAmplitude * CameraTimeNormalized * direction;

        float r = Mathf.Lerp(initialRadius, initialRadius * endRadiusPercentage, CameraTimeNormalized);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * r;

        return mainInterestPoint.transform.position + offset;
    }

    protected override bool FindCameraPosition()
    {
        bool ret = base.FindCameraPosition();

        if(ret)
        {
            this.initialRadius = (CameraPosition - mainInterestPoint.transform.position).magnitude;
            Vector3 toTarget = (initialPosition - mainInterestPoint.transform.position).normalized;
            initialAngle = Mathf.Atan2(toTarget.z, toTarget.x) + Mathf.PI * .5f;

            CameraPosition = CalculatePosition();
        }

        return ret;
    }
    
    protected override void OnUpdateStrategy()
    {
        CameraPosition = CalculatePosition();
        CameraRotation = GetViewDirectionForInterestPoint(mainInterestPoint, Composition);
    }

    protected override Vector3 GetCameraDirectionBias()
    {
        // Orbit a bit higher
        Vector3 r = Random.onUnitSphere;
        r.y = (r.y + .5f) * 2f;
        return r;
    }
}
