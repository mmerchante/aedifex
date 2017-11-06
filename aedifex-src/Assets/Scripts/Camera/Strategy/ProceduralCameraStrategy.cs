using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A recipe kind of class that tries to find interesting camera movements, and saves them for the specific shot.
/// They can be compared based on a score.
/// </summary>
public class ProceduralCameraStrategy
{
    public Vector3 CameraPosition { get; protected set; }
    public Quaternion CameraRotation { get; protected set; }
    public CompositionSettings Composition { get; protected set; }

    protected float FrustumImportance { get; set; }

    protected InterestPoint mainInterestPoint;
    
    public virtual void UpdateStrategy(ProceduralCamera camera)
    {
        // Here, derived classes can play with the interest points or the camera
        mainInterestPoint.IsSelected = true;

        CameraRotation = GetViewDirectionForInterestPoint(mainInterestPoint, Composition);
    }

    public Matrix4x4 GetViewProjection()
    {
        float aspect = Screen.width / Screen.height;
        Matrix4x4 viewMatrix = Matrix4x4.TRS(CameraPosition, CameraRotation, Vector3.one).inverse;
        Matrix4x4 projMatrix = Matrix4x4.Perspective(Composition.fieldOfView, aspect, .01f, 500f);
        return projMatrix * viewMatrix;
    }

    /// <summary>
    /// Tries to find an interesting shot, returns false if it failed
    /// and should not be considered for evaluation
    /// </summary>
    public virtual bool Propose(EmotionEvent e, InterestPoint p)
    {
        this.mainInterestPoint = p;

        if (!FindCameraPosition())
            return false;

        Composition = ProposeComposition();
        CameraRotation = GetViewDirectionForInterestPoint(p, Composition);

        return true;
    }

    /// <summary>
    /// Returns a score based on specific logic. Can/should use emotion affinities to choose
    /// </summary>
    public float Evaluate(EmotionEvent e, List<InterestPoint> frustumPoints, float frustumImportanceAccumulation)
    {
        int threshold = 32;
        float avgImportance = frustumImportanceAccumulation / frustumPoints.Count;
        int maxCount = Mathf.Min(threshold, frustumPoints.Count);
        return 1f + Mathf.Lerp(0f, frustumImportanceAccumulation * maxCount, FrustumImportance);
    }

    protected virtual CompositionSettings ProposeComposition()
    {
        CompositionSettings c = new CompositionSettings();
        c.screenTarget = new Vector2(.5f, .5f); // The center
        c.deadZoneSize = 0f;
        c.fieldOfView = 45; // TODO: Find a fov that adjusts to the interest point's size
        return c;
    }

    protected Quaternion GetViewDirectionForInterestPoint(InterestPoint p, CompositionSettings composition)
    {
        Vector3 cameraPosition = CameraPosition;
        Vector3 target = p.transform.position;

        Vector3 dir = (target - cameraPosition).normalized;
        Quaternion lookAt = Quaternion.LookRotation(dir, Vector3.up);

        //float farClip = 500f;

        //float aspect = Screen.width / Screen.height;
        //Matrix4x4 viewMatrix = Matrix4x4.TRS(cameraPosition, lookAt, Vector3.one).inverse;
        //Matrix4x4 projMatrix = Matrix4x4.Perspective(composition.fieldOfView, aspect, .01f, farClip);

        //Matrix4x4 viewProj = projMatrix * viewMatrix;

        //Vector4 ndcPos = viewProj * new Vector4(target.x, target.y, target.z, 1f);
        //ndcPos /= ndcPos.w;

        //Vector2 screenPos = new Vector2(ndcPos.x * .5f + .5f, ndcPos.y * .5f + .5f);
        //Vector2 screenDifference = composition.screenTarget - screenPos;

        //// If the object is outside our screen target, force look at it
        //if (screenDifference.magnitude > composition.deadZoneSize)
        //{
        //    // If the object is _very_ far, first try to get to look at it
        //    if (screenDifference.magnitude > 1f)
        //        return lookAt;

        //    Vector2 screenTarget = composition.screenTarget * 2f - Vector2.one;

        //    float distance = Mathf.Clamp01(Vector3.Distance(cameraPosition, target) / farClip);
        //    Matrix4x4 invViewProj = viewProj.inverse;
        //    Vector4 targetNdcPos = new Vector4(screenTarget.x, screenTarget.y, distance, 1f) * farClip;

        //    Vector4 vsPos = invViewProj * targetNdcPos;
        //    vsPos /= vsPos.w;

        //    Vector3 newTargetPosition = new Vector3(vsPos.x, vsPos.y, vsPos.z);
        //    dir = (newTargetPosition - cameraPosition).normalized;
        //    return Quaternion.LookRotation(dir, Vector3.up);
        //}

        return lookAt;
    }

    protected virtual bool FindCameraPosition()
    {
        Vector3 p = mainInterestPoint.transform.position;

        float minDistance = 5f; // TODO: temp
        float maxDistance = 30f;
        int maxTries = 32;

        for(int i = 0; i < maxTries; ++i)
        {
            Vector3 startPoint = p + Vector3.Scale(Random.onUnitSphere * mainInterestPoint.size, mainInterestPoint.transform.lossyScale);

            Ray ray = new Ray(startPoint, Random.onUnitSphere.normalized);
            RaycastHit hit;
            bool intersect = Physics.Raycast(ray, out hit, maxDistance);

            bool firstPass = false;

            if(intersect)
            {
                if(hit.distance > minDistance)
                {
                    float d = Mathf.Clamp(hit.distance * .75f, minDistance, maxDistance);
                    CameraPosition = ray.origin + ray.direction * ProceduralEngine.RandomRange(minDistance, d);
                    firstPass = true;
                }
            }
            else
            {
                CameraPosition = ray.origin + ray.direction * ProceduralEngine.RandomRange(minDistance, maxDistance);
                firstPass = true;
            }

            // Now we need to shoot from the other side... non convex meshes and all...
            if(firstPass)
            {
                ray = new Ray(CameraPosition, -ray.direction);
                intersect = Physics.Raycast(ray, out hit, maxDistance);

                if (intersect)
                {
                    if (hit.distance > (CameraPosition - startPoint).magnitude)
                        return true;
                }
                else
                {
                    // This case should be rare, but can happen.
                    return true;
                }
            }
        }
        
        return false;
    }
}
