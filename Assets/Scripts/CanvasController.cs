using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasController : MonoBehaviour
{

    public GameObject ThumbnailCanvas;

    private Pixvana.Video.Projector projector;

    private void Awake()
    {
        projector = GameObject.Find("Projector").GetComponent<Pixvana.Video.Projector>();

        projector.player.onReadyStateChanged += Player_onReadyStateChanged;
        projector.player.onReset += Player_onReset;
    }

    private void Player_onReset(object sender, System.EventArgs e)
    {
        ThumbnailCanvas.SetActive(true);
    }

    private void Player_onReadyStateChanged(object sender, Pixvana.Video.PlayerBase.ReadyStateChangedEventArgs e)
    {
        switch (e.readyState)
        {
            case Pixvana.Video.PlayerBase.ReadyState.Preparing:

                ThumbnailCanvas.SetActive(false);
                break;

            case Pixvana.Video.PlayerBase.ReadyState.Ended:

                projector.player.ResetPlayer();
                break;
        }
    }
}
