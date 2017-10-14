using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatDetector : MonoBehaviour
{
    public float detectionCoefficient = 1.3f;
    public int instantEnergySamples = 1024;
    public int localEnergyPersistenceSamples = 44032;
    public AudioClip clip;

    // Expose signals for UI
    public float[] Samples { get; protected set; }
    public float[] BeatSamples { get; protected set; }

    public void Initialize()
    {
        if (!clip)
            Debug.LogError("No clip assigned!", this);

        LoadData();
        Analyze();
    }

    protected void LoadData()
    {
        this.Samples = new float[clip.samples];

        clip.LoadAudioData();
        clip.GetData(Samples, 0);
    }

    public void Analyze()
    {
        int instantEnergySampleCount = Samples.Length / instantEnergySamples;
        int localEnergyOffset = 0;
        float localEnergyAverage = 0f;
        float localEnergySectorSum = 0f;

        if (Samples.Length < instantEnergySampleCount)
            Debug.LogError("Track is too short!");

        this.BeatSamples = new float[instantEnergySampleCount];

        // Initialize the average
        for (int i = 0; i < localEnergyPersistenceSamples; ++i)
        {
            if (i < instantEnergySamples)
                localEnergySectorSum += Samples[i];

            localEnergyAverage += Samples[i];
        }

        // Jump half a second so that we have reasonable energy average past/future
        int startOffset = (localEnergyPersistenceSamples / instantEnergySamples) / 2;
        int endOffset = instantEnergySampleCount - startOffset;

        //Debug.Log(startOffset + ", " + endOffset + ", " + instantEnergySampleCount);

        for (int i = startOffset; i < endOffset - 1; ++i)
        {
            float instantEnergy = 0f;

            for (int j = 0; j < instantEnergySamples; ++j)
                instantEnergy += Mathf.Pow(Samples[i * instantEnergySamples + j], 2f);

            // Now we shift the average with a new block
            localEnergyAverage -= localEnergySectorSum;
            localEnergySectorSum = 0f;

            for (int j = 0; j < instantEnergySamples; ++j)
                localEnergyAverage += Samples[j + localEnergyPersistenceSamples + localEnergyOffset];

            localEnergyOffset += instantEnergySamples;

            // Recalculate the first sector sum
            for (int j = 0; j < instantEnergySamples; ++j)
                localEnergySectorSum += Samples[j + localEnergyOffset];

            if(instantEnergy > detectionCoefficient * localEnergyAverage * instantEnergySamples / (float) localEnergyPersistenceSamples)
                BeatSamples[i] = 1f;
        }
    }
    
}