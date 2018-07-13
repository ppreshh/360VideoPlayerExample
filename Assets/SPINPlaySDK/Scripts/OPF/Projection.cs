using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Namespace that contains all Open Projection Format functionality.
/// </summary>
namespace Pixvana.Opf
{
    /// <summary>
    /// Represents an OPF projection.
    /// </summary>
    public class Projection
    {

        private const int minimumRequiredOpfVersion = 1;    // Minimum required OPF version

        #region Keys

        private const string VersionKey 				= "version";
        private const string UrlKey 					= "url";
        private const string BackgroundColorKey         = "backgroundColor";
        private const string HeadingKey 				= "heading";
        private const string HeadingYawDegreesKey 		= "yawDegrees";
        private const string HeadingPitchDegreesKey 	= "pitchDegrees";
        private const string HeadingRollDegreesKey 		= "rollDegrees";
        private const string AudioKey                   = "audio";
        private const string StereoModeKey 				= "stereoMode";
        private const string FormatKey 					= "format";
        private const string FormatInfoKey 				= "formatInfo";
        private const string TilesKey					= "tiles";
        private const string UserDataKey				= "userData";
        private const string TransformKey               = "transform";
        private const string TransformInfoKey           = "transformInfo";
        private const string TransformInfoTypeKey       = "type";

        #endregion

        #region Values

        private const string MonoStereoModeValue        = "mono";
        private const string StereoTopBottomValue       = "stereoTopBottom";
        private const string StereoLeftRightValue       = "stereoLeftRight";
        private const string StereoInterleavedValue     = "stereoInterleaved";
        private const string EquirectangularFormatValue = "equirectangular";
        private const string FrustumFormatValue         = "frustum";
        private const string UvMapValue                 = "uvMap";
        private const string vsTransform                = "vstransform"; // VariSqueeze transform
        private const string DiamondPlaneFormatValue    = "diamondPlane";

        #endregion

        /// <summary>
        /// A stereo mode.
        /// </summary>
        public enum StereoMode
        {
            /// <summary>
            /// Undefined stereo mode.
            /// </summary>
            Undefined = -1,
            /// <summary>
            /// Monoscopic mode.
            /// </summary>
            Mono = 0,
            /// <summary>
            /// Stereoscopic mode with left eye conent on the top and right eye content on the bottom.
            /// </summary>
            StereoTopBottom = 1,
            /// <summary>
            /// Stereoscopic mode with left eye content on the left and right eye content on the right.
            /// </summary>
            StereoLeftRight = 2,
            /// <summary>
            /// Stereoscopic mode with left eye content on odd frames and right eye content on even frames.
            /// </summary>
            StereoInterleaved = 3,
            Max_StereoMode
        }

        /// <summary>
        /// The OPF schema version.
        /// </summary>
        public int version = 1;
        /// <summary>
        /// The URL for the MP4 media or MPEG-DASH manifest file.
        /// </summary>
        public Uri url = null;
        /// <summary>
        /// The background color.
        /// </summary>
        public Color backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        /// <summary>
        /// The initial playback heading.
        /// </summary>
        public Quaternion heading = Quaternion.identity;
        /// <summary>
        /// The audio format.
        /// </summary>
        public Audio audio = null;
        /// <summary>
        /// The type of stereo frame layout.
        /// </summary>
        public StereoMode stereoMode = StereoMode.Undefined;
        /// <summary>
        /// The projection format.
        /// </summary>
        public Format format = null;
        /// <summary>
        /// The list of tiles.
        /// </summary>
        public List<Tile> Tiles = new List<Tile>();
        /// <summary>
        /// User-defined data.
        /// </summary>
        public JSONNode userData = null;
        /// <summary>
        /// TransformInfo (optional).
        /// </summary>
        public VideoTransform vTransform = null;


        private Uri m_SourceUri = null;

        /// <summary>
        /// opf version which first added the transformInfo field
        /// </summary>
        private const int minTransformInfoVer = 2;


        /// <summary>
        /// Initializes a new instance of the <see cref="Pixvana.Opf.Projection"/> class with a source URI.
        /// </summary>
        /// <param name="jsonString">A Json string.</param>
        /// <param name="sourceUri">The absolute URI of the OPF data.</param>
        public Projection (string jsonString, Uri sourceUri = null)
        {
            Assert.IsNotNull (jsonString, "jsonString cannot be null");

            m_SourceUri = sourceUri;

            // See: http://wiki.unity3d.com/index.php/SimpleJSON

            JSONNode jsonObject = JSON.Parse (jsonString);
            if (jsonObject == null)
                throw new System.Exception("error parsing json");

            // version is required and must be >= minimumRequiredOpfVersion
            version = jsonObject [VersionKey].AsInt;
            Assert.IsTrue (version >= minimumRequiredOpfVersion, VersionKey + " must be equal to or greater than " + minimumRequiredOpfVersion);

            // url is required
            string urlString = jsonObject [UrlKey];
            Assert.IsTrue (urlString != null && urlString.Length > 0, UrlKey + " is required");
            url = EnsureAbsoluteUri(new Uri (urlString, UriKind.RelativeOrAbsolute));

            // backgroundColor is optional
            string backgroundColorString = jsonObject[BackgroundColorKey];
            backgroundColor = (backgroundColorString != null ? ColorTools.ParseColor(backgroundColorString) : new Color(0.0f, 0.0f, 0.0f, 0.0f));

            // heading is optional
            heading = ParseHeading (jsonObject [HeadingKey].AsObject);

            // audio is optional
            JSONNode audioNode = jsonObject [AudioKey];
            if (audioNode != null) 
            {
                audio = new Audio (audioNode.AsObject, version);
            } 
            else 
            {
                audio = new Audio ();
            }

            // stereoMode is optional and defaults to mono
            string stereoModeString = jsonObject [StereoModeKey];
            stereoModeString = (stereoModeString != null ? stereoModeString : MonoStereoModeValue);
            switch (stereoModeString) {

            case MonoStereoModeValue:
                {
                    stereoMode = StereoMode.Mono;
                    break;
                }
            case StereoTopBottomValue:
                {
                    stereoMode = StereoMode.StereoTopBottom;
                    break;
                }
            case StereoLeftRightValue:
                {
                    stereoMode = StereoMode.StereoLeftRight;
                    break;
                }
            case StereoInterleavedValue:
                {
                    stereoMode = StereoMode.StereoInterleaved;
                    break;
                }
            }
            Assert.IsTrue (stereoMode != StereoMode.Undefined, "undefined " + StereoModeKey);

            // format is required
            string formatString = jsonObject [FormatKey];
            Assert.IsTrue (formatString != null && formatString.Length > 0, FormatKey + " is required");
            JSONObject formatInfoObject = jsonObject [FormatInfoKey].AsObject;
            switch (formatString) {

            case EquirectangularFormatValue:
                {
                    format = new EquirectangularFormat (formatInfoObject, version);
                    break;
                }
            case FrustumFormatValue:
                {
                    format = new FrustumFormat (formatInfoObject, version);
                    break;
                }
            case DiamondPlaneFormatValue:
                {
                    format = new DiamondPlaneFormat(formatInfoObject, version);
                    break;
                }
            }
            Assert.IsNotNull (format, "undefined " + FormatKey);

            // tiles are optional
            JSONNode tilesNode = jsonObject [TilesKey];
            if (tilesNode != null) {

                foreach (JSONObject tileObject in tilesNode.AsArray) {

                    Tiles.Add (new Tile (tileObject, version));
                }

                Assert.IsTrue (Tiles.Count > 0, TilesKey + " must contain 1 or more items");

            } else {

                // default to a single tile at (0, 0, 0)
                Tiles.Add(new Tile());
            }

            // userData is optional
            userData = jsonObject [UserDataKey];

            // if the version of the opf is new enough to potentially contain
            // a transformInfo object, then look for it and use it if it is there
            vTransform = null;
            if (version >= minTransformInfoVer)
            {
                JSONNode jn = jsonObject[TransformKey];
                if (jn != null)
                {
                    string transformType = jn;
                    switch (transformType)
                    {
                        case UvMapValue:
                            {
                                JSONObject jc = jsonObject[TransformInfoKey].AsObject;
                                vTransform = new UvMapTransform(jc, version);
                                break;
                            }
                        case vsTransform:
                            {
                                JSONObject jc = jsonObject[TransformInfoKey].AsObject;
                                vTransform = new VariSqueezeTransform(jc, version);
                                break;
                            }
                    }
                    Assert.IsTrue(vTransform != null, "undefined transformInfo type " + transformType);
                }
            }
            if (vTransform == null)
            {
                vTransform = new IdentityVideoTransform();
            }

        }

        public VideoTransform videoTransform
        {
            get {
                return vTransform;
            }
        }

        private Quaternion ParseHeading(JSONObject jsonObject) {

            float yawDegrees = jsonObject [HeadingYawDegreesKey].AsFloat;
            float pitchDegrees = jsonObject [HeadingPitchDegreesKey].AsFloat;
            float rollDegrees = jsonObject [HeadingRollDegreesKey].AsFloat;

            // Accommodate user-to-Unity expectations
            return Quaternion.Euler(pitchDegrees, -yawDegrees, rollDegrees);
        }

        /// <summary>
        /// Returns the tile with the closest view center to the given view heading and tile heading offset.
        /// </summary>
        /// <returns>The tile.</returns>
        /// <param name="heading">A heading.</param>
        /// <param name="tileHeadingOffset">A heading offset.</param>
        public Tile ClosestTileForHeading(Vector3 heading, Quaternion tileHeadingOffset) {

            Vector3 headingPosition = Quaternion.Euler (heading) * Vector3.forward;

            Tile closestTile = null;
            float closestDistance = float.MaxValue;

            foreach (Tile tile in Tiles) {

                Vector3 tilePosition = tileHeadingOffset * tile.heading * Vector3.forward;
                float distance = Vector3.Distance(headingPosition, tilePosition);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTile = tile;
                }
            }

            return closestTile;
        }

        /// <summary>
        /// Returns a tile with the given tile ID.
        /// </summary>
        /// <returns>The tile.</returns>
        /// <param name="id">An ID.</param>
        public Tile TileWithID(string id) {

            // note: an empty id "" is valid in the case of non-tiled formats
            Assert.IsTrue (id != null, "id is required");

            Tile tile = null;

            foreach (Tile searchTile in Tiles) {

                if (searchTile.id.Equals (id)) {

                    tile = searchTile;
                    break;
                }
            }

            return tile;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="Pixvana.Opf.Projection"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="Pixvana.Opf.Projection"/>.</returns>
        public override string ToString ()
        {
            string tiles = "";
            foreach (Tile tile in Tiles) {

                tiles += "\n    " + tile;
            }

            return string.Format ("url: {0}, heading: {1}, audio: {2}, stereoMode: {3}, format: {4}, tiles: {5}",
                url, heading, audio, stereoMode, format, tiles);
        }

        /// <summary>
        /// Ensures that a URI is absolute.
        /// </summary>
        /// <param name="uri">An absolute or relative URI.</param>
        /// <returns></returns>
        private Uri EnsureAbsoluteUri(Uri uri)
        {
            Uri absoluteUri = uri;

            // If this is a relative URI and we have an absolute source, convert to an absolute URI
            if (!uri.IsAbsoluteUri &&
                m_SourceUri != null)
            {
                string absoluteUriString = m_SourceUri.AbsoluteUri;

                int lastSlashPosition = absoluteUriString.LastIndexOf("/");
                if (lastSlashPosition > -1)
                {
                    absoluteUri = new Uri(absoluteUriString.Substring(0, lastSlashPosition) + "/" + uri, UriKind.Absolute);
                }
                Debug.Assert(absoluteUri.IsAbsoluteUri, "Could not create absolute Uri from: " + uri);
            }

            return absoluteUri;
        }
    }
}