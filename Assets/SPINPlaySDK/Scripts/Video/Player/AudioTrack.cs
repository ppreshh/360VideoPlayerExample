using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace Pixvana.Video
{
    public class AudioTrack : Track {

        public const int UNKNOWN_CHANNEL_COUNT = -1;    // An integer number of channels is unknown

        #region Keys
        private const string ChannelCountKey            = "channelCount";
        private const string LanguageKey                = "language";
        #endregion

        #region Properties

        private int m_ChannelCount = UNKNOWN_CHANNEL_COUNT;
        public int channelCount { get { return m_ChannelCount; } }

        private string m_Language = null;
        public string language { get { return m_Language; } }

        #endregion

        public AudioTrack(string id, int bandwidth, int channelCount, string language) : base(id, bandwidth) {

            // Validation
            if (channelCount < UNKNOWN_CHANNEL_COUNT) { throw new ArgumentOutOfRangeException ("channelCount", "channelCount cannot be less than UNKNOWN_CHANNEL_COUNT"); }

            m_ChannelCount = channelCount;
            m_Language = language;
        }

        public AudioTrack(JSONObject jsonObject) : base (jsonObject)
        {
            // See: http://wiki.unity3d.com/index.php/SimpleJSON
            m_ChannelCount = jsonObject [ChannelCountKey].AsInt;
            m_Language = jsonObject [LanguageKey];
        }

        public override string ToString()
        {
            return string.Format ("{0} • {1} ch • {2}",
                base.ToString(),
                m_ChannelCount,
                m_Language);
        }
    }
}
