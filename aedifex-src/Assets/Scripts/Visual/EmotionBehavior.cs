using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for emotionally aware components. 
/// They can specify a specific emotion and track they are related to,
/// and do whatever they want with it -- they have no state associated to it.
/// </summary>
public class EmotionBehavior : MonoBehaviour
{
    // TODO: In the future, change this by an actual spectrum and add an inspector editor
    public CoreEmotion emotionAffinity = CoreEmotion.Joy;
    public int TrackId = -1;

    public float GlobalEmotionIncidence { get; protected set; } // The dot product of the affinity with the current global emotion
    public float TrackEmotionIncidence { get; protected set; } // The dot product of the affinity with the associated track, if any

    private EmotionSpectrum internalSpectrum = null;

    public void Update()
    {
        if (internalSpectrum == null)
            internalSpectrum = new EmotionSpectrum(EmotionVector.GetCoreEmotion(emotionAffinity));

        EmotionSpectrum globalEmotion = ProceduralEngine.Instance.GetCurrentEmotion();
        GlobalEmotionIncidence = globalEmotion.Dot(internalSpectrum);

        // TODO: per track incidence
    }

    protected virtual void OnUpdate()
    {
    }
}
