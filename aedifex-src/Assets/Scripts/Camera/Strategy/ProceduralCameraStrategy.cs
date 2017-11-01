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
    }

    /// <summary>
    /// Returns a score based on specific logic. Can/should use emotion affinities to choose
    /// </summary>
    public float Evaluate(EmotionEvent e)
    {
        InterestPoints = FindInterestPoints();
        CameraPosition = FindCameraPosition();
        Composition = ProposeComposition();

        if (InterestPoints.Count == 0)
            return 0f;

        return 1f;
    }

    protected virtual CompositionSettings ProposeComposition()
    {
        CompositionSettings c = new CompositionSettings();
        c.screenTarget = Vector2.one * .5f; // The center
        c.deadZoneSize = 0f;
        return c;
    }

    protected virtual List<InterestPoint> FindInterestPoints()
    {
        List<InterestPoint> points = new List<InterestPoint>();
        InterestPoint p = GameObject.FindObjectOfType<InterestPoint>();

        if (p)
            points.Add(p);

        return points;
    }

    protected virtual Vector3 FindCameraPosition()
    {
        return Random.insideUnitSphere * 10f;
    }
}
