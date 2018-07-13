using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pixvana.Video;

namespace Pixvana.Video
{
    public class ShaderSelector
    {
        public bool produceLinearRGB = false;

        public PlayerBase.TextureColorModel colorModel = PlayerBase.TextureColorModel.RGB;

        public Texture2D[] sourceTextures = new Texture2D[0];

        public Texture2D[] uvMaps = new Texture2D[0];

        public Vector2[] discontinuities = new Vector2[0];

        public Texture2D[] vsMaps = new Texture2D[0]; // varisqueeze maps
        public float left; // varisqueeze maps
        public float right;// varisqueeze maps
        public float top; // varisqueeze maps
        public float bottom; // varisqueeze maps
    }
}
