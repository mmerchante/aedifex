using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Base class for emotionally aware components. 
/// They can specify a specific emotion and track they are related to,
/// and do whatever they want with it -- they have no state associated to it.
/// </summary>
public class EmotionBehavior : MonoBehaviour
{
    // TODO: In the future, change this by an actual spectrum and add an inspector editor
    public CoreEmotion emotionAffinity = CoreEmotion.Joy;
    public int TrackId = -1; // For now, we hardcode it, in the future the inspector should load it :)

    public bool smooth = false;

    public float GlobalEmotionIncidence { get; protected set; } // The dot product of the affinity with the current global emotion
    public float TrackEmotionIncidence { get; protected set; } // The dot product of the affinity with the associated track, if any

    private EmotionSpectrum internalSpectrum = null;
    protected List<InterestPoint> interestPoints = new List<InterestPoint>();

    public void Awake()
    {
        this.internalSpectrum = new EmotionSpectrum(EmotionVector.GetCoreEmotion(emotionAffinity));
        this.interestPoints = new List<InterestPoint>(GetComponentsInChildren<InterestPoint>());
        OnAwake();
    }

    protected virtual void OnAwake()
    {
    }

    public float GetGlobalAffinityInTime(float nT)
    {
        EmotionSpectrum globalEmotion = smooth ? ProceduralEngine.Instance.EmotionEngine.GetSmoothSpectrum(nT) : ProceduralEngine.Instance.EmotionEngine.GetSpectrum(nT);
        return globalEmotion.Dot(internalSpectrum);
    }

    public float GetTrackAffinityInTime(float nT)
    {
        TrackData track = ProceduralEngine.Instance.EmotionEngine.GetTrackById(TrackId);

        if (track != null)
            return internalSpectrum.Dot(ProceduralEngine.Instance.EmotionEngine.EvaluateTrack(track, nT));

        return 0f;
    }

    public void Update()
    {
        if (ProceduralEngine.Instance.Running)
        {
            GlobalEmotionIncidence = GetGlobalAffinityInTime(ProceduralEngine.Instance.CurrentTimeNormalized);
            TrackEmotionIncidence = GetTrackAffinityInTime(ProceduralEngine.Instance.CurrentTimeNormalized);

            OnUpdate();
        }
    }

    public List<InterestPoint> GetAllInterestPoints()
    {
        return interestPoints;
    }

    protected virtual void OnUpdate()
    {
    }
}
