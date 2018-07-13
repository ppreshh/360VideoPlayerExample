using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlaylistManager : MonoBehaviour
{

    public GameObject thumbnailPrefab;

    public List<Sprite> thumbnails;
    public List<string> videoUrls;

    private Dictionary<Sprite, string> playlist;

    private void Awake()
    {
        playlist = new Dictionary<Sprite, string>();

        for (int i = 0; i < thumbnails.Count; i++)
        {
            playlist.Add(thumbnails[i], videoUrls[i]);
        }
    }

    private void Start()
    {
        foreach (KeyValuePair<Sprite, string> playlistItem in playlist)
        {
            GameObject newThumbnail = Instantiate(thumbnailPrefab, transform);
            newThumbnail.GetComponent<Image>().sprite = playlistItem.Key;
            newThumbnail.GetComponent<ThumbnailButton>().videoURL = playlistItem.Value;
        }
    }
}