using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pixvana.Extras
{
    [RequireComponent(typeof(CanvasGroup))]
    public class AutoFader : MonoBehaviour {

        private const float defaultAutoFadeTime = 5.0f; // time to wait before automatically fading out (in seconds)
        private const float fadeOutTime = 0.25f;        // fade out time in seconds

        [SerializeField] private bool m_AutoFade = true;
        public bool autoFade { get { return m_AutoFade; } set { UpdateAutoFade (value); } }

        [SerializeField] private float m_AutoFadeTime = defaultAutoFadeTime;
        public float autoFadeTime { get { return m_AutoFadeTime; } set { m_AutoFadeTime = value; } }

        private CanvasGroup m_CanvasGroup = null;
        private bool m_fadingContent = false;

        void Awake ()
        {
            // Cache canvas group
            m_CanvasGroup = GetComponent<CanvasGroup> ();

            // Start faded out?
            m_CanvasGroup.alpha = (m_AutoFade ? 0.0f : 1.0f);
        }

        public void ResetFade()
        {
            if (m_AutoFade) {

                m_fadingContent = false;
                CancelInvoke ("FadeOut");

                // Show immediately
                m_CanvasGroup.alpha = 1.0f;

                Invoke ("FadeOut", autoFadeTime);
            }
        }

        private void UpdateAutoFade (bool autoFade)
        {
            m_AutoFade = autoFade;

            if (m_AutoFade) {

                // If we're not fading, schedule a fade
                if (!m_fadingContent &&
                    m_CanvasGroup.alpha > 0.0f) {

                    Invoke ("FadeOut", autoFadeTime);
                }

            } else {

                // Cancel any scheduled fades
                CancelInvoke ("FadeOut");

                // Snap to completely faded or opaque
                m_CanvasGroup.alpha = (m_fadingContent ? 0.0f : 1.0f);

                m_fadingContent = false;
            }
        }

        private void FadeOut()
        {
            if (this.isActiveAndEnabled)
            {
                StartCoroutine(FadeOut(fadeOutTime));
            }
        }

        IEnumerator FadeOut(float t) {

            m_fadingContent = true;

            while (m_fadingContent &&
                m_CanvasGroup.alpha > 0.0f)
            {
                m_CanvasGroup.alpha -= (Time.deltaTime / t);

                yield return null;
            }

            m_fadingContent = false;
        }
    }
}