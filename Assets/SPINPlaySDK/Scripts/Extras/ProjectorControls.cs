using Pixvana.Video;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Pixvana.Extras
{
    public class ProjectorControls : MonoBehaviour
    {
        private const string defaultPlaylistFileName = "playlist.json";

        [SerializeField] private Video.Projector m_Projector = null;
        public Video.Projector projector { get { return m_Projector; } set { m_Projector = value; } }

        [SerializeField] private AutoFader m_AutoFader = null;
        public AutoFader autoFader { get { return m_AutoFader; } set { m_AutoFader = value; } }

        [Space]

        [SerializeField]
        private Text m_TitleText = null;
        public Text titleText { get { return m_TitleText; } set { m_TitleText = value; } }

        [Space]

        [Tooltip("A playlist file path overrides a text asset")]
        [SerializeField]
        private string m_PlaylistFilePath = null;
        public string playlistFilePath { get { return m_PlaylistFilePath; } set { m_PlaylistFilePath = value; } }

        [Tooltip("A playlist file path overrides a text asset")]
        [SerializeField]
        private TextAsset m_PlaylistTextAsset = null;
        public TextAsset playlistTextAsset { get { return m_PlaylistTextAsset; } set { m_PlaylistTextAsset = value; } }

        private Playlist m_Playlist = null;

        void Reset()
        {
            if (m_AutoFader == null)
            {

                m_AutoFader = GetComponent<AutoFader>();
            }
        }

        void Awake()
        {

            // If we don't have a specific file path, but we do have a Text Asset, load it
            if (string.IsNullOrEmpty(m_PlaylistFilePath) &&
                m_PlaylistTextAsset != null)
            {

                m_Playlist = new Playlist(m_PlaylistTextAsset.text);
            }

            // To debug a programmatic playlist, uncomment the following lines
            //m_Playlist = new Playlist();
            //m_Playlist.items.Add(new PlaylistItem("Dan Colvin", new Uri("http://media.pixvana.com.s3.amazonaws.com/media/twM2FJs0tM9R48nj7eXJbYwcpX-CVrD6uBznicrsi00~/index.opf")));
            //m_Playlist.items.Add(new PlaylistItem("Sounders DiamondPlane", new Uri("file:///C:/Pixvana/VariSqueeze/sounders1.opf")));

            // Should we look for a command-line argument (-playlist)? Only look if we don't have
            // a specific path by now. Note that this will override an internal playlist.
#if !UNITY_WSA_10_0 && !UNITY_ANDROID
            if (string.IsNullOrEmpty(m_PlaylistFilePath))
            {
                string[] args = System.Environment.GetCommandLineArgs();
                if (args.Length > 2)
                {
                    for (int i = 1; i < args.Length - 1; i++)
                    {
                        if (args[i] == "-playlist")
                        {
                            // Remove any existing/internal playlist
                            m_Playlist = null;
                            m_PlaylistFilePath = args[i + 1];
                        }
                    }
                }
            }
#endif

            // If we still don't have a playlist or file path, look in Unity's persistentDataPath
            if (m_Playlist == null &&
                string.IsNullOrEmpty(m_PlaylistFilePath))
            {
                // Look in a default location
                m_PlaylistFilePath = Application.persistentDataPath + "/" + defaultPlaylistFileName;
                Debug.Log("Looking for: " + m_PlaylistFilePath);
            }

            // Finally, if we have a file path, try to load a playlist
            if (m_Playlist == null &&
                !string.IsNullOrEmpty(m_PlaylistFilePath))
            {
                StartCoroutine(GetPlaylist(new Uri(m_PlaylistFilePath).AbsoluteUri));
            }
        }

        private IEnumerator GetPlaylist(string uri)
        {
            Debug.Assert(!string.IsNullOrEmpty(uri), "uri is required");

            WWW www = new WWW(uri);

            yield return www;

            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.Log("Error: " + www.error + ", uri: \"" + uri + "\"");
            }
            else
            {
                m_Playlist = new Playlist(www.text, new Uri(uri));
                StartPlaylist();
            }
        }

        void Start()
        {
            if (m_Projector != null)
            {

                // Subscribe to projector events
                m_Projector.onPrepared += OnPrepared;
                m_Projector.onForceMonoscopicChanged += OnForceMonoscopicChanged;

                // Subscribe to player events
                m_Projector.player.onReset += OnReset;
                m_Projector.player.onReadyStateChanged += onReadyStateChanged;

                StartPlaylist();
            }
            else
            {
                Debug.LogWarning("No projector to control");
            }

            // Subscribe to control events
            ControllerManager.instance.onCommand += OnCommand;
        }

        private void OnCommand(object sender, ControllerManager.CommandEventArgs e)
        {
            switch (e.command) {

            case ControllerManager.Command.Up:
                {
                    m_Playlist.PreviousItem();
                    break;
                }
            case ControllerManager.Command.Down:
                {
                    m_Playlist.NextItem();
                    break;
                }
            }
        }

        private void onReadyStateChanged(object sender, PlayerBase.ReadyStateChangedEventArgs e)
        {
            if (e.readyState == PlayerBase.ReadyState.Ended)
            {
                m_Playlist.ItemEnded();
            }
        }

        private void StartPlaylist()
        {
            if (m_Playlist != null)
            {
                // Subscribe to playlist changes
                m_Playlist.onSelectedItemChanged += onSelectedItemChanged;

                m_Playlist.Start();
            }
        }

        private void onSelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            ProjectPlaylistItem();
        }

        void Update()
        {
            // Keyboard controls
            if (m_Projector != null && Input.GetKeyDown(KeyCode.M))
            {

                // Toggle monoscopic mode
                m_Projector.forceMonoscopic = !m_Projector.forceMonoscopic;
            }
        }

        private void ResetFade()
        {
            if (m_AutoFader != null)
            {

                m_AutoFader.ResetFade();
            }
        }

        private void ProjectPlaylistItem()
        {
            // Reset player before playing the next item
            m_Projector.player.ResetPlayer();
        }

        private void OnReset(object sender, EventArgs e)
        {
            // Player has been reset, so play the selected item
            m_Projector.player.autoPlay = m_Playlist.selectedItem.autoPlay;
            m_Projector.player.loop = m_Playlist.selectedItem.loop;
            m_Projector.sourceUrl = m_Playlist.selectedItem.url.AbsoluteUri;
            m_Projector.Prepare();
        }

        private void OnPrepared(object sender, EventArgs e)
        {
            UpdateTitle();
            ResetFade();
        }

        private void OnForceMonoscopicChanged(object sender, Pixvana.Video.Projector.ForceMonoscopicChangedEventArgs e)
        {
            UpdateTitle();
            ResetFade();
        }

        private void UpdateTitle()
        {
            string stereoMode = string.Format("{0}{1}",
                                    m_Projector.projection.stereoMode,
                                    (m_Projector.forceMonoscopic ? " (forced mono)" : ""));

            m_TitleText.text = string.Format("{0}\n{1} {2}",
                m_Playlist.selectedItem.title,
                m_Projector.projection.format.name,
                stereoMode);
        }
    }
}