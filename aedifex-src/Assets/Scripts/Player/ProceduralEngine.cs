﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ProceduralEngine : MonoBehaviorSingleton<ProceduralEngine>
{
    public DebugPanel debugPanel;
    public string sceneRootId;

    private AudioSource musicSource;
    private AudioClip musicTrack;
    private float[] audioSignal;

    public ProceduralEventDispatcher EventDispatcher { get; protected set; }
    public EmotionEngine EmotionEngine { get; protected set; }
    public float CurrentTime { get; protected set; }
    public float CurrentTimeNormalized { get { return CurrentTime / musicTrack.length; } }
    public float Duration { get { return musicTrack.length; } }
    public bool Running { get; protected set; }

    public System.Random RNG { get; protected set; }
    public int Seed { get; protected set; }


    protected override void Initialize()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.volume = 1f;
    }

    protected void LoadSignal(AudioClip clip)
    {
        this.musicTrack = clip;
        audioSignal = new float[musicTrack.samples];
        musicTrack.GetData(audioSignal, 0);
    }

    protected void RunSimulationInternal(DataContainer data)
    {
        Application.targetFrameRate = 24;
        this.EmotionEngine = new EmotionEngine();
        this.Seed = 14041956 + System.DateTime.Now.Millisecond;
        this.RNG = new System.Random(Seed);

        if (data != null)
        {
            this.CurrentTime = 0f;
            this.Running = true;
            this.musicSource.clip = musicTrack;
            this.musicSource.Play();

            EmotionEngine.Initialize(musicTrack.length, audioSignal, data, 1024);
            EmotionEngine.Precompute();

            this.EventDispatcher = gameObject.AddComponent<ProceduralEventDispatcher>();
            this.EventDispatcher.Initialize();

            debugPanel.ShowPanel();

            // Director must be alive before the scene is built
            ProceduralCameraDirector.Instance.InitializeDirector(EmotionEngine);

            ItemFactory.Instance.BuildItem(Quaternion.identity, Vector3.forward, Vector3.forward, sceneRootId);

            // ... and update the spatial grid after that
            ProceduralCameraDirector.Instance.InitializeGrid();
            ProceduralCameraDirector.Instance.StartFirstShot();
        }
    }

    public void RunSimulation(AudioClip audioClip, DataContainer data)
    {
        LoadSignal(audioClip);
        RunSimulationInternal(data);
    }

    public void RunSimulation(AudioClip audioClip, string path)
    {
        LoadSignal(audioClip);
        string json = System.IO.File.ReadAllText(path);
        DataContainer container = JsonUtility.FromJson<DataContainer>(json);
        RunSimulationInternal(container);
    }

    public EmotionSpectrum GetCurrentEmotion()
    {
        if (Running)
            return EmotionEngine.GetSpectrum(CurrentTimeNormalized);

        return new EmotionSpectrum();
    }

    public EmotionSpectrum GetCurrentEmotion(TrackData track)
    {
        return new EmotionSpectrum();
    }

    public void Update()
    {
        if (!Running)
            return;

        // We need the time to be synchronized!
        CurrentTime = musicSource.time;

        if (CurrentTimeNormalized >= 1f)
            Running = false;

        EventDispatcher.UpdateEvents(CurrentTimeNormalized);
        ProceduralCameraDirector.Instance.UpdateCamera(CurrentTimeNormalized);
    }

    public static float RandomRange(float min, float max)
    {
        return Mathf.Lerp(min, max, (float)Instance.RNG.NextDouble());
    }

    public static T SelectRandomWeighted<T>(List<T> list, System.Func<T, float> func)
    {
        if (list.Count == 0)
            return default(T);

        float sum = list.Sum(x => func(x));
        float value = (float)Instance.RNG.NextDouble() * sum;

        foreach (T t in list)
        {
            value -= func(t);

            if (value <= 0f)
                return t;
        }

        return list.Last();
    }

    public static T SelectRandomWeighted<T>(List<T> list, System.Func<T, float> func, float sum)
    {
        if (list.Count == 0)
            return default(T);

        float value = (float)Instance.RNG.NextDouble() * sum;

        foreach (T t in list)
        {
            value -= func(t);

            if (value <= 0f)
                return t;
        }

        return list.Last();
    }
}
