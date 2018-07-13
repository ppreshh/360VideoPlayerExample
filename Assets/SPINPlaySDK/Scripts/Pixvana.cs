using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Namespace that contains all Pixvana-related functionality.
/// </summary>
namespace Pixvana
{
    /// <summary>
    /// Error event arguments.
    /// </summary>
    public class ErrorEventArgs : System.EventArgs
    {
        private string m_Message = "";

        /// <summary>
        /// Initializes a new instance of the <see cref="Pixvana.Video.PlayerBase+ErrorEventArgs"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ErrorEventArgs(string message)
        {
            m_Message = message;
        }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <value>The error message.</value>
        public string message { get { return m_Message; } }
    }
}
