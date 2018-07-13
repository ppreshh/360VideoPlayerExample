using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Pixvana.Opf
{

    /// <summary>
    /// The abstract base class for all projection formats.
    /// </summary>
    public abstract class VideoTransform
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="Pixvana.Opf.VideoTransform"/> class.
        /// </summary>
        public VideoTransform() { }

        public virtual void UpdateShader(Pixvana.Video.ShaderSelector ss, Action completion) { completion(); }

    }

}
