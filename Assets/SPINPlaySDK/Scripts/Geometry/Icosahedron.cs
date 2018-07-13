using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pixvana.Geometry
{

    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    /// <summary>
    /// Represents an icosahedron.
    /// </summary>
    public class Icosahedron : Shape
    {

        // Name of the default mesh
        private const string MeshName = "Models/icosahedron_97";

        // Accommodate the "seam" offset of the icosahedron model
        /// <summary>
        /// Gets the offset of this shape to properly orient it to yaw = 0, pitch = 0, roll = 0.
        /// </summary>
        /// <value>The offset.</value>
        public override Quaternion offset { get { return Quaternion.Euler(0.0f, 180.0f, 0.0f); } }

        private MeshFilter m_MeshFilter = null;

        /// <summary>
        /// Creates a GameObject with icosahedron geometry.
        /// </summary>
        public new static GameObject Create()
        {
            GameObject gameObject = new GameObject("Icosahedron");
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();
            Icosahedron icosahedron = gameObject.AddComponent<Icosahedron>();
            icosahedron.UpdateMesh();

            return gameObject;
        }

        void Awake()
        {
            // Cache our MeshFilter
            m_MeshFilter = gameObject.GetComponent<MeshFilter>();
        }

        void Start()
        {
            UpdateMesh();
        }

        private void UpdateMesh()
        {
            if (m_MeshFilter.sharedMesh == null)
            {

                m_MeshFilter.sharedMesh = Resources.Load<Mesh>(MeshName);
            }
        }
    }
}
