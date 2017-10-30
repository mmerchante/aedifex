﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralEngine : MonoBehaviorSingleton<ProceduralEngine>
{
    public DebugPanel debugPanel;

    private AudioSource musicSource;
    private AudioClip musicTrack;
    private float[] audioSignal;

    public ProceduralEventDispatcher EventDispatcher { get; protected set; }
    public EmotionEngine EmotionEngine { get; protected set; }
    public float CurrentTime { get; protected set; }
    public float CurrentTimeNormalized { get { return CurrentTime / musicTrack.length; } }
    public float Duration { get { return musicTrack.length; } }
    public bool Running { get; protected set; }

    private Queue<EmotionEvent> eventQueue = new Queue<EmotionEvent>();

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
        this.EmotionEngine = new EmotionEngine();

        if (data != null)
        {
            this.CurrentTime = 0f;
            this.Running = true;
            this.musicSource.clip = musicTrack;
            this.musicSource.Play();

            EmotionEngine.Initialize(musicTrack.length, audioSignal, data, 1024);
            EmotionEngine.Precompute();
            eventQueue = EmotionEngine.BuildEventQueue();

            this.EventDispatcher = gameObject.AddComponent<ProceduralEventDispatcher>();
            this.EventDispatcher.Initialize();

            debugPanel.ShowPanel();
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

    private float beatCounter = 0f;

    public void Update()
    {
        if (!Running)
            return;

        // We need the time to be synchronized!
        CurrentTime = musicSource.time;
        EventDispatcher.UpdateEvents(CurrentTimeNormalized);
    }
}
