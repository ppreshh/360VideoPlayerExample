using Pixvana.Video;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pixvana.Extras
{
    [RequireComponent(typeof(Video.Projector))]
    /// <summary>
    /// Add to a projector to interactively modify mono and stereo projection scales using the left and right bracket keys.
    /// </summary>
    public class ProjectionScaleControls : MonoBehaviour
    {

        [SerializeField] private Video.Projector m_Projector = null;
        /// <summary>
        /// Gets or sets the projector.
        /// </summary>
        /// <value>The projector.</value>
        public Video.Projector projector { get { return m_Projector; } set { m_Projector = value; } }

        [SerializeField] private float m_StepSize = 10.0f;
        /// <summary>
        /// Gets or sets the scale step size.
        /// </summary>
        /// <value>The step size.</value>
        public float stepSize { get { return m_StepSize; } set { m_StepSize = value; } }

        void Reset()
        {
            if (m_Projector == null)
            {
                m_Projector = GetComponent<Video.Projector>();
            }
        }

        void Update()
        {
            if (m_Projector != null)
            {
                float newScale = m_Projector.monoProjectionScale;

                // Keyboard controls
                if (Input.GetKeyDown(KeyCode.LeftBracket))
                {
                    // Mininum value of 1.0
                    newScale = Math.Max(newScale - m_StepSize, 1.0f);
                }
                else if (Input.GetKeyDown(KeyCode.RightBracket))
                {
                    // No upper bound
                    newScale += m_StepSize;
                }

                if (newScale != m_Projector.monoProjectionScale)
                {
                    // Set new scale
                    m_Projector.monoProjectionScale = newScale;
                    m_Projector.stereoProjectionScale = newScale;

                    Debug.Log("projectionScale: " + newScale);
                }
            }
        }
    }
}