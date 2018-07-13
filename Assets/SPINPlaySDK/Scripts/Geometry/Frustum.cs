using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pixvana.Geometry
{

    [ExecuteInEditMode]
    [RequireComponent (typeof(MeshFilter))]
    [RequireComponent (typeof(MeshRenderer))]
    /// <summary>
    /// Represents a parameterized frustum shape.
    /// </summary>
    public class Frustum : Shape
    {
        
        // Parameter defaults
        private const float defaultFrontRadius      = 0.5f;
        private const float defaultBackRadius       = 1.0f;
        private const float defaultZFront           = 1.0f;
        private const float defaultZBack            = -1.0f;
        private const int defaultTextureWidth       = 1536;
        private const int defaultTextureHeight      = 1024;
        private const int defaultPadding            = 1;
        private const bool defaultTextureInside     = true;
        private const TextureEdge defaultTextureEdge= TextureEdge.None;

        [SerializeField] private float m_FrontRadius = defaultFrontRadius;
        /// <summary>
        /// Gets or sets the radius of the front face.
        /// </summary>
        /// <value>The front radius.</value>
        public float frontRadius { get { return m_FrontRadius; } set { m_FrontRadius = value; UpdateMesh (); } }

        [SerializeField] private float m_BackRadius = defaultBackRadius;
        /// <summary>
        /// Gets or sets the radius of the back face.
        /// </summary>
        /// <value>The back radius.</value>
        public float backRadius { get { return m_BackRadius; } set { m_BackRadius = value; UpdateMesh (); } }

        [SerializeField] private float m_ZFront = defaultZFront;
        /// <summary>
        /// Gets or sets the Z position of the front face.
        /// </summary>
        /// <value>The Z position.</value>
        public float zFront { get { return m_ZFront; } set { m_ZFront = value; UpdateMesh (); } }

        [SerializeField] private float m_ZBack = defaultZBack;
        /// <summary>
        /// Gets or sets the Z position of the back face.
        /// </summary>
        /// <value>The Z position.</value>
        public float zBack { get { return m_ZBack; } set { m_ZBack = value; UpdateMesh (); } }

        [SerializeField] private int m_TextureWidth = defaultTextureWidth;
        /// <summary>
        /// Gets or sets the width of the texture.
        /// </summary>
        /// <value>The width of the texture (in pixels).</value>
        public int textureWidth { get { return m_TextureWidth; } set { m_TextureWidth = value; UpdateMesh (); } }

        [SerializeField] private int m_TextureHeight = defaultTextureHeight;
        /// <summary>
        /// Gets or sets the height of the texture.
        /// </summary>
        /// <value>The height of the texture (in pixels).</value>
        public int textureHeight { get { return m_TextureHeight; } set { m_TextureHeight = value; UpdateMesh (); } }

        [Tooltip("Number of padding pixels around the seam")]
        [SerializeField] private int m_Padding = defaultPadding;
        /// <summary>
        /// Gets or sets the number of padding pixels along the seam.
        /// </summary>
        /// <value>The padding (in pixels).</value>
        public int padding { get { return m_Padding; } set { m_Padding = value; UpdateMesh (); } }

        [Tooltip("Should the texture be on the inside of the frustum?")]
        [SerializeField] private bool m_TextureInside = defaultTextureInside;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Pixvana.Geometry.Frustum"/> should render its texture inside its geometry.
        /// </summary>
        /// <value><c>true</c> if texture should be rendered inside; otherwise, <c>false</c>.</value>
        public override bool textureInside { get { return m_TextureInside; } set { m_TextureInside = value; UpdateMesh (); } }

        [SerializeField]
        private TextureEdge m_TextureEdge = defaultTextureEdge;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Pixvana.Geometry.Frustum"/> has padded texture edges.
        /// </summary>
        /// <value>The texture edge.</value>
        public TextureEdge textureEdge { get { return m_TextureEdge; } set { m_TextureEdge = value; UpdateMesh(); } }

        // Instead of an "isDirty" concept, we need to compare cached values,
        // because the Unity editor directly modifies the private variables
        // instead of the public properties.
        private float m_lastFrontRadius = -1.0f;
        private float m_lastBackRadius = -1.0f;
        private float m_lastZFront = -1.0f;
        private float m_lastZBack = -1.0f;
        private int m_lastTextureWidth = -1;
        private int m_lastTextureHeight = -1;
        private int m_lastPadding = -1;
        private bool m_lastTextureInside = !defaultTextureInside;
        private TextureEdge m_lastTextureEdge = defaultTextureEdge;
        private Mesh m_lastMesh = null;

        private MeshFilter m_MeshFilter = null;

        /// <summary>
        /// Creates a GameObject with frustum geometry.
        /// </summary>
        public new static GameObject Create()
        {
            GameObject gameObject = new GameObject ("Frustum");
            gameObject.AddComponent<MeshFilter> ();
            gameObject.AddComponent<MeshRenderer> ();
            Frustum frustum = gameObject.AddComponent<Frustum> ();
            frustum.UpdateMesh ();

            return gameObject;
        }

        void Awake ()
        {
            // Cache our MeshFilter
            m_MeshFilter = GetComponent<MeshFilter> ();
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

        void UpdateMesh ()
        {

            // Anything to update?
            if ((m_FrontRadius == m_lastFrontRadius) &&
                (m_BackRadius == m_lastBackRadius) &&
                (m_ZFront == m_lastZFront) &&
                (m_ZBack == m_lastZBack) &&
                (m_TextureWidth == m_lastTextureWidth) &&
                (m_TextureHeight == m_lastTextureHeight) &&
                (m_Padding == m_lastPadding) &&
                (m_TextureInside == m_lastTextureInside) &&
                (m_TextureEdge == m_lastTextureEdge) &&
                (m_MeshFilter.sharedMesh == m_lastMesh)) {

                return;
            }

            //Debug.Log("Regenerating frustum");

            // Be careful about how we access the mesh property, or we'll end up
            // creating extra mesh instances that could leak.
            Mesh mesh = null;
            if (m_MeshFilter.sharedMesh == null ||
                m_MeshFilter.sharedMesh != m_lastMesh) {

                mesh = new Mesh ();
                mesh.name = "Frustum";
                m_MeshFilter.mesh = mesh;

            } else {

                mesh = m_MeshFilter.sharedMesh;
            }
            mesh.Clear ();

            // Limit values
            m_FrontRadius = Mathf.Max (0.0f, m_FrontRadius);
            m_BackRadius = Mathf.Max (0.0f, m_BackRadius);
            float maxZFront = Mathf.Max (m_ZBack, m_ZFront);
            float minZBack = Mathf.Min (m_ZFront, m_ZBack);
            m_ZFront = maxZFront;
            m_ZBack = minZBack;
            m_TextureWidth = Mathf.Max (1, m_TextureWidth);
            m_TextureHeight = Mathf.Max (1, m_TextureHeight);
            m_Padding = Mathf.Max (0, m_Padding);

            mesh.vertices = Vertices;
            mesh.uv = UV;
            mesh.triangles = Triangles;

            mesh.RecalculateNormals ();

            if (!m_TextureInside) {

                Geometry.Tools.ReverseNormals (mesh);
                Geometry.Tools.FlipUVs (mesh, Tools.Direction.Horizontal);
            }

            mesh.RecalculateBounds ();

            // Cache last-generated values
            m_lastFrontRadius = m_FrontRadius;
            m_lastBackRadius = m_BackRadius;
            m_lastZFront = m_ZFront;
            m_lastZBack = m_ZBack;
            m_lastTextureWidth = m_TextureWidth;
            m_lastTextureHeight = m_TextureHeight;
            m_lastPadding = m_Padding;
            m_lastTextureInside = m_TextureInside;
            m_lastTextureEdge = m_TextureEdge;
            m_lastMesh = mesh;
        }

        Vector3[] Vertices {

            get {

                Vector3[] vertices = new Vector3[24];

                vertices [0] = new Vector3 (-frontRadius, -frontRadius, zFront);
                vertices [1] = new Vector3 (-frontRadius, frontRadius, zFront);
                vertices [2] = new Vector3 (frontRadius, frontRadius, zFront);
                vertices [3] = new Vector3 (frontRadius, -frontRadius, zFront);
                vertices [4] = new Vector3 (-backRadius, backRadius, zBack);
                vertices [5] = new Vector3 (backRadius, -backRadius, zBack);
                vertices [6] = new Vector3 (backRadius, backRadius, zBack);
                vertices [7] = new Vector3 (-backRadius, -backRadius, zBack);
                vertices [8] = new Vector3 (backRadius, backRadius, zBack);
                vertices [9] = new Vector3 (backRadius, -backRadius, zBack);
                vertices [10] = new Vector3 (frontRadius, -frontRadius, zFront);
                vertices [11] = new Vector3 (frontRadius, frontRadius, zFront);
                vertices [12] = new Vector3 (-backRadius, -backRadius, zBack);
                vertices [13] = new Vector3 (-backRadius, backRadius, zBack);
                vertices [14] = new Vector3 (-frontRadius, frontRadius, zFront);
                vertices [15] = new Vector3 (-frontRadius, -frontRadius, zFront);
                vertices [16] = new Vector3 (-backRadius, backRadius, zBack);
                vertices [17] = new Vector3 (frontRadius, frontRadius, zFront);
                vertices [18] = new Vector3 (-frontRadius, frontRadius, zFront);
                vertices [19] = new Vector3 (backRadius, backRadius, zBack);
                vertices [20] = new Vector3 (-frontRadius, -frontRadius, zFront);
                vertices [21] = new Vector3 (frontRadius, -frontRadius, zFront);
                vertices [22] = new Vector3 (backRadius, -backRadius, zBack);
                vertices [23] = new Vector3 (-backRadius, -backRadius, zBack);

                return vertices;
            }
        }

        Vector2[] UV {

            get {

                // Common
                const float zero = 0.0f;
                const float oneThird = (1.0f / 3.0f);
                const float oneHalf = (1.0f / 2.0f);
                const float twoThirds = (2.0f / 3.0f);
                const float one = 1.0f;

                // Padding from pixels
    			float hPadding = (float)m_Padding / (float)m_TextureWidth;
                float vPadding = (float)m_Padding / (float)m_TextureHeight;

                // Accommodate padding in center of frame and at possible edges
                float oneHalfPlus = oneHalf + vPadding;
                float oneHalfMinus = oneHalf - vPadding;
                float zeroXPlus = zero + ((m_TextureEdge & TextureEdge.Left) != 0 ? hPadding : 0.0f);
                float oneXMinus = one - ((m_TextureEdge & TextureEdge.Right) != 0 ? hPadding : 0.0f);
                float zeroYPlus = zero + ((m_TextureEdge & TextureEdge.Bottom) != 0 ? vPadding : 0.0f);
                float oneYMinus = one - ((m_TextureEdge & TextureEdge.Top) != 0 ? vPadding : 0.0f);

                Vector2[] uv = new Vector2[24];

                uv [0] = new Vector2 (oneThird, oneHalfPlus);
                uv [1] = new Vector2 (oneThird, oneYMinus);
                uv [2] = new Vector2 (twoThirds, oneYMinus);
                uv [3] = new Vector2 (twoThirds, oneHalfPlus);
                uv [4] = new Vector2 (twoThirds, zeroYPlus);
                uv [5] = new Vector2 (oneThird, oneHalfMinus);
                uv [6] = new Vector2 (twoThirds, oneHalfMinus);
                uv [7] = new Vector2 (oneThird, zeroYPlus);
                uv [8] = new Vector2 (oneXMinus, oneYMinus);
                uv [9] = new Vector2 (oneXMinus, oneHalfPlus);
                uv [10] = new Vector2 (twoThirds, oneHalfPlus);
                uv [11] = new Vector2 (twoThirds, oneYMinus);
                uv [12] = new Vector2 (zeroXPlus, oneHalfPlus);
                uv [13] = new Vector2 (zeroXPlus, oneYMinus);
                uv [14] = new Vector2 (oneThird, oneYMinus);
                uv [15] = new Vector2 (oneThird, oneHalfPlus);
                uv [16] = new Vector2 (twoThirds, zeroYPlus);
                uv [17] = new Vector2 (oneXMinus, oneHalfMinus);
                uv [18] = new Vector2 (oneXMinus, zeroYPlus);
                uv [19] = new Vector2 (twoThirds, oneHalfMinus);
                uv [20] = new Vector2 (zeroXPlus, zeroYPlus);
                uv [21] = new Vector2 (zeroXPlus, oneHalfMinus);
                uv [22] = new Vector2 (oneThird, oneHalfMinus);
                uv [23] = new Vector2 (oneThird, zeroYPlus);

                return uv;
            }
        }

        int[] Triangles {

            get {

                int[] triangles = new int[36];

                // Front1
                triangles [0] = 0;
                triangles [1] = 1;
                triangles [2] = 2;

                // Front2
                triangles [3] = 0;
                triangles [4] = 2;
                triangles [5] = 3;

                // Top1
                triangles [6] = 4;
                triangles [7] = 5;
                triangles [8] = 6;

                // Top2
                triangles [9] = 4;
                triangles [10] = 7;
                triangles [11] = 5;

                // Back1
                triangles [12] = 8;
                triangles [13] = 9;
                triangles [14] = 10;

                // Back2
                triangles [15] = 10;
                triangles [16] = 11;
                triangles [17] = 8;

                // Bottom1
                triangles [18] = 12;
                triangles [19] = 13;
                triangles [20] = 14;

                // Bottom2
                triangles [21] = 14;
                triangles [22] = 15;
                triangles [23] = 12;

                // Left1
                triangles [24] = 16;
                triangles [25] = 17;
                triangles [26] = 18;

                // Left2
                triangles [27] = 16;
                triangles [28] = 19;
                triangles [29] = 17;

                // Right1
                triangles [30] = 20;
                triangles [31] = 21;
                triangles [32] = 22;

                // Right2
                triangles [33] = 22;
                triangles [34] = 23;
                triangles [35] = 20;

                return triangles;
            }
        }
    }
}