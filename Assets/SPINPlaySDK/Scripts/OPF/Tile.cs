using SimpleJSON;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace Pixvana.Opf
{

    /// <summary>
    /// Represents a tile.
    /// </summary>
    public class Tile
    {

        #region Keys

        private const string IdKey = "id";
        private const string YawDegreesKey = "yawDegrees";
        private const string PitchDegreesKey = "pitchDegrees";
        private const string RollDegreesKey = "rollDegrees";

        #endregion

        /// <summary>
        /// The ID of the tile.
        /// </summary>
        public string id = "";
        /// <summary>
        /// The yaw orientation of the tile (in degrees).
        /// </summary>
        public float yawDegrees = 0.0f;
        /// <summary>
        /// The pitch orientation of the tile (in degrees).
        /// </summary>
        public float pitchDegrees = 0.0f;
        /// <summary>
        /// The roll orientation of the tile (in degrees).
        /// </summary>
        public float rollDegrees = 0.0f;
        /// <summary>
        /// A quaternion that represents the combined yaw, pitch, and roll.
        /// </summary>
        public Quaternion heading = Quaternion.identity;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pixvana.Opf.Tile"/> class.
        /// </summary>
        public Tile() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pixvana.Opf.Tile"/> class.
        /// </summary>
        /// <param name="jsonObject">A Json object.</param>
        /// <param name="version">The OPF schema version.</param>
        public Tile (JSONObject jsonObject, int version)
        {
            Assert.IsNotNull (jsonObject, "jsonObject cannot be null");

            // See: http://wiki.unity3d.com/index.php/SimpleJSON

            // According to v1 of the OPF specification, id is required,
            // but it CAN be an empty string.
            // NOTE: SimpleJSON reads an empty string as null, so this
            //       test will fail for empty strings for now.
            id = jsonObject [IdKey];
            Assert.IsNotNull (id, IdKey + " is required");
            Assert.IsTrue (id.IndexOf (".") == -1, IdKey + " cannot contain a period");

            yawDegrees = jsonObject [YawDegreesKey].AsFloat;
            pitchDegrees = jsonObject [PitchDegreesKey].AsFloat;
            rollDegrees = jsonObject [RollDegreesKey].AsFloat;

            // Convert to Unity heading for convenience
            heading = Quaternion.Euler (-pitchDegrees, yawDegrees, rollDegrees);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="Pixvana.Opf.Tile"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="Pixvana.Opf.Tile"/>.</returns>
        public override string ToString ()
        {
            return string.Format ("id: {0}, yawDegrees: {1}, pitchDegrees: {2}, rollDegrees: {3}",
                id, yawDegrees, pitchDegrees, rollDegrees);
        }
    }
}
