using Pixvana.Geometry;
using UnityEngine;
using UnityEngine.Assertions;
using System;
using SimpleJSON;

namespace Pixvana.Opf
{

    /// <summary>
    /// Represents an equirectangular format.
    /// </summary>
    public class EquirectangularFormat : Format {

        #region Keys

        private const string SourceHorizontalFovKey             = "sourceHorizontalFov";
        private const string SourceVerticalFovKey               = "sourceVerticalFov";
        private const string ClipHorizontalFovKey               = "clipHorizontalFov";
        private const string ClipVerticalFovKey                 = "clipVerticalFov";

        #endregion

        private const float ClipHorizontalFovDefault            = 360.0f;
        private const float ClipVerticalFovDefault              = 180.0f;

        /// <summary>
        /// The source horizontal field of view.
        /// </summary>
        public float sourceHorizontalFov = ClipHorizontalFovDefault;

        /// <summary>
        /// The source vertical field of view.
        /// </summary>
        public float sourceVerticalFov = ClipVerticalFovDefault;

        /// <summary>
        /// The clipped horizontal field of view.
        /// </summary>
        public float clipHorizontalFov = ClipHorizontalFovDefault;

        /// <summary>
        /// The clipped vertical field of view.
        /// </summary>
        public float clipVerticalFov = ClipVerticalFovDefault;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pixvana.Opf.EquirectangularFormat"/> class.
        /// </summary>
        /// <param name="jsonObject">A Json object.</param>
        /// <param name="version">The OPF schema version.</param>
        public EquirectangularFormat(JSONObject jsonObject, int version) : base(jsonObject, version)
        {
            if (jsonObject != null)
            {
                // See: http://wiki.unity3d.com/index.php/SimpleJSON

                JSONNode sourcehorizontalFovNode = jsonObject[SourceHorizontalFovKey];
                sourceHorizontalFov = (sourcehorizontalFovNode != null ? sourcehorizontalFovNode.AsFloat : ClipHorizontalFovDefault);
                Assert.IsTrue(sourceHorizontalFov >= 0.5f && sourceHorizontalFov <= 360.0f, SourceHorizontalFovKey + " must be greater than or equal to 0.5 and less than or equal to 360.0");

                JSONNode sourceVerticalFovNode = jsonObject[SourceVerticalFovKey];
                sourceVerticalFov = (sourceVerticalFovNode != null ? sourceVerticalFovNode.AsFloat : ClipVerticalFovDefault);
                Assert.IsTrue(sourceVerticalFov >= 0.5f && sourceVerticalFov <= 180.0f, SourceVerticalFovKey + " must be greater than or equal to 0.5 and less than or equal to 180.0");

                // Defaults to sourceHorizontalFov if no value is provided
                JSONNode clipHorizontalFovNode = jsonObject[ClipHorizontalFovKey];
                clipHorizontalFov = (clipHorizontalFovNode != null ? clipHorizontalFovNode.AsFloat : sourceHorizontalFov);
                Assert.IsTrue(clipHorizontalFov >= 0.5f && clipHorizontalFov <= 360.0f, ClipHorizontalFovKey + " must be greater than or equal to 0.5 and less than or equal to 360.0");

                // Defaults to sourceVerticalFov if no value is provided
                JSONNode clipVerticalFovNode = jsonObject[ClipVerticalFovKey];
                clipVerticalFov = (clipVerticalFovNode != null ? clipVerticalFovNode.AsFloat : sourceVerticalFov);
                Assert.IsTrue(clipVerticalFov >= 0.5f && clipVerticalFov <= 180.0f, ClipVerticalFovKey + " must be greater than or equal to 0.5 and less than or equal to 180.0");
            }
        }

        /// <summary>
        /// Creates a GameObject with equirectangular geometry.
        /// </summary>
        /// <returns>The geometry.</returns>
        /// <param name="textureWidth">The texture width.</param>
        /// <param name="textureHeight">The texture height.</param>
        /// <param name="stereoMode">The type of stereo frame layout.</param>
        /// <param name="isLeftEye"><c>true</c> for left eye; <c>false</c> for right eye.</value></param>
        public override GameObject CreateGeometry(int textureWidth, int textureHeight, Projection.StereoMode stereoMode, bool isLeftEye)
        {
            GameObject gameObject = Sphere.Create ();

            Sphere sphere = gameObject.GetComponent<Sphere>();
            sphere.sourceHorizontalFov = sourceHorizontalFov;
            sphere.sourceVerticalFov = sourceVerticalFov;
            sphere.clipHorizontalFov = clipHorizontalFov;
            sphere.clipVerticalFov = clipVerticalFov;

            return gameObject;
        }

        /// <summary>
        /// Friendly name for this format.
        /// </summary>
        public override string name { get { return "Equirectangular"; } }
    }
}