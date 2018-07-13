using Pixvana.Geometry;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Pixvana.Opf
{

    /// <summary>
    /// Represents a frustum format.
    /// </summary>
    public class FrustumFormat : Format
    {

        #region Keys

        private const string RadiusFrontKey 				= "radiusFront";
        private const string RadiusBackKey 					= "radiusBack";
        private const string ZFrontKey					 	= "zFront";
        private const string ZBackKey 						= "zBack";
        private const string PaddingKey						= "padding";

        #endregion

        private const int PaddingDefault = 1;

        /// <summary>
        /// The radius of the front face.
        /// </summary>
        public float radiusFront = 0.0f;
        /// <summary>
        /// The radius of the back face.
        /// </summary>
        public float radiusBack = 0.0f;
        /// <summary>
        /// The coordinate of the front face.
        /// </summary>
        public float zFront = 0.0f;
        /// <summary>
        /// The coordinate of the back face.
        /// </summary>
        public float zBack = 0.0f;
        /// <summary>
        /// The number of padded pixels along the center seam.
        /// </summary>
        public int padding = PaddingDefault;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pixvana.Opf.FrustumFormat"/> class.
        /// </summary>
        /// <param name="jsonObject">A Json object.</param>
        /// <param name="version">The OPF schema version.</param>
        public FrustumFormat (JSONObject jsonObject, int version) : base (jsonObject, version)
        {
            Assert.IsNotNull (jsonObject, "jsonObject cannot be null");

            // See: http://wiki.unity3d.com/index.php/SimpleJSON

            JSONNode radiusFrontNode = jsonObject [RadiusFrontKey];
            Assert.IsNotNull (radiusFrontNode, RadiusFrontKey + " is required");
            radiusFront = radiusFrontNode.AsFloat;
            Assert.IsTrue (radiusFront >= 0.0f, RadiusFrontKey + " must be greater than or equal to 0");

            JSONNode radiusBackNode = jsonObject [RadiusBackKey];
            Assert.IsNotNull (radiusBackNode, RadiusBackKey + " is required");
            radiusBack = radiusBackNode.AsFloat;
            Assert.IsTrue (radiusBack >= 0.0f, RadiusBackKey + " must be greater than or equal to 0");

            JSONNode zFrontNode = jsonObject [ZFrontKey];
            Assert.IsNotNull (zFrontNode, ZFrontKey + " is required");
            zFront = zFrontNode.AsFloat;
            Assert.IsTrue (zFront >= 0.0f, ZFrontKey + " must be greater than or equal to 0");

            JSONNode zBackNode = jsonObject [ZBackKey];
            Assert.IsNotNull (zBackNode, ZBackKey + " is required");
            zBack = zBackNode.AsFloat;
            Assert.IsTrue (zBack <= 0.0f, ZBackKey + " must be less than or equal to 0");

            JSONNode paddingNode = jsonObject [PaddingKey];
            padding = (paddingNode != null ? paddingNode.AsInt : PaddingDefault);
            Assert.IsTrue (padding >= 0, PaddingKey + " must be greater than or equal to 0");
        }

        // NOTE: MSwanson - Don't like that we have to expose a Projection type (StereoMode) here. This is only to accommodate
        //       pixel-accurate texture seams, which we eventually hope to replace with a resolution independent replacement.
        /// <summary>
        /// Creates a GameObject with frustum geometry.
        /// </summary>
        /// <returns>The geometry.</returns>
        /// <param name="textureWidth">The texture width.</param>
        /// <param name="textureHeight">The texture height.</param>
        /// <param name="stereoMode">The type of stereo frame layout.</param>
        /// <param name="isLeftEye"><c>true</c> for left eye; <c>false</c> for right eye.</value></param>
        public override GameObject CreateGeometry(int textureWidth, int textureHeight, Projection.StereoMode stereoMode, bool isLeftEye)
        {
            GameObject gameObject = Frustum.Create ();

            Frustum frustum = gameObject.GetComponent<Frustum> ();
            frustum.frontRadius = radiusFront;
            frustum.backRadius = radiusBack;
            frustum.zFront = zFront;
            frustum.zBack = zBack;
            frustum.padding = padding;

            // Is there extra frame padding to consider?
            Shape.TextureEdge textureEdge = Shape.TextureEdge.None;
            if (stereoMode == Projection.StereoMode.StereoTopBottom)
            {
                textureEdge = (isLeftEye ? Shape.TextureEdge.Bottom : Shape.TextureEdge.Top);
            }
            else if (stereoMode == Projection.StereoMode.StereoLeftRight)
            {
                textureEdge = (isLeftEye ? Shape.TextureEdge.Right : Shape.TextureEdge.Left);
            }
            frustum.textureEdge = textureEdge;

            UpdateGeometry(gameObject, textureWidth, textureHeight);

            return gameObject;
        }

        public override void UpdateGeometry(GameObject gameObject, int textureWidth, int textureHeight)
        {
            Frustum frustum = gameObject.GetComponent<Frustum>();
            frustum.textureWidth = textureWidth;
            frustum.textureHeight = textureHeight;
        }

        /// <summary>
        /// Friendly name for this format.
        /// </summary>
        public override string name { get { return "Frustum"; } }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="Pixvana.Opf.FrustumFormat"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="Pixvana.Opf.FrustumFormat"/>.</returns>
        public override string ToString ()
        {
            return string.Format ("\nradiusFront: {0}, radiusBack: {1}, zFront: {2}, zBack: {3}, padding: {4}",
                radiusFront, radiusBack, zFront, zBack, padding);
        }
    }
}