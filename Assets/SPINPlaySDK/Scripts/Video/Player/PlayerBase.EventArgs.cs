using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Pixvana.Video
{
    public abstract partial class PlayerBase
    {
        /// <summary>
        /// Ready state changed event arguments.
        /// </summary>
        public class ReadyStateChangedEventArgs : EventArgs
        {
            private ReadyState m_ReadyState = ReadyState.Idle;

            /// <summary>
            /// Initializes a new instance of the <see cref="Pixvana.Video.PlayerBase+ReadyStateChangedEventArgs"/> class.
            /// </summary>
            /// <param name="readyState">The ready state.</param>
            public ReadyStateChangedEventArgs(ReadyState readyState) {

                m_ReadyState = readyState;
            }

            /// <summary>
            /// Gets the <see cref="ReadyState"/>.
            /// </summary>
            /// <value>The ready state.</value>
            public ReadyState readyState { get { return m_ReadyState; } }
        }

        /// <summary>
        /// Current time changed event arguments.
        /// </summary>
        public class CurrentTimeChangedEventArgs : EventArgs
        {
            private double m_CurrentTime = UNKNOWN_TIME;

            /// <summary>
            /// Initializes a new instance of the <see cref="Pixvana.Video.PlayerBase+CurrentTimeChangedEventArgs"/> class.
            /// </summary>
            /// <param name="currentTime">The current time (in seconds).</param>
            public CurrentTimeChangedEventArgs(double currentTime) {

                m_CurrentTime = currentTime;
            }

            /// <summary>
            /// Gets the current time.
            /// </summary>
            /// <value>The current time (in seconds).</value>
            public double currentTime { get { return m_CurrentTime; } }
        }

        /// <summary>
        /// Tile changed event arguments.
        /// </summary>
        public class TileChangedEventArgs : EventArgs
        {
            private string m_TileID = null;

            /// <summary>
            /// Initializes a new instance of the <see cref="Pixvana.Video.PlayerBase+TileChangedEventArgs"/> class.
            /// </summary>
            /// <param name="tileID">The tile ID.</param>
            public TileChangedEventArgs(string tileID) {

                Assert.IsNotNull (tileID, "tileID cannot be null");

                m_TileID = tileID;
            }

            /// <summary>
            /// Gets the tile ID.
            /// </summary>
            /// <value>The tile ID.</value>
            public string tileID { get { return m_TileID; } }
        }

        /// <summary>
        /// Quality group changed event arguments.
        /// </summary>
        public class QualityGroupChangedEventArgs : EventArgs
        {
            private QualityGroup m_QualityGroup = null;

            /// <summary>
            /// Initializes a new instance of the <see cref="Pixvana.Video.PlayerBase+QualityGroupChangedEventArgs"/> class.
            /// </summary>
            /// <param name="qualityGroup">The quality group.</param>
            public QualityGroupChangedEventArgs(QualityGroup qualityGroup) {

                Assert.IsNotNull (qualityGroup, "qualityGroup cannot be null");

                m_QualityGroup = qualityGroup;
            }

            /// <summary>
            /// Gets the quality group.
            /// </summary>
            /// <value>The quality group.</value>
            public QualityGroup qualityGroup { get { return m_QualityGroup; } }
        }

        /// <summary>
        /// Audio track changed event arguments.
        /// </summary>
        public class AudioTrackChangedEventArgs : EventArgs
        {
            private AudioTrack m_AudioTrack = null;

            /// <summary>
            /// Initializes a new instance of the <see cref="Pixvana.Video.PlayerBase+AudioTrackChangedEventArgs"/> class.
            /// </summary>
            /// <param name="audioTrack">The audio track.</param>
            public AudioTrackChangedEventArgs(AudioTrack audioTrack) {

                Assert.IsNotNull (audioTrack, "audioTrack cannot be null");

                m_AudioTrack = audioTrack;
            }

            /// <summary>
            /// Gets the quality group.
            /// </summary>
            /// <value>The quality group.</value>
            public AudioTrack audioTrack { get { return m_AudioTrack; } }
        }

        /// <summary>
        /// Seeked event arguments.
        /// </summary>
        public class SeekedEventArgs : EventArgs
        {
            private double m_SeekTime = 0.0;

            /// <summary>
            /// Initializes a new instance of the <see cref="Pixvana.Video.PlayerBase+SeekedEventArgs"/> class.
            /// </summary>
            /// <param name="seekTime">The seeked-to time (in seconds).</param>
            public SeekedEventArgs(double seekTime)
            {
                m_SeekTime = seekTime;
            }

            /// <summary>
            /// Gets the seeked-to time.
            /// </summary>
            /// <value>The seeked-to time (in seconds).</value>
            public double seekTime { get { return m_SeekTime; } }
        }

        /// <summary>
        /// Error event arguments.
        /// </summary>
        public class ErrorEventArgs : EventArgs
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

        /// <summary>
        /// IOCompleted event arguments.
        /// </summary>
        public class IOCompletedEventArgs : EventArgs
        {
            private int m_Bytes = 0;
            private double m_Time = 0.0;

            public IOCompletedEventArgs(int bytes, double time)
            {
                m_Bytes = bytes;
                m_Time = time;
            }

            /// <summary>
            /// Gets the number of bytes read in this block of data.
            /// </summary>
            /// <value>The number of bytes read.</value>
            public int bytes { get { return m_Bytes; } }

            /// <summary>
            /// Gets the number of seconds spent reading this block of data.
            /// </summary>
            /// <value>The time spent reading this data.</value>
            public double time { get { return m_Time; } }
        }

        /// <summary>
        /// DroppedFramesArgs event arguments.
        /// </summary>
        public class DroppedFramesEventArgs : EventArgs
        {
            private int m_NumFramesDropped = 0;
 
            public DroppedFramesEventArgs(int numFramesDropped)
            {
                m_NumFramesDropped = numFramesDropped;
             }

            /// <summary>
            /// Gets the number of dropped frames represented by this event.
            /// </summary>
            /// <value>The number of bytes read.</value>
            public int count { get { return m_NumFramesDropped; } }
        }


    }
}
