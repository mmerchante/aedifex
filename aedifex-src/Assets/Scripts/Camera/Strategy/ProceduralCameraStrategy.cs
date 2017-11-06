using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A recipe kind of class that tries to find interesting camera movements, and saves them for the specific shot.
/// They can be compared based on a score.
/// </summary>
public class ProceduralCameraStrategy
{
    public List<InterestPoint> InterestPoints { get; protected set; }
    public Vector3 CameraPosition { get; protected set; }
    public CompositionSettings Composition { get; protected set; }

    public virtual void UpdateStrategy(ProceduralCamera camera)
    {
        // Here, derived classes can play with the interest points or the camera
        foreach (InterestPoint p in InterestPoints)
            p.IsSelected = true;
    }

    /// <summary>
    /// Returns a score based on specific logic. Can/should use emotion affinities to choose
    /// </summary>
    public float Evaluate(EmotionEvent e)
    {
        InterestPoints = FindInterestPoints();
        
        if (InterestPoints.Count == 0 || !FindCameraPosition())
            return 0f;

        Composition = ProposeComposition();
        return 1f;
    }

    protected virtual CompositionSettings ProposeComposition()
    {
        CompositionSettings c = new CompositionSettings();
        c.screenTarget = new Vector2(.5f, .5f); // The center
        c.deadZoneSize = 0f;
        return c;
    }

    protected virtual List<InterestPoint> FindInterestPoints()
    {
        List<InterestPoint> points = new List<InterestPoint>();

        int tries = 10;

        for(int i = 0; i < tries;++i)
        {
            InterestPoint p = ProceduralCameraDirector.Instance.GetRandomInterestPoint();

            // Make sure our interest point is not buried under the floor
            // TODO: should we do just V3.up?
            if (p)
            {
                Vector3 lowestPossiblePoint = p.transform.position - Vector3.up * p.size * p.transform.lossyScale.y;

                Vector3 skyPoint = lowestPossiblePoint;
                skyPoint.y = 200f;

                Ray ray = new Ray(skyPoint, -Vector3.up);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 1000f, 1 << LayerMask.NameToLayer("Floor")))
                {
                    if(Vector3.Distance(skyPoint, lowestPossiblePoint) < hit.distance)
                    {
                        points.Add(p);
                        return points;
                    }
                }
                else
                {
                    points.Add(p);
                    return points;
                }

            }
        }

        return points;
    }

    protected virtual bool FindCameraPosition()
    {
        Vector3 p = InterestPoints[0].transform.position;

        float minDistance = 10f; // TODO: temp
        float maxDistance = 100f;
        int tries = 50;

        for(int i = 0; i < tries; ++i)
        {
            Vector3 startPoint = p + Vector3.Scale(Random.onUnitSphere * InterestPoints[0].size, InterestPoints[0].transform.lossyScale);

            Ray ray = new Ray(startPoint, Random.onUnitSphere);
            RaycastHit hit;
            bool intersect = Physics.Raycast(ray, out hit, maxDistance);

            bool firstPass = false;

            if(intersect)
            {
                if(hit.distance > minDistance && hit.distance < maxDistance)
                {
                    CameraPosition = ray.origin + ray.direction * ProceduralEngine.RandomRange(minDistance, hit.distance);
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
                    if ((hit.point - startPoint).magnitude > (CameraPosition - startPoint).magnitude)
                        return true;
                }
                else
                {
                    // This case should be rare, but can happen.
                    return true;
                }
            }
        }

        Debug.Log("Failed to find a camera position!");

        return false;
    }
}
