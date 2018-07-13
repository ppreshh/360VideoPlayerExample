using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;


// Handles VariSqueeze as a transform

namespace Pixvana.Opf
{

    /// <summary>
    /// Represents a uvMap transform.
    /// </summary>
    public class VariSqueezeTransform : VideoTransform
    {
        private const string MapsKey = "vsMaps";
        private const string IdKey = "id";

        #region Keys
        // JSON key words
        private const string EquirectWidthKey = "equirectWidth";
        private const string EquirectHeightKey = "equirectHeight";
        private const string SqueezedWidthKey = "squeezedWidth";
        private const string SqueezedHeightKey = "squeezedHeight";
        private const string IdentityWidthKey = "identityWidth";
        private const string IdentityHeightKey = "identityHeight";
        private const string SmoothnessHorizontalKey = "smoothnessHorizontal";
        private const string SmoothnessVerticalKey = "smoothnessVertical";

        #endregion


        /// <summary>
        /// The final equirect image Width in pixels
        /// </summary>
        public int equirectWidth = 7680;
        /// <summary>
        /// The final equirect image height in pixels
        /// </summary>
        public int equirectHeight = 3840;
        /// <summary>
        /// The squeezed image width in pixels.
        /// </summary>
        public int squeezedWidth = 3840;
        /// <summary>
        /// The squeezed image height in pixels.
        /// </summary>
        public int squeezedHeight = 2160;
        /// <summary>
        /// The center untouched width in pixels
        /// </summary>
        public int identityWidth = 1500;
        /// <summary>
        /// The center untouched height in pixels
        /// </summary>
        public int identityHeight = 1500;

        /// <summary>
        /// Factor of smoothness from untouched width to squeezed image edge 0 to 1.0  0=linear
        /// </summary>
        public float widthSmoothness = .8f;
        /// <summary>
        /// Factor of smoothness from untouched height to squeezed image edge 0 to 1.0  0=linear
        /// </summary>
        public float heightSmoothness = .8f;

        private Texture2D uvXTexture;  // holds 1 line of texture which will hold floating point UV info for X axis
        private Texture2D uvYTexture;  // holds 1 line of texture which will hold floating point UV info for Y axis

        // Take a JSON object and extract the info
        public VariSqueezeTransform(JSONObject jsonObject, int version)
        {
            Assert.IsNotNull(jsonObject, "jsonObject cannot be null");
            Debug.Log("VariSqueezeTransform");
            JSONNode equirectWidthNode = jsonObject[EquirectWidthKey];
            Assert.IsNotNull(equirectWidthNode, EquirectWidthKey + " is required");
            equirectWidth = equirectWidthNode.AsInt;
            Assert.IsTrue(equirectWidth > 0, EquirectWidthKey + " must be greater than 0");

            JSONNode equirectHeightNode = jsonObject[EquirectHeightKey];
            Assert.IsNotNull(equirectHeightNode, EquirectHeightKey + " is required");
            equirectHeight = equirectHeightNode.AsInt;
            Assert.IsTrue(equirectHeight > 0, EquirectHeightKey + " must be greater than 0");

            JSONNode squeezedWidthNode = jsonObject[SqueezedWidthKey];
            Assert.IsNotNull(squeezedWidthNode, SqueezedWidthKey + " is required");
            squeezedWidth = squeezedWidthNode.AsInt;
            Assert.IsTrue(squeezedWidth > 0, SqueezedWidthKey + " must be greater than 0");

            JSONNode squeezedHeightNode = jsonObject[SqueezedHeightKey];
            Assert.IsNotNull(squeezedHeightNode, SqueezedHeightKey + " is required");
            squeezedHeight = squeezedHeightNode.AsInt;
            Assert.IsTrue(squeezedHeight > 0, SqueezedHeightKey + " must be greater than 0");

			// identity width and height can be 0
            JSONNode identityWidthNode = jsonObject[IdentityWidthKey];
            Assert.IsNotNull(identityWidthNode, IdentityWidthKey + " is required");
            identityWidth = identityWidthNode.AsInt;
            Assert.IsTrue(identityWidth >= 0, IdentityWidthKey + " must be greater than or equal to 0");

            JSONNode identityHeightNode = jsonObject[IdentityHeightKey];
            Assert.IsNotNull(identityHeightNode, IdentityHeightKey + " is required");
            identityHeight = identityHeightNode.AsInt;
           Assert.IsTrue(identityHeight >= 0, IdentityHeightKey + " must be greater than or equal to 0");

            JSONNode widthSmoothnessNode = jsonObject[SmoothnessHorizontalKey];
            Assert.IsNotNull(widthSmoothnessNode, SmoothnessHorizontalKey + " is required");
            widthSmoothness = widthSmoothnessNode.AsFloat;
            Assert.IsTrue(widthSmoothness >= 0.0f && widthSmoothness <= 1.0f, SmoothnessHorizontalKey + " must be between 0.0 and 1.0");

            JSONNode heightSmoothnessNode = jsonObject[SmoothnessVerticalKey];
            Assert.IsNotNull(heightSmoothnessNode, SmoothnessVerticalKey + " is required");
            heightSmoothness = heightSmoothnessNode.AsFloat;
            Assert.IsTrue(heightSmoothness >= 0.0f && heightSmoothness <= 1.0f, SmoothnessVerticalKey + " must be between 0.0 and 1.0");


            BuildUVArrays();  // when all info available, go ahead and create texturemaps for UV
        }


        // Create a floating point texture 1 pixel tall (used for both X and Y)
        // Only uses 1 channel but creating RGBA based on reliable Unity support
        // S8 can only deal with half Floats (still testing)
        Texture2D CreateWidthFloatingTexture(int inWidth)
        {	// last parameter is colorspace. False = Gamma, True = linear
        	 Texture2D theTexture = new Texture2D(inWidth, 1, TextureFormat.RFloat, false, false); // for S8
          //   Texture2D theTexture = new Texture2D(inWidth, 1, TextureFormat.RFloat, false, false); // for PC
            theTexture.wrapMode = TextureWrapMode.Clamp; // prevents seam in back
            return theTexture;

        }
	// generate empty texture and fill in with bezier curve values
	 Texture2D CreateTextureRamp(int original, int squeezed, int  identity, float smoothness)
        {

            Texture2D theTexture = CreateWidthFloatingTexture(original);

            VarisqueezeEquirectCurveBezier theCurve = new VarisqueezeEquirectCurveBezier();
            theCurve.SetupBezier(original, squeezed, identity, smoothness);
            for (int i = 0; i < original; i++)
            {
                float index = (float)i;
                float x = (theCurve.getX(index));
                float theValue = x / (float)squeezed;
                //  Debug.Log("value for " + i + " is: " + x + " 0 to 1: " + theValue);
                Color pixelColor = new Color(theValue, theValue, theValue, 1.0f);
                theTexture.SetPixel(i, 0, pixelColor);
            }

            theTexture.Apply(false, false);
			return theTexture;

    	}


    	 // Creates x and Y UV texturemaps for VAriSqueeze.
        // Uses stored values for parameters
        void BuildUVArrays()
        {
           uvXTexture =  CreateTextureRamp(equirectWidth, squeezedWidth, identityWidth, widthSmoothness);
            Debug.Log("Width: " + equirectWidth + "Squeeze Width: " + squeezedWidth + "IdentityWidths " + identityWidth + " widthSmoothness " + widthSmoothness);


            if (equirectHeight != 0)
            {

				 uvYTexture =  CreateTextureRamp(equirectHeight, squeezedHeight, identityHeight, heightSmoothness);
				 Debug.Log("equirectHeight: " + equirectHeight + " squeezedHeight: " + squeezedHeight + " identityHeight " + identityHeight + " heightSmoothness " + heightSmoothness);
            }
            else
            {   // no height specified, so punt. In theory should not have gotten here but could be useful in future
                // for option to simply do a horizontal squeeze as in prototype

                equirectHeight = 1920; // this should be a real value but for now reasonable
				Debug.Log("No equirect height specified for VariSqueeze!");
                BuildUVYLinearArray(equirectHeight);
            }
        }

        // assigns the created UV maps to Shader Selector
        public override void UpdateShader(Pixvana.Video.ShaderSelector ss, Action completion)
        {
            Debug.Log("Varisqueeze UpdateShader");
            ss.vsMaps = new Texture2D[2] { uvXTexture, uvYTexture };


            float identWidthStart = ((equirectWidth / 2.0f) - (identityWidth / 2.0f)) / equirectWidth;
            float identWidthEnd = ((equirectWidth / 2.0f) + (identityWidth / 2.0f)) / equirectWidth;
            float identHeightStart = ((equirectHeight / 2.0f) - (identityHeight / 2.0f)) / equirectHeight;
            float identHeightEnd = ((equirectHeight / 2.0f) + (identityHeight / 2.0f)) / equirectHeight;


            ss.left = identWidthStart;
            ss.right = identWidthEnd;
            ss.top = identHeightStart;
            ss.bottom = identHeightEnd;

            completion();
        }

		  //  Used to create a a texturemap and fills in linear UV values for height
        // only meant for backup in case no height info
        void BuildUVYLinearArray(int totalHeight)
        {

            Texture2D theTexture = CreateWidthFloatingTexture(totalHeight);


            for (int i = 0; i < totalHeight; i++)
            {
                float index = (float)i;

                float theValue = index / (float)totalHeight;

                //  Debug.Log("value for " + i + " is: " + x + " 0 to 1: " + theValue);

                Color pixelColor = new Color(theValue, theValue, theValue, 1.0f);
                theTexture.SetPixel(i, 0, pixelColor);
            }

            theTexture.Apply(false, false);
            uvYTexture = theTexture;



        }
		// for testing
		void BuildUVXLinearArray(int totalWidth)
        {
            Texture2D theTexture = CreateWidthFloatingTexture(totalWidth);

            for (int i = 0; i < totalWidth; i++)
            {
                float index = (float)i;

                float theValue = index / (float)totalWidth;

                Color pixelColor = new Color(theValue, theValue, theValue, 1.0f);
                theTexture.SetPixel(i, 0, pixelColor);
				Color rdPixel =  theTexture.GetPixel(i, 0);
				Debug.Log("value for " + i + " is: " + index + " 0 to 1: " + theValue + " when read back: " + rdPixel.r);


            }

            theTexture.Apply(false, false);
            uvXTexture = theTexture;

        }



    }

}

