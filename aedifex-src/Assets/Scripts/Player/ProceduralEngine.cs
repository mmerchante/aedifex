using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralEngine : MonoBehaviorSingleton<ProceduralEngine>
{
    public DebugPanel debugPanel;

    private AudioSource musicSource;
    private AudioClip musicTrack;
    private float[] audioSignal;
    private EmotionEngine emotionEngine;

    public float CurrentTime { get; protected set; }

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
        this.emotionEngine = new EmotionEngine();

        if (data != null)
        {
            this.CurrentTime = 0f;
            this.Running = true;
            this.musicSource.clip = musicTrack;
            this.musicSource.Play();

            emotionEngine.Initialize(musicTrack.length, audioSignal, data, 1024);
            emotionEngine.Precompute();
            eventQueue = emotionEngine.BuildEventQueue();

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

    public List<EmotionEvent> AccumulateCloseEvents()
    {
        float threshold = emotionEngine.BeatDurationNormalized * .25f;
        List <EmotionEvent> list = new List<EmotionEvent>();

        float timeNormalized = Mathf.Clamp01(CurrentTime / musicTrack.length);

        while (eventQueue.Count > 0 && eventQueue.Peek().timestamp <= timeNormalized + threshold )
            list.Add(eventQueue.Dequeue());

        return list;
    }

    private float beatCounter = 0f;

    public void Update()
    {
        if (!Running)
            return;

        // We need the time to be synchronized!
        CurrentTime = musicSource.time;

        // Minimum time resolution for events? TODO: gather and dispatch
        if (beatCounter >= emotionEngine.BeatDuration * .25f)
        {
            beatCounter = 0f;
            List<EmotionEvent> eventGroup = AccumulateCloseEvents();

            if (eventGroup.Count > 0)
            {
                string debugText = "Total events: " + eventGroup.Count + "\n";
                foreach (EmotionEvent e in eventGroup)
                    debugText += e.ToString() + "-" + e.timestamp + "\n";

                debugPanel.DebugText(debugText);

                Debug.Log(debugText);
            }
        }
        else
        {
            beatCounter += Time.deltaTime;
        }
    }
}
