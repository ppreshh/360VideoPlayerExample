using Pixvana.Video;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pixvana.Extras
{
    public class QualityControls : MonoBehaviour
    {

        private const float defaultAutoFadeTime = 5.0f; // time to wait before automatically fading out (in seconds)
        private const float fadeOutTime = 0.25f;        // fade out time in seconds

        [SerializeField] private PlayerBase m_Player = null;
        public PlayerBase player { get { return m_Player; } set { m_Player = value; } }

        [SerializeField] private AutoFader m_AutoFader = null;
        public AutoFader autoFader { get { return m_AutoFader; } set { m_AutoFader = value; } }

        [Space]

        [SerializeField] private Text m_QualityGroupText = null;
        public Text qualityGroupText { get { return m_QualityGroupText; } set { m_QualityGroupText = value; } }

        void Reset ()
        {
            if (m_AutoFader == null) {

                m_AutoFader = GetComponent<AutoFader> ();
            }
        }

        void Start ()
        {
            if (m_Player != null)
            {
                // Subscribe to player events
                m_Player.onQualityGroupChanged += OnQualityGroupChanged;

                UpdateQualityGroupText();
            }
            else
            {
                Debug.LogWarning("No player to control");
            }

            // Subscribe to control events
//            ControllerManager.instance.onCommand += OnCommand;
        }

        private void OnCommand(object sender, ControllerManager.CommandEventArgs e)
        {
            switch (e.command) {

            case ControllerManager.Command.Right:
                {
                    NextQualityGroup (1);
                    ResetFade ();
                    break;
                }
            case ControllerManager.Command.Left:
                {
                    NextQualityGroup (-1);
                    ResetFade ();
                    break;
                }
            }
        }

        void Update()
        {
            // Keyboard controls
            // NOTE: Added check for equals key for keyboards where the non-keypad "plus" isn't being detected
            if (m_Player != null && 
                m_Player.isPlaying && 
                (Input.GetKeyDown (KeyCode.Plus) || Input.GetKeyDown (KeyCode.Equals) || Input.GetKeyDown (KeyCode.KeypadPlus))) {

                NextQualityGroup (1);
                ResetFade ();

            } else if (m_Player != null &&
                m_Player.isPlaying && 
                (Input.GetKeyDown (KeyCode.Minus) || Input.GetKeyDown (KeyCode.KeypadMinus))) {

                NextQualityGroup (-1);
                ResetFade ();

            } else if (m_Player != null && m_Player.isPlaying && Input.GetKeyDown (KeyCode.A)) {

                m_Player.autoQuality = !m_Player.autoQuality;

                // No event is fired when disabling auto-quality, so manually refresh
                UpdateQualityGroupText ();

                ResetFade ();
            }
        }

        private void ResetFade ()
        {
            if (m_AutoFader != null) {

                m_AutoFader.ResetFade ();
            }
        }

        private void NextQualityGroup (int direction)
        {
            int index = m_Player.qualityGroups.IndexOf (m_Player.currentQualityGroup);
            if (index > -1) {

                index += direction;
                if (index < 0) {

                    index = m_Player.qualityGroups.Count - 1;

                } else if (index >= m_Player.qualityGroups.Count) {

                    index = 0;
                }

                m_Player.SetQualityGroup(m_Player.qualityGroups [index]);
            }
        }

        void OnQualityGroupChanged (object sender, PlayerBase.QualityGroupChangedEventArgs e)
        {
            UpdateQualityGroupText ();
            ResetFade ();
        }

        private void UpdateQualityGroupText() {

            string autoQualityState = (m_Player.autoQuality ? "ON" : "OFF");

            QualityGroup qualityGroup = m_Player.currentQualityGroup;

            string qualityGroupSummary = "";

            if (qualityGroup != null) {
                
                qualityGroupSummary = string.Format ("Group: {0}  {1} x {2}  {3:0.0} Mbps  {4:0.00} fps",
                    m_Player.currentQualityGroup.name, 
                    qualityGroup.videoWidth, 
                    qualityGroup.videoHeight, 
                    ((float)qualityGroup.bandwidth / (1000.0f * 1000.0f)),
                    ((float)qualityGroup.frameRateNumerator / (float)qualityGroup.frameRateDenominator));
            }


            m_QualityGroupText.text = string.Format ("Auto: {0}  {1}", autoQualityState, qualityGroupSummary);
        }
    }
}