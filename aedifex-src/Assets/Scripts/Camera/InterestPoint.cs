﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dummy transform with some useful information
/// </summary>
public class InterestPoint : MonoBehaviour
{
    public int importance = 1; // Larger -> better
    public float size = 1f;

    public CoreEmotion primaryAffinity = CoreEmotion.Joy;

    [Range (0f, 1f)]
    public float directionality = 0f; // How directional this object in its forward axis

    public float emotionalImpact = 0f; // How much the primary affinity affects the interest

    public bool IsSelected { get; set; }

    public Bounds AssociatedItemBounds { get; protected set; }
    public Transform AssociatedItemRoot { get; protected set; }

    private static bool DirectorNotAvailable = false;
    private EmotionSpectrum internalSpectrum;
    
    public void Awake()
    {
        if (DirectorNotAvailable)
            return;
        
        internalSpectrum = new EmotionSpectrum(EmotionVector.GetCoreEmotion(primaryAffinity));

        if (ProceduralCameraDirector.IsAvailable())
            ProceduralCameraDirector.Instance.RegisterInterestPoint(this);
        else
            DirectorNotAvailable = true;
    }

    public void AssociateItemBounds(Transform itemRoot, Bounds b)
    {
        this.AssociatedItemBounds = b;
        this.AssociatedItemRoot = itemRoot;
    }

    public void OnDestroy()
    {
        //if(ProceduralCameraDirector.IsAvailable())
        //    ProceduralCameraDirector.Instance.DeregisterInterestPoint(this);
    }

    private void OnDrawGizmos()
    {
        if (IsSelected || !Application.isPlaying)
        {
            Vector3 p = transform.position;
            Vector3 f = transform.forward * .5f;
            Vector3 u = transform.up * .5f;
            Vector3 r = transform.right * .5f;

            Gizmos.color = Color.Lerp(Color.yellow, EmotionVector.GetColorForAngle(EmotionVector.GetAngleForCoreEmotion(primaryAffinity)), emotionalImpact);
            Gizmos.DrawLine(p - f, p + f * (1f + directionality));
            Gizmos.DrawLine(p - u, p + u);
            Gizmos.DrawLine(p - r, p + r);

            Gizmos.color = IsSelected ? Color.magenta : Color.yellow;
            Gizmos.DrawWireSphere(p, size * transform.lossyScale.x);

            if (AssociatedItemRoot)
            {
                Gizmos.color = Color.red;
                Gizmos.matrix = AssociatedItemRoot.localToWorldMatrix;
                Gizmos.DrawWireCube(AssociatedItemBounds.center, AssociatedItemBounds.size);
            }
        }
    }

    public Bounds GetBounds()
    {
        return new Bounds(transform.position, transform.lossyScale * size * 2f);
    }

    /// <summary>
    /// This heuristic is useful for two cases:
    /// - Trying to find the main interest point
    /// - Trying to evaluate different views for the same interest point (shot heuristic)
    /// Because it is really hard to explore the space of possibilities, this heuristic will let us
    /// sample points in solution space and make stochastic decisions.
    /// Note: this heuristic must be deterministic!
    /// </summary>
    public float EvaluateHeuristic(EmotionSpectrum currentEmotion, bool primaryInterest = false)
    {
        // If this GO is inactive just ignore this IP
        // This is useful for state machines
        if (!gameObject.activeInHierarchy)
            return 0f;

        float heuristic = importance;

        // If this is the primary interest point, we want to make sure it is 
        // close to other interesting places, so biasing the importance with a heatmap is useful.
        if (primaryInterest)
        {
            // Interesting: while being on an important place is good, we must not make unimportant elements important.
            // Thus we need to be careful with this multiplier
            heuristic += .35f * ProceduralCameraDirector.Instance.GetGrid().GetAverageImportanceForPosition(transform.position);
            
            heuristic += emotionalImpact * currentEmotion.Dot(internalSpectrum);
        }

        // TODO: ideas:
        // - Is it being lit right now? Or in shadow?
        //      - If it is reflective/specular, where would be a good place to look at it from?
        // - Is it moving?
        return heuristic;
    }
}