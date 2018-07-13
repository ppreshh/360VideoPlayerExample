using UnityEngine;

namespace Pixvana.Video
{

    /// <summary>
    /// Player that streams video to a target texture
    /// </summary>
    [AddComponentMenu("SPIN Play SDK/Player", 0)]
    [HelpURL("http://www.pixvana.com/")]
    public partial class Player : PlayerBase 
    {
        #region Configuration keys

        /// <summary>
        /// Contains a bool that indicates whether the player should force frame synchronization at end-of-frame
        /// </summary>
        public const string ForceFrameSyncKey           = "ForceFrameSync";

        /// <summary>
        /// Contains a bool that is a hint suggesting that the player should deliver yuv video frames
        /// </summary>
        /// All platform players are required to be able to deliver their video frames as (a)RGB textures
        /// in the "normal" y-orientation where the first scan line of the texture data (the lowest
        /// memory address) corresponds to the visual bottom of the frame.  Some platform players are
        /// able to also able to deliver frames as the "raw" yuv buffers that are the typical output
        /// of h264/h265/etc... video decoders.  In some cases the UnitySDK itself can operate more
        /// efficiently if it is given the raw yuv buffers instead of the rgb buffers.  This key
        /// is present and set to true in those cases.
        public const string PreferYUVBuffersKey = "PreferYUVBuffers";

        /// <summary>
        /// Contains a <see cref="System.Globalization.CultureInfo"/> object that indicates the preferred language
        /// </summary>
        public const string PreferredLanguageKey        = "PreferredLanguage";

        /// <summary>
        /// Contains an int that indicates the number of spatial audio channels
        /// </summary>
        public const string SpatialChannelsKey = "SpatialChannels";

        /// <summary>
        /// Contains an int that indicates the number of head-locked audio channels
        /// </summary>
        public const string HeadLockedChannelsKey = "HeadLockedChannels";

        /// <summary>
        /// Contains a string that indicates the spatial audio format
        /// </summary>
        public const string SpatialFormatKey = "SpatialFormat";

        #endregion
    }
}
