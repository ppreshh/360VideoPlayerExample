using System;
using UnityEngine;

/// <summary>
/// Namespace that contains all geometry and related tools.
/// </summary>
namespace Pixvana.Geometry
{

    /// <summary>
    /// The abstract base class for all shapes.
    /// </summary>
    public abstract class Shape : MonoBehaviour {

        [Flags]
        /// <summary>
        /// Texture edge directions.
        /// </summary>
        public enum TextureEdge
        {
            /// <summary>
            /// No edge.
            /// </summary>
            None = 0,
            /// <summary>
            /// Left edge.
            /// </summary>
            Left = (1 << 0),
            /// <summary>
            /// Top edge.
            /// </summary>
            Top = (1 << 1),
            /// <summary>
            /// Right edge.
            /// </summary>
            Right = (1 << 2),
            /// <summary>
            /// Bottom edge.
            /// </summary>
            Bottom = (1 << 3)
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Pixvana.Geometry.Shape"/> should render its texture inside its geometry.
        /// </summary>
        /// <value><c>true</c> if texture should be rendered inside; otherwise, <c>false</c>.</value>
        public virtual bool textureInside { get; set; }

        /// <summary>
        /// Gets the offset of this shape to properly orient it to yaw = 0, pitch = 0, roll = 0.
        /// </summary>
        /// <value>The offset.</value>
        public virtual Quaternion offset { get { return Quaternion.identity; } }

        /// <summary>
        /// Creates a shape instance.
        /// </summary>
        public static GameObject Create() { return null; }
    }
}
