using Pixvana.Video;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pixvana.Extras
{
    public class PlayerControls : MonoBehaviour
    {

        private const double skipTime = 10.0f;          // time to skip (in seconds)

        // Time format if hours are included (0 = hours, 1 = minutes, 2 = seconds)
        private const string hoursTimeFormat = "{0:D2}:{1:D2}:{2:D2}";

        // Time format if hours are NOT included (0 = hours, 1 = minutes, 2 = seconds)
        private const string minutesTimeFormat = "{1:D2}:{2:D2}";

        [SerializeField] private PlayerBase m_Player = null;
        public PlayerBase player { get { return m_Player; } set { m_Player = value; } }

        [SerializeField] private AutoFader m_AutoFader = null;
        public AutoFader autoFader { get { return m_AutoFader; } set { m_AutoFader = value; } }

        [Space]

        [SerializeField]
        private Image m_PlayImage = null;
        public Image playImage { get { return m_PlayImage; } set { m_PlayImage = value; } }

        [SerializeField] private Texture2D m_PlayTexture = null;
        public Texture2D playTexture { get { return m_PlayTexture; } set { m_PlayTexture = value; } }

        [SerializeField] private Texture2D m_PauseTexture = null;
        public Texture2D pauseTexture { get { return m_PauseTexture; } set { m_PauseTexture = value; } }

        [Space]

        [SerializeField]
        private GameObject m_BackgroundPanel = null;
        public GameObject backgroundPanel { get { return m_BackgroundPanel; } set { m_BackgroundPanel = value; } }

        [SerializeField] private GameObject m_FillPanel = null;
        public GameObject fillPanel { get { return m_FillPanel; } set { m_FillPanel = value; } }

        [Space]

        [SerializeField]
        private Text m_TimeText = null;
        public Text timeText { get { return m_TimeText; } set { m_TimeText = value; } }

        void Reset()
        {
            if (m_AutoFader == null)
            {

                m_AutoFader = GetComponent<AutoFader>();
            }
        }

        void Start()
        {
            if (m_Player != null)
            {
                // Subscribe to player events
                m_Player.onReadyStateChanged += OnReadyStateChanged;
                m_Player.onCurrentTimeChanged += OnCurrentTimeChanged;
                m_Player.onPlay += OnPlayPause;
                m_Player.onPause += OnPlayPause;
                m_Player.onReset += OnReset;

                m_PlayImage.sprite = Sprite.Create(m_PlayTexture, new Rect(0.0f, 0.0f, 64.0f, 64.0f), new Vector2(0.5f, 0.5f));

                UpdatePlayPause();
            }
            else
            {
                Debug.LogWarning("No player to control");
            }

            // Subscribe to control events
            ControllerManager.instance.onCommand += OnCommand;
        }

        private void OnCommand(object sender, ControllerManager.CommandEventArgs e)
        {
            switch (e.command) {

            case ControllerManager.Command.Right:
                {
                    m_Player.Seek (m_Player.currentTime + skipTime, true);
                    ResetFade ();
                    break;
                }
            case ControllerManager.Command.Left:
                {
                    m_Player.Seek (m_Player.currentTime - skipTime, true);
                    ResetFade ();
                    break;
                }
            case ControllerManager.Command.Select:
                {
                    TogglePlay ();
                    break;
                }
            case ControllerManager.Command.Menu:
                {
                    Application.Quit ();
                    break;
                }
            }
        }

        private void ResetFade()
        {
            if (m_AutoFader != null)
            {

                m_AutoFader.ResetFade();
            }
        }

        private void TogglePlay()
        {
            if (m_Player.isPlaying)
            {

                m_Player.Pause();

            }
            else
            {

                m_Player.Play();
            }
        }

        private void UpdatePlayPause()
        {
            // Update the play/pause image
            m_PlayImage.sprite = Sprite.Create(
                (m_Player.isPlaying ? m_PauseTexture : m_PlayTexture),
                new Rect(0.0f, 0.0f, 64.0f, 64.0f),
                new Vector2(0.5f, 0.5f));
        }

        void OnPlayPause(object sender, System.EventArgs e)
        {
            UpdatePlayPause();
            ResetFade();
        }

        private void OnReadyStateChanged(object sender, PlayerBase.ReadyStateChangedEventArgs e)
        {
            UpdatePlayPause();
            UpdateTimeline();
            ResetFade();
        }

        void OnCurrentTimeChanged(object sender, PlayerBase.CurrentTimeChangedEventArgs e)
        {
            UpdateTimeline();
        }

        private void UpdateTimeline()
        {
            if (m_Player.duration != PlayerBase.UNKNOWN_TIME)
            {

                float progress = (float)((m_Player.currentTime != PlayerBase.UNKNOWN_TIME ? m_Player.currentTime : 0.0) / m_Player.duration);

                RectTransform rectTransform = m_BackgroundPanel.GetComponent<RectTransform>();
                RectTransform fillRectTransform = m_FillPanel.GetComponent<RectTransform>();

                fillRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (progress * rectTransform.rect.size.x));

                UpdateTimeText();
            }
        }

        private void OnReset(object sender, EventArgs e)
        {
            UpdatePlayPause();
            UpdateTimeline();
            ResetFade();
        }

        private void UpdateTimeText()
        {

            if (m_Player.duration != PlayerBase.UNKNOWN_TIME)
            {

                TimeSpan currentTime = TimeSpan.FromSeconds(Math.Floor((m_Player.currentTime != PlayerBase.UNKNOWN_TIME ? m_Player.currentTime : 0.0)));
                TimeSpan totalTime = TimeSpan.FromSeconds(Math.Ceiling(m_Player.duration));

                // Choose an appropriate time format (should we internationalize?)
                string timeFormat = ((currentTime.Hours > 0 || totalTime.Hours > 0) ? hoursTimeFormat : minutesTimeFormat);

                m_TimeText.text = string.Format(timeFormat,
                    currentTime.Hours,
                    currentTime.Minutes,
                    currentTime.Seconds)
                + " / " +
                string.Format(timeFormat,
                    totalTime.Hours,
                    totalTime.Minutes,
                    totalTime.Seconds);
            }
        }
    }
}
