using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for procedural cameras
/// </summary>
public class ProceduralCamera : MonoBehaviour
{
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

    public Vector3 EvaluateTargetPosition()
    {
        return transform.position;
    }
    

    public Quaternion EvaluateTargetRotation()
    {
        return transform.rotation;
    }

}
