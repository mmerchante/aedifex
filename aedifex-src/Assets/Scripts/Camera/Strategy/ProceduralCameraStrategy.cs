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

    protected Vector3 initialPosition;
    protected Quaternion initialRotation;

    protected float cameraTime;
    protected float shotDuration;

    protected ProceduralCamera camera;

    protected float CameraTimeNormalized { get { return cameraTime / shotDuration; } }

    public void StartStrategy(ProceduralCamera camera)
    {
        this.camera = camera;
        this.cameraTime = 0f;
        this.initialPosition = CameraPosition;
        this.initialRotation = CameraRotation;
        mainInterestPoint.IsSelected = true;

        OnStart();
    }

    protected virtual void OnStart()
    {
    }

    public void StopStrategy()
    {
        mainInterestPoint.IsSelected = false;
        OnStop();
    }

    protected virtual void OnStop()
    {
    }

    public void UpdateStrategy()
    {
        cameraTime += Time.deltaTime;
        OnUpdateStrategy();
    }

    protected virtual void OnUpdateStrategy()
    {
    }

    public float GetFocalDistance()
    {
        return Vector3.Distance(CameraPosition, mainInterestPoint.transform.position);
    }

    public Matrix4x4 GetCameraToWorldMatrix()
    {
        return Matrix4x4.TRS(CameraPosition, CameraRotation, Vector3.one);
    }

    public Vector3 GetForward()
    {
        return GetCameraToWorldMatrix().MultiplyVector(Vector3.forward);
    }

    public Vector3 GetUp()
    {
        return GetCameraToWorldMatrix().MultiplyVector(Vector3.up);
    }

    public Vector3 GetRight()
    {
        return GetCameraToWorldMatrix().MultiplyVector(Vector3.right);
    }

    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.TRS(CameraPosition, CameraRotation, Vector3.one).inverse;
    }

    public Matrix4x4 GetViewProjection()
    {
        float aspect = Screen.width / (float)Screen.height;
        Matrix4x4 viewMatrix = Matrix4x4.TRS(CameraPosition, CameraRotation, Vector3.one).inverse;
        Matrix4x4 projMatrix = Matrix4x4.Perspective(Composition.fieldOfView, aspect, .01f, 1500f);
        return projMatrix * viewMatrix;
    }

    public Matrix4x4 GetViewProjection(Vector3 position, Quaternion rotation)
    {
        float aspect = Screen.width / (float)Screen.height;
        Matrix4x4 viewMatrix = Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
        Matrix4x4 projMatrix = Matrix4x4.Perspective(Composition.fieldOfView, aspect, .01f, 1500f);
        return projMatrix * viewMatrix;
    }

    /// <summary>
    /// Tries to find an interesting shot, returns false if it failed
    /// and should not be considered for evaluation
    /// </summary>
    public virtual bool Propose(EmotionEvent e, InterestPoint p, float shotDuration)
    {
        this.shotDuration = shotDuration;
        this.mainInterestPoint = p;
        this.FrustumImportance = p.secondary ? 1f : .35f; // If the point is secondary, look towards more interesting stuff ;)

        if (!FindCameraPosition())
            return false;

        Composition = ProposeComposition();
        CameraRotation = GetViewDirectionForInterestPoint(p, Composition);

        return true;
    }

    /// <summary>
    /// Returns a score based on specific logic. Can/should use emotion affinities to choose
    /// </summary>
    public virtual float Evaluate(EmotionEvent e, List<InterestPoint> frustumPoints, float frustumImportanceAccumulation)
    {
        int threshold = 15;
        float avgImportance = frustumImportanceAccumulation / (frustumPoints.Count + 1);
        int maxCount = Mathf.Min(threshold, frustumPoints.Count);
        return 1f + Mathf.Lerp(0f, avgImportance * maxCount, FrustumImportance);
    }

    protected virtual CompositionSettings ProposeComposition()
    {
        CompositionSettings c = new CompositionSettings();

        // Random rule of thirds. TODO: based on item's biggest axis, align on X or Y
        int column = Mathf.RoundToInt(ProceduralEngine.RandomRange(1f, 3f));

        c.screenTarget = new Vector2(column * .25f + ProceduralEngine.RandomRange(-.05f, .05f), ProceduralEngine.RandomRange(.4f, .6f)); // TODO: use frustum main axis
        c.deadZoneSize = 0.01f;
        c.fieldOfView = ProceduralEngine.RandomRange(25f, 50f); // TODO: Find a fov that adjusts to the interest point's size
        return c;
    }

    public Vector3 GetViewportPosition(Vector3 cameraPosition, Quaternion cameraRotation, Vector3 p)
    {
        Vector4 ndcPos = GetViewProjection(cameraPosition, cameraRotation) * new Vector4(p.x, p.y, p.z, 1f);
        ndcPos /= ndcPos.w;
        return new Vector3(ndcPos.x * .5f + .5f, ndcPos.y * .5f + .5f, ndcPos.z);
    }

    protected Quaternion GetViewDirectionForInterestPoint(InterestPoint p, CompositionSettings composition)
    {
        Vector3 cameraPosition = CameraPosition;
        Vector3 target = p.transform.position;
        
        Vector3 dir = (target - cameraPosition).normalized;
        Quaternion lookAt = Quaternion.LookRotation(dir, Vector3.up);
        
        float farClip = 1500f;
        Vector2 screenTarget = composition.screenTarget * 2f - Vector2.one;

        float distance = Mathf.Clamp01(Vector3.Distance(cameraPosition, target) / farClip);
        Matrix4x4 invViewProj = GetViewProjection(cameraPosition, lookAt).inverse;
        Vector4 targetNdcPos = new Vector4(screenTarget.x, screenTarget.y, -distance, 1f) * farClip;

        Vector4 vsPos = invViewProj * targetNdcPos;
        vsPos /= vsPos.w;

        Vector3 newTargetPosition = new Vector3(vsPos.x, vsPos.y, vsPos.z);
        Vector3 newDir = (newTargetPosition - cameraPosition).normalized;
        lookAt = Quaternion.LookRotation(-newDir, Vector3.up);
        
        return lookAt;
    }

    protected virtual bool FindCameraPosition()
    {
        float smoothEnergy = ProceduralEngine.Instance.EmotionEngine.GetSmoothEnergy(ProceduralEngine.Instance.CurrentTimeNormalized) / ProceduralEngine.Instance.EmotionEngine.MaxEnergy;
        float distMultiplier = 1f + smoothEnergy;
        return FindCameraPosition(7.5f * distMultiplier, 60f * distMultiplier);
    }

    protected virtual Vector3 GetCameraDirectionBias()
    {
        return Vector3.zero;
    }

    protected virtual bool FindCameraPosition(float minDistance, float maxDistance)
    {
        Vector3 p = mainInterestPoint.transform.position;
        
        int maxTries = 4;

        for(int i = 0; i < maxTries; ++i)
        {
            Vector3 pointScale = mainInterestPoint.transform.lossyScale * mainInterestPoint.size;
            Vector3 startPoint = p + Vector3.Scale(Random.onUnitSphere, pointScale);

            // If the interest point is very directional, consider that for the ray direction
            Vector3 biasedDirection = (Random.onUnitSphere + mainInterestPoint.transform.forward * mainInterestPoint.directionality * 6f + GetCameraDirectionBias()).normalized;

            Ray ray = new Ray(startPoint, biasedDirection);
            RaycastHit hit;
            bool intersect = Physics.Raycast(ray, out hit, maxDistance);

            bool firstPass = false;

            if(intersect)
            {
                if(hit.distance > minDistance)
                {
                    float d = Mathf.Clamp(hit.distance * .75f, minDistance, maxDistance);
                    CameraPosition = ray.origin + ray.direction * (ProceduralEngine.RandomRange(minDistance, d) + pointScale.magnitude * 2f); // Make sure we're outside the sphere
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
