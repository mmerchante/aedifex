using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverviewCameraStrategy : DollyCameraStrategy
{
    protected override void OnStart()
    {
        base.OnStart();

        camera.RotationDampingTime = .4f;
        camera.PositionDampingTime = .4f;
        camera.SetNoiseParameters(ProceduralEngine.RandomRange(.3f, .6f), ProceduralEngine.RandomRange(.5f, 1f));
        keepAttention = false; // Just pan!

        // 2D angle
        float angle = ProceduralEngine.RandomRange(0f, 2f * Mathf.PI);
        movementDirection = (GetRight() * Mathf.Cos(angle) + GetUp() * Mathf.Sin(angle)).normalized;

        speed = ProceduralEngine.Instance.EmotionEngine.GetSmoothEnergy(ProceduralEngine.Instance.CurrentTimeNormalized) / ProceduralEngine.Instance.EmotionEngine.MaxEnergy;
        speed = Mathf.Lerp(10f, 20f, speed);
    }

    protected override bool FindCameraPosition()
    {
        float smoothEnergy = ProceduralEngine.Instance.EmotionEngine.GetSmoothEnergy(ProceduralEngine.Instance.CurrentTimeNormalized) / ProceduralEngine.Instance.EmotionEngine.MaxEnergy;
        float distMultiplier = 1f + smoothEnergy;
        return FindCameraPosition(95f * distMultiplier, 140f * distMultiplier);
    }

    protected override Vector3 GetCameraDirectionBias()
    {
        // Shoot rays up!
        Vector3 r = Random.onUnitSphere;
        r.y = 7f;
        return r * 1.5f;
    }
}