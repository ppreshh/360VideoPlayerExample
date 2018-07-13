using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using SimpleJSON;

namespace Pixvana.Video
{
    public abstract class Track {

        public const int UNKNOWN_BANDWIDTH = -1;    // An integer bandwidth is unknown

        #region Keys
        private const string IdKey                      = "id";
        private const string BandwidthKey               = "bandwidth";
        #endregion

        #region Properties

        private string m_Id = null;
        public string id { get { return m_Id; } }

        private int m_Bandwidth = UNKNOWN_BANDWIDTH;
        public int bandwidth { get { return m_Bandwidth; } }

        #endregion

        public Track(string id, int bandwidth) {

            // Validation
            if (id == null) { throw new ArgumentNullException ("id", "id cannot be null"); }
            if (bandwidth < UNKNOWN_BANDWIDTH) { throw new ArgumentOutOfRangeException ("bandwidth", "bandwidth cannot be less than UNKNOWN_BANDWIDTH"); }

            m_Id = id;
            m_Bandwidth = bandwidth;
        }

        public Track(JSONObject jsonObject)
        {
            Assert.IsNotNull (jsonObject, "jsonObject cannot be null");
            // See: http://wiki.unity3d.com/index.php/SimpleJSON
            m_Id = jsonObject [IdKey];
            m_Bandwidth = jsonObject [BandwidthKey].AsInt;
        }

        public override string ToString()
        {
            return string.Format ("{0} • {1:n0} bps",
                m_Id,
                m_Bandwidth);
        }
    }
}