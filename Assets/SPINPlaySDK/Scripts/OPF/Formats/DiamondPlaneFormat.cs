using Pixvana.Geometry;
using UnityEngine;
using System;
using SimpleJSON;

namespace Pixvana.Opf
{

    /// <summary>
    /// Represents a diamond plane format.
    /// </summary>
    public class DiamondPlaneFormat : Format
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="Pixvana.Opf.DiamondPlaneFormat"/> class.
        /// </summary>
        /// <param name="jsonObject">A Json object.</param>
        /// <param name="version">The OPF schema version.</param>
        public DiamondPlaneFormat(JSONObject jsonObject, int version) : base(jsonObject, version)
        {
            // See: http://wiki.unity3d.com/index.php/SimpleJSON
        }

        /// <summary>
        /// Creates a GameObject with icosahedron geometry.
        /// </summary>
        /// <returns>The geometry.</returns>
        /// <param name="textureWidth">The texture width.</param>
        /// <param name="textureHeight">The texture height.</param>
        /// <param name="stereoMode">The type of stereo frame layout.</param>
        /// <param name="isLeftEye"><c>true</c> for left eye; <c>false</c> for right eye.</value></param>
        public override GameObject CreateGeometry(int textureWidth, int textureHeight, Projection.StereoMode stereoMode, bool isLeftEye)
        {
            return Icosahedron.Create();
        }

        /// <summary>
        /// Friendly name for this format.
        /// </summary>
        public override string name { get { return "DiamondPlane"; } }
    }
}