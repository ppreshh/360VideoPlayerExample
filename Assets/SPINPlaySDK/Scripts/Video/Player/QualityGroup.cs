using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using SimpleJSON;

namespace Pixvana.Video
{

    /// <summary>
    /// Represents a video quality group.
    /// </summary>
    public class QualityGroup {

        #region Keys
        private const string NameKey                    = "name";
        private const string VideoWidthKey              = "videoWidth";
        private const string VideoHeightKey             = "videoHeight";
        private const string FrameRateNumeratorKey      = "frameRateNumerator";
        private const string FrameRateDenominatorKey    = "frameRateDenominator";
        private const string BandwidthKey               = "bandwidth";
        #endregion

        private string m_Name = null;
        /// <summary>
        /// Gets the name of the quality group.
        /// </summary>
        /// <value>The name.</value>
        public string name { get { return m_Name; } }

        private int m_VideoWidth = 0;
        /// <summary>
        /// Gets the video frame width of the quality group.
        /// </summary>
        /// <value>The video frame width (in pixels).</value>
        public int videoWidth { get { return m_VideoWidth; } }

        private int m_VideoHeight = 0;
        /// <summary>
        /// Gets the video frame height of the quality group.
        /// </summary>
        /// <value>The video frame height (in pixels).</value>
        public int videoHeight { get { return m_VideoHeight; } }

        private int m_FrameRateNumerator = 1;
        /// <summary>
        /// Gets the numerator of the frame rate for the quality group.
        /// </summary>
        /// <value>The frame rate numerator (in frames per second).</value>
        public int frameRateNumerator { get { return m_FrameRateNumerator; } }

        private int m_FrameRateDenominator = 30;
        /// <summary>
        /// Gets the denominator of the frame rate for the quality group.
        /// </summary>
        /// <value>The frame rate denominator (in frames per second).</value>
        public int frameRateDenominator { get { return m_FrameRateDenominator; } }

        private int m_Bandwidth = 0;
        /// <summary>
        /// Gets the bandwidth of the quality group.
        /// </summary>
        /// <value>The bandwidth (in bits per second).</value>
        public int bandwidth { get { return m_Bandwidth; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pixvana.Video.QualityGroup"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="width">The video frame width (in pixels).</param>
        /// <param name="height">The video frame height (in pixels).</param>
        /// <param name="frameRateNumerator">The frame rate numerator (in frames per second).</param>
        /// <param name="frameRateDenominator">The frame rate denominator (in frames per second).</param>
        /// <param name="bandwidth">The bandwidth (in bits per second).</param>
        public QualityGroup(string name, int width, int height, int frameRateNumerator, int frameRateDenominator, int bandwidth) {

            Assert.IsNotNull (name, "name is required");
            Assert.IsTrue (width >= 0, "width must be equal to or greater than 0");
            Assert.IsTrue (height >= 0, "height must be equal to or greater than 0");
            Assert.IsTrue (bandwidth >= 0, "bandwidth must be equal to or greater than 0");

            m_Name = name;
            m_VideoWidth = width;
            m_VideoHeight = height;
            m_FrameRateNumerator = frameRateNumerator;
            m_FrameRateDenominator = frameRateDenominator;
            m_Bandwidth = bandwidth;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pixvana.Video.QualityGroup"/> class.
        /// </summary>
        /// <param name="jsonClass">JSON class.</param>
        public QualityGroup(JSONObject jsonObject)
        {
            Assert.IsNotNull (jsonObject, "jsonObject cannot be null");
            // See: http://wiki.unity3d.com/index.php/SimpleJSON
            m_Name = jsonObject [NameKey];
            m_VideoWidth = jsonObject [VideoWidthKey].AsInt;
            m_VideoHeight = jsonObject [VideoHeightKey].AsInt;
            m_FrameRateNumerator = jsonObject [FrameRateNumeratorKey].AsInt;
            m_FrameRateDenominator = jsonObject [FrameRateDenominatorKey].AsInt;
            m_Bandwidth = jsonObject [BandwidthKey].AsInt;
        }

        public override string ToString()
        {
            return string.Format ("{0} • {1} x {2} • {3:0.##} fps • {4:n0} bps",
                m_Name,
                m_VideoWidth,
                m_VideoHeight,
                ((float)m_FrameRateNumerator / (float)m_FrameRateDenominator),
                m_Bandwidth);
        }
    }
}