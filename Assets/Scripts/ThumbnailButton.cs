using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThumbnailButton : Button
{

    public string videoURL;
    private Pixvana.Video.Projector projector;

    protected override void Awake()
    {
        projector = GameObject.Find("Projector").GetComponent<Pixvana.Video.Projector>();
    }

    protected override void Start()
    {
        onClick.AddListener(PlayVideo);
    }

    private void PlayVideo()
    {
        projector.sourceUrl = videoURL;
        projector.Prepare();
    }
}