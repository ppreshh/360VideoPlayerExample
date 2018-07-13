using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Pixvana.Video
{
    public partial class Projector : MonoBehaviour
    {

        /// <summary>
        /// Force monoscopic changed event arguments.
        /// </summary>
        public class ForceMonoscopicChangedEventArgs : EventArgs
        {
            private bool m_ForceMonoscopic = false;

            /// <summary>
            /// Initializes a new instance of the <see cref="Pixvana.Video.Projector+ForceMonoscopicChangedEventArgs"/> class.
            /// </summary>
            /// <param name="forceMonoscopic">The force monoscopic mode.</param>
            public ForceMonoscopicChangedEventArgs(bool forceMonoscopic) {

                m_ForceMonoscopic = forceMonoscopic;
            }

            /// <summary>
            /// Gets the monoscopic mode.
            /// </summary>
            /// <value><c>true</c> if monoscopic; otherwise, <c>false</c>.</value>
            public bool forceMonoscopic { get { return m_ForceMonoscopic; } }
        }
    }
}