using Pixvana.Video;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pixvana.Extras
{
    [RequireComponent(typeof(Camera))]
    /// <summary>
    /// Add to a camera to enable/disable the camera based on Player events.
    /// </summary>
    public class DisableCameraDuringPlayback : MonoBehaviour
    {

        [SerializeField] private PlayerBase m_Player = null;
        /// <summary>
        /// Gets or sets the player.
        /// </summary>
        /// <value>The player.</value>
        public PlayerBase player { get { return m_Player; } set { UpdatePlayer(value); } }

        [Space]

        [SerializeField] private bool m_DisableWhenReady = true;
        /// <summary>
        /// Gets or sets a value indicating whether to disable the camera when <see cref="Pixvana.Video.PlayerBase.ReadyState"/> is <see cref="Pixvana.Video.PlayerBase.ReadyState.Ready"/>.
        /// </summary>
        /// <value><c>true</c> to disable; otherwise, <c>false</c>.</value>
        public bool disableWhenReady { get { return m_DisableWhenReady; } set { m_DisableWhenReady = value; } }

        [SerializeField] private bool m_EnableWhenEnded = true;
        /// <summary>
        /// Gets or sets a value indicating whether to enable the camera when <see cref="Pixvana.Video.PlayerBase.ReadyState"/> is <see cref="Pixvana.Video.PlayerBase.ReadyState.Ended"/>.
        /// </summary>
        /// <value><c>true</c> to enable; otherwise, <c>false</c>.</value>
        public bool enableWhenEnded { get { return m_EnableWhenEnded; } set { m_EnableWhenEnded = value; } }

        [SerializeField] private bool m_EnableWhenError = true;
        /// <summary>
        /// Gets or sets a value indicating whether to enable the camera when <see cref="Pixvana.Video.PlayerBase.ReadyState"/> is <see cref="Pixvana.Video.PlayerBase.ReadyState.Error"/>.
        /// </summary>
        /// <value><c>true</c> to enable; otherwise, <c>false</c>.</value>
        public bool enableWhenError { get { return m_EnableWhenError; } set { m_EnableWhenError = value; } }

        [Space]

        [SerializeField]
        private bool m_DisableOnPlay = false;
        /// <summary>
        /// Gets or sets a value indicating whether to disable the camera when <see cref="Pixvana.Video.PlayerBase.onPlay"/> is raised.
        /// </summary>
        /// <value><c>true</c> to disable; otherwise, <c>false</c>.</value>
        public bool disableOnPlay { get { return m_DisableOnPlay; } set { m_DisableOnPlay = value; } }

        [SerializeField] private bool m_EnableOnPause = false;
        /// <summary>
        /// Gets or sets a value indicating whether to enable the camera when <see cref="Pixvana.Video.PlayerBase.onPause"/> is raised.
        /// </summary>
        /// <value><c>true</c> to enable; otherwise, <c>false</c>.</value>
        public bool enableOnPause { get { return m_EnableOnPause; } set { m_EnableOnPause = value; } }

        void Awake()
        {

            UpdatePlayer(m_Player);
        }

        private void UpdatePlayer(PlayerBase player)
        {

            if (m_Player != null)
            {

                m_Player.onReadyStateChanged -= OnReadyStateChanged;
                m_Player.onPlay -= OnPlay;
                m_Player.onPause -= OnPause;
            }

            m_Player = player;

            if (m_Player != null)
            {

                m_Player.onReadyStateChanged += OnReadyStateChanged;
                m_Player.onPlay += OnPlay;
                m_Player.onPause += OnPause;
            }
        }

        private void OnPlay(object sender, System.EventArgs e)
        {
            UpdateCameraState(m_DisableOnPlay, false);
        }

        private void OnPause(object sender, System.EventArgs e)
        {
            UpdateCameraState(m_EnableOnPause, true);
        }

        private void OnReadyStateChanged(object sender, PlayerBase.ReadyStateChangedEventArgs e)
        {

            switch (e.readyState)
            {

                case PlayerBase.ReadyState.Ready:
                    {

                        UpdateCameraState(m_DisableWhenReady, false);
                        break;
                    }
                case PlayerBase.ReadyState.Ended:
                    {

                        UpdateCameraState(m_EnableWhenEnded, true);
                        break;
                    }
                case PlayerBase.ReadyState.Error:
                    {

                        UpdateCameraState(m_EnableWhenError, true);
                        break;
                    }
            }
        }

        private void UpdateCameraState(bool condition, bool isActive)
        {
            if (condition)
            {
                gameObject.SetActive(isActive);
            }
        }
    }
}