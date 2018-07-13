using SimpleJSON;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace Pixvana.Opf
{

    /// <summary>
    /// Represents audio format information.
    /// </summary>
    public class Audio
    {

        #region Keys

        private const string SpatialChannelsKey = "spatialChannels";
        private const string HeadLockedChannelsKey = "headLockedChannels";
        private const string SpatialFormatKey = "spatialFormat";

        #endregion

        #region Values

        private const string NoneSpatialFormatValue = "none";
        private const string AmbixSpatialFormatValue = "ambix";

        #endregion

        /// <summary>
        /// A spatial format.
        /// </summary>
        public enum SpatialFormat
        {
            /// <summary>
            /// Undefined spatial format.
            /// </summary>
            Undefined = -1,
            /// <summary>
            /// No spatial audio.
            /// </summary>
            None = 0,
            /// <summary>
            /// Spatial audio in ambiX format.
            /// </summary>
            Ambix = 1,
            Max_SpatialFormat
        }

        /// <summary>
        /// The number of spatial channels (e.g. diegetic).
        /// </summary>
        public int spatialChannels = 0;
        /// <summary>
        /// The number of head-locked channels (e.g. non-diegetic).
        /// </summary>
        public int headLockedChannels = 2;
        /// <summary>
        /// The spatial audio format.
        /// </summary>
        public SpatialFormat spatialFormat = SpatialFormat.None;

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the <see cref="Pixvana.Opf.SpatialFormat"/>.
        /// </summary>
        /// <param name="spatialFormat"></param>
        /// <returns>A <see cref="System.String"/> that represents the <see cref="Pixvana.Opf.SpatialFormat"/>.</returns>
        public static string StringForSpatialFormat(SpatialFormat spatialFormat)
        {
            string stringValue = string.Empty;

            switch (spatialFormat)
            {
                case SpatialFormat.None:
                    {
                        stringValue = "none";
                        break;
                    }
                case SpatialFormat.Ambix:
                    {
                        stringValue = "ambix";
                        break;
                    }
                default:
                    {
                        stringValue = string.Empty;
                        break;
                    }
            }

            return stringValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pixvana.Opf.Audio"/> class.
        /// </summary>
        public Audio() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pixvana.Opf.Audio"/> class.
        /// </summary>
        /// <param name="jsonObject">A Json object.</param>
        /// <param name="version">The OPF schema version.</param>
        public Audio (JSONObject jsonObject, int version)
        {
            Assert.IsNotNull (jsonObject, "jsonObject cannot be null");

            // See: http://wiki.unity3d.com/index.php/SimpleJSON

            // spatialChannels is optional
            JSONNode spatialChannelsNode = jsonObject [SpatialChannelsKey];
            if (spatialChannelsNode != null) 
            {
                spatialChannels = spatialChannelsNode.AsInt;
            }
                
            // headLockedChannels is optional
            JSONNode headLockedChannelsNode = jsonObject [HeadLockedChannelsKey];
            if (headLockedChannelsNode != null) 
            {
                headLockedChannels = headLockedChannelsNode.AsInt;
            }

            // spatialFormat is optional and defaults to none
            string spatialFormatString = jsonObject [SpatialFormatKey];
            spatialFormatString = (spatialFormatString != null ? spatialFormatString : NoneSpatialFormatValue);
            switch (spatialFormatString) {

            case NoneSpatialFormatValue:
                {
                    spatialFormat = SpatialFormat.None;
                    break;
                }
            case AmbixSpatialFormatValue:
                {
                    spatialFormat = SpatialFormat.Ambix;
                    break;
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="Pixvana.Opf.Audio"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="Pixvana.Opf.Audio"/>.</returns>
        public override string ToString ()
        {
            return string.Format ("spatialChannels: {0}, headLockedChannels: {1}, spatialFormat: {2}",
                spatialChannels, headLockedChannels, spatialFormat);
        }
    }
}
