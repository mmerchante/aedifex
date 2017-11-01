using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrackHeader : MonoBehaviour
{
    public Button selectTrackButton;
    public Button removeTrackButton;
    public InputField input;
    public Image bgImage;

    public AbstractTrack Track { get; protected set; }

    private TrackEditor editor;
    private RectTransform rect;

    public void Awake()
    {
        this.rect = GetComponent<RectTransform>();
        this.selectTrackButton.onClick.AddListener(OnSelected);
    }

    private void OnSelected()
    {
        editor.SelectTrack(Track);
    }

    public void Initialize(TrackEditor editor, AbstractTrack track)
    {
        this.editor = editor;
        this.Track = track;
        this.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Track.GetTrackHeight());
        this.input.text = track.TrackName;
        Update();
    }

    public void Update()
    {
        this.bgImage.color = Track.TrackColor * .75f;
        Track.TrackName = input.text;
    }
}
