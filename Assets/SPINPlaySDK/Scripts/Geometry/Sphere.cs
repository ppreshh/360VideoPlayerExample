using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pixvana.Geometry
{

    [ExecuteInEditMode]
    [RequireComponent (typeof(MeshFilter))]
    [RequireComponent (typeof(MeshRenderer))]
    /// <summary>
    /// Represents a sphere.
    /// </summary>
    public class Sphere : Shape
    {

        private const float M_PI                    = Mathf.PI;
        private const float M_TAU                   = (Mathf.PI * 2.0f);
        private const float M_PI_2                  = (Mathf.PI / 2.0f);

        // Name of the default mesh
        private const string MeshName               = "Models/Sphere";

        [Header("Source")]

        [Tooltip("Source horizontal field of view (in degrees)")]
        [SerializeField]
        [Range(0.5f, 360.0f)]
        private float m_SourceHorizontalFov = 360.0f;
        /// <summary>
        /// Gets or sets the source horizontal field of view (in degrees).
        /// </summary>
        /// <value>The source horizontal field of view.</value>
        public float sourceHorizontalFov { get { return m_SourceHorizontalFov; } set { m_SourceHorizontalFov = value; UpdateMesh(); } }

        [Tooltip("Source vertical field of view (in degrees)")]
        [SerializeField]
        [Range(0.5f, 180.0f)]
        private float m_SourceVerticalFov = 180.0f;
        /// <summary>
        /// Gets or sets the source vertical field of view (in degrees).
        /// </summary>
        /// <value>The source vertical field of view.</value>
        public float sourceVerticalFov { get { return m_SourceVerticalFov; } set { m_SourceVerticalFov = value; UpdateMesh(); } }

        [Header("Projection")]

        [Tooltip("Clipped horizontal field of view (in degrees)")]
        [SerializeField]
        [Range(0.5f, 360.0f)]
        private float m_ClipHorizontalFov = 360.0f;
        /// <summary>
        /// Gets or sets the clipped horizontal field of view (in degrees).
        /// </summary>
        /// <value>The clipped horizontal field of view.</value>
        public float clipHorizontalFov { get { return m_ClipHorizontalFov; } set { m_ClipHorizontalFov = value; UpdateMesh(); } }

        [Tooltip("Clipped vertical field of view (in degrees)")]
        [SerializeField]
        [Range(0.5f, 180.0f)]
        private float m_ClipVerticalFov = 180.0f;
        /// <summary>
        /// Gets or sets the clipped vertical field of view (in degrees).
        /// </summary>
        /// <value>The clipped vertical field of view.</value>
        public float clipVerticalFov { get { return m_ClipVerticalFov; } set { m_ClipVerticalFov = value; UpdateMesh(); } }

        [Header("Shape")]

        [Tooltip("Number of longitude sections")]
        [SerializeField] private int m_VerticalSlices = 64;
        /// <summary>
        /// Gets or sets the number of vertical (longitudinal) slices.
        /// </summary>
        /// <value>The vertical slices.</value>
        public int verticalSlices { get { return m_VerticalSlices; } set { m_VerticalSlices = value; UpdateMesh (); } }

        [Tooltip("Number of latitude sections")]
        [SerializeField] private int m_HorizontalSlices = 64;
        /// <summary>
        /// Gets or sets the number of horizontal (latitudinal) slices.
        /// </summary>
        /// <value>The horizontal slices.</value>
        public int horizontalSlices { get { return m_HorizontalSlices; } set { m_HorizontalSlices = value; UpdateMesh (); } }

        [Tooltip("Should the texture be on the inside of the sphere?")]
        [SerializeField] private bool m_TextureInside = true;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Pixvana.Geometry.Sphere"/> should render its texture inside its geometry.
        /// </summary>
        /// <value><c>true</c> if texture should be rendered inside; otherwise, <c>false</c>.</value>
        public override bool textureInside { get { return m_TextureInside; } set { m_TextureInside = value; UpdateMesh (); } }

        // Accommodate the "seam" offset of the sphere model
        /// <summary>
        /// Gets the offset of this shape to properly orient it to yaw = 0, pitch = 0, roll = 0.
        /// </summary>
        /// <value>The offset.</value>
        public override Quaternion offset { get { return Quaternion.Euler (0.0f, 90.0f, 0.0f); } }

        // Instead of an "isDirty" concept, we need to compare cached values,
        // because the Unity editor directly modifies the private variables
        // instead of the public properties.
        private float m_lastSourceHorizontalFov = -1.0f;
        private float m_lastSourceVerticalFov = -1.0f;
        private float m_lastClipHorizontalFov = -1.0f;
        private float m_lastClipVerticalFov = -1.0f;
        private int m_lastVerticalSlices = -1;
        private int m_lastHorizontalSlices = -1;
        private bool m_lastTextureInside = false;
        private Mesh m_lastMesh = null;

        private MeshFilter m_MeshFilter = null;

        /// <summary>
        /// Creates a GameObject with sphere geometry.
        /// </summary>
        public new static GameObject Create()
        {
            GameObject gameObject = new GameObject ("Sphere");
            gameObject.AddComponent<MeshFilter> ();
            gameObject.AddComponent<MeshRenderer> ();
            Sphere sphere = gameObject.AddComponent<Sphere> ();
            sphere.UpdateMesh ();

            return gameObject;
        }

        void Awake ()
        {
            // Cache our MeshFilter
            m_MeshFilter = gameObject.GetComponent<MeshFilter> ();
        }

        void Start()
        {
            UpdateMesh ();
        }

#if UNITY_EDITOR
        // No need to update in play mode, since public property access will update
        // the mesh directly.
        void Update ()
        {
            UpdateMesh ();
        }
#endif

        private void UpdateMesh ()
        {

            // Anything to update?
            if ((m_SourceHorizontalFov == m_lastSourceHorizontalFov) &&
                (m_SourceVerticalFov == m_lastSourceVerticalFov) &&
                (m_ClipHorizontalFov == m_lastClipHorizontalFov) &&
                (m_ClipVerticalFov == m_lastClipVerticalFov) &&
                (m_VerticalSlices == m_lastVerticalSlices) &&
                (m_HorizontalSlices == m_lastHorizontalSlices) &&
                (m_TextureInside == m_lastTextureInside) &&
                (m_MeshFilter.sharedMesh == m_lastMesh)) {

                return;
            }

            //Debug.Log ("Regenerating sphere");

            // Be careful about how we access the mesh property, or we'll end up
            // creating extra mesh instances that could leak.
            Mesh mesh = null;
            if (m_MeshFilter.sharedMesh == null ||
                 m_MeshFilter.sharedMesh != m_lastMesh) {

                mesh = new Mesh ();
                mesh.name = "Sphere";
                m_MeshFilter.mesh = mesh;

            } else {

                mesh = m_MeshFilter.sharedMesh;
            }
            mesh.Clear ();

            // Limit values
            m_SourceHorizontalFov = Mathf.Clamp(m_SourceHorizontalFov, 0.5f, 360.0f);
            m_SourceVerticalFov = Mathf.Clamp(m_SourceVerticalFov, 0.5f, 180.0f);
            m_ClipHorizontalFov = Mathf.Clamp (m_ClipHorizontalFov, 0.5f, 360.0f);
            m_ClipVerticalFov = Mathf.Clamp (m_ClipVerticalFov, 0.5f, 180.0f);
            m_VerticalSlices = Mathf.Max (3, m_VerticalSlices);
            m_HorizontalSlices = Mathf.Max (3, m_HorizontalSlices);

            #region Vertices
            Vector3[] vertices = new Vector3[(m_VerticalSlices + 1) * (m_HorizontalSlices + 1)];

            float verticalScale = m_ClipVerticalFov / 180.0f;
            float verticalOffset = (1.0f - verticalScale) / 2.0f;

            float horizontalScale = m_ClipHorizontalFov / 360.0f;
            float horizontalOffset = (1.0f - horizontalScale) / 2.0f;

            for (int y = 0; y <= m_HorizontalSlices; y++)
            {
                float angle1 = ((M_PI * ((float)y / m_HorizontalSlices)) * verticalScale) + (verticalOffset * M_PI);
                float sin1 = Mathf.Sin (angle1);
                float cos1 = Mathf.Cos (angle1);

                for (int x = 0; x <= m_VerticalSlices; x++)
                {
                    float angle2 = ((M_TAU * ((float)x / m_VerticalSlices)) * horizontalScale) + (horizontalOffset * M_TAU);
                    float sin2 = Mathf.Sin (angle2);
                    float cos2 = Mathf.Cos (angle2);

                    vertices [x + (y * (m_VerticalSlices + 1))] = new Vector3 (sin1 * cos2, cos1, sin1 * sin2);
                }
            }
            #endregion

            #region Normals		
            Vector3[] normals = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                normals[i] = -vertices[i].normalized;
            }
            #endregion

            #region UVs
            float uvHorizontalScale = m_ClipHorizontalFov / m_SourceHorizontalFov;
            float uvHorizontalOffset = (1.0f - uvHorizontalScale) / 2.0f;
            float uvVerticalScale = m_ClipVerticalFov / m_SourceVerticalFov;
            float uvVerticalOffset = (1.0f - uvVerticalScale) / 2.0f;

            Vector2[] uvs = new Vector2[vertices.Length];
            for (int y = 0; y <= m_HorizontalSlices; y++)
            {
                for (int x = 0; x <= m_VerticalSlices; x++)
                {
                    Vector2 uv = new Vector2(1.0f - ((float)x / m_VerticalSlices), 1.0f - ((float)y / m_HorizontalSlices));
                    uv.x = (uv.x * uvHorizontalScale) + uvHorizontalOffset;
                    uv.y = (uv.y * uvVerticalScale) + uvVerticalOffset;
                    uvs[x + (y * (m_VerticalSlices + 1))] = uv;
                }
            }
            #endregion

            #region Triangles
            int indexCount = (((m_HorizontalSlices * m_VerticalSlices) * 2) * 3);   // faces * (triangles per face) * (vertices per triangle)
            int[] triangles = new int[indexCount];

            int index = 0;
            for (int y = 0; y < m_HorizontalSlices; y++)
            {
                for (int x = 0; x < m_VerticalSlices; x++)
                {
                    int current = x + (y * (m_VerticalSlices + 1));
                    int next = current + m_VerticalSlices + 1;

                    triangles [index++] = current + 1;
                    triangles [index++] = current;
                    triangles [index++] = next + 1;

                    triangles [index++] = next + 1;
                    triangles [index++] = current;
                    triangles [index++] = next;
                }
            }
            #endregion

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            if (!m_TextureInside) {

                Tools.ReverseNormals (mesh);
                Tools.FlipUVs (mesh, Tools.Direction.Horizontal);
            }

            mesh.RecalculateBounds ();

            // Cache last-generated values
            m_lastSourceHorizontalFov = m_SourceHorizontalFov;
            m_lastSourceVerticalFov = m_SourceVerticalFov;
            m_lastClipHorizontalFov = m_ClipHorizontalFov;
            m_lastClipVerticalFov = m_ClipVerticalFov;
            m_lastVerticalSlices = m_VerticalSlices;
            m_lastHorizontalSlices = m_HorizontalSlices;
            m_lastTextureInside = m_TextureInside;
            m_lastMesh = mesh;
        }
    }
}
