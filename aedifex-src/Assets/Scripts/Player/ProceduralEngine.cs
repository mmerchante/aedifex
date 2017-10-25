using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralEngine : MonoBehaviour
{
    public AudioClip musicTrack;

    private float[] audioSignal;
    private EmotionEngine emotionEngine;

    public void Start()
    {
        string[] files = SFB.StandaloneFileBrowser.OpenFilePanel("Open File", Application.dataPath, "json", false);

        if (files.Length == 1)
            LoadFile(files[0]);
    }

    public void LoadFile(string path)
    {
        string json = System.IO.File.ReadAllText(path);
        DataContainer container = JsonUtility.FromJson<DataContainer>(json);

        emotionEngine = new EmotionEngine();

        audioSignal = new float[musicTrack.samples];
        musicTrack.GetData(audioSignal, 0);

        if (container != null)
        {
            emotionEngine.Initialize(audioSignal, container, 1024);

            emotionEngine.Precompute();
        }
    }
}
