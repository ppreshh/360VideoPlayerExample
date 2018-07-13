using Pixvana.Geometry;
using SimpleJSON;
using System;
using UnityEngine;

namespace Pixvana.Opf
{

    /// <summary>
    /// The abstract base class for all projection formats.
    /// </summary>
    public abstract class Format {

        /// <summary>
        /// Initializes a new instance of the <see cref="Pixvana.Opf.Format"/> class.
        /// </summary>
        /// <param name="jsonObject">A Json object.</param>
        /// <param name="version">The OPF schema version.</param>
        public Format(JSONObject jsonObject, int version) { }

        /// <summary>
        /// Creates a GameObject with the correct geometry.
        /// </summary>
        /// <returns>The geometry.</returns>
        /// <param name="textureWidth">The texture width.</param>
        /// <param name="textureHeight">The texture height.</param>
        /// <param name="stereoMode">The type of stereo frame layout.</param>
        /// <param name="isLeftEye"><c>true</c> for left eye; <c>false</c> for right eye.</value></param>
        public virtual GameObject CreateGeometry(int textureWidth, int textureHeight, Projection.StereoMode stereoMode, bool isLeftEye) { return null; }

        /// <summary>
        /// Updates a GameObject with a new texture size.
        /// </summary>
        /// <param name="gameObject">The GameObject to update.</param>
        /// <param name="textureWidth">The texture width.</param>
        /// <param name="textureHeight">The texture height.</param>
        public virtual void UpdateGeometry(GameObject gameObject, int textureWidth, int textureHeight) { }

        /// <summary>
        /// Friendly name for this format.
        /// </summary>
        public abstract string name { get; }
    }
}
