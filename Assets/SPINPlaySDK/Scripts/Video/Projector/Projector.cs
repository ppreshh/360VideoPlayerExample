using Pixvana.Geometry;
using Pixvana.Opf;
using Pixvana.VR;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Pixvana.Video
{

    [AddComponentMenu("SPIN Play SDK/Projector", 1)]
    [DisallowMultipleComponent]
    /// <summary>
    /// A class that projects OPF video using a <see cref="Player"/>.
    /// </summary>
    public partial class Projector : MonoBehaviour
    {

        /// <summary>
        /// Occurs when asyncronous errors are detected in the Projector and all
        /// objects that are owned by this Projector.
        /// </summary>
        public event EventHandler<Pixvana.ErrorEventArgs> onError;

        private const int unityDefaultLayerIndex            = 0;                            // index for the Unity "default" layer
        private const float defaultRenderScale              = 1.0f;                         // default render target scale
        private const string defaultMaterialName            = "Materials/DefaultMaterial";  // default material for projection
        private const string yuvMaterialName                = "Materials/YUVNV12";
        private const string uvMapYUVMaterialName           = "Materials/UVMap-YUVNV12";
        private const string uvMapRGBMaterialName           = "Materials/UVMap-RGB";
        private const string uvMapYUVDiscontMaterialName    = "Materials/UVMap-YUVNV12-Discont";
        private const string uvMapRGBDiscontMaterialName    = "Materials/UVMap-RGB-Discont";
        private const string varisqueezeMaterialName        = "Materials/VariSqueeze";
        private const float defaultMonoProjectionScale      = 200.0f;                       // default limit of detectable stereo depth distance (scaling meters)
        private const float defaultStereoProjectionScale    = defaultMonoProjectionScale;   // default projection scale for stereo (in meters)
        private const string mainTexturePropertyName        = "_MainTex";                   // Name of the main texture property

        private enum Eye
        {
            Center,
            Left,
            Right,
            Max_Eye
        }

        [SerializeField] private CameraRig m_CameraRig = null;
        /// <summary>
        /// Gets or sets the camera rig.
        /// </summary>
        /// <value>The camera rig.</value>
        public CameraRig cameraRig { get { return m_CameraRig; } set { m_CameraRig = value; } }

        [SerializeField] private Hmd m_Hmd = null;
        /// <summary>
        /// Gets or sets the HMD.
        /// </summary>
        /// <value>The HMD.</value>
        public Hmd hmd { get { return m_Hmd; } set { UpdateHmd (value); } }

        [SerializeField]  private PlayerBase m_Player = null;
        /// <summary>
        /// Gets or sets the player.
        /// </summary>
        /// <value>The player.</value>
        public PlayerBase player { get { return m_Player; } set { UpdatePlayer (value); } }

        [Tooltip("The URL of the OPF source")]
        [SerializeField] private string m_SourceUrl = null;
        /// <summary>
        /// Gets or sets the OPF source URL.
        /// </summary>
        /// <value>The source URL.</value>
        public string sourceUrl {

            get { return m_SourceUrl; } 
            set {

                Assert.IsTrue (m_Player.readyState == PlayerBase.ReadyState.Idle, "Player must be in idle state to set the source URL");
                m_SourceText = null;    // sourceUrl and sourceText are mutually exclusive
                m_SourceUrl = value; 
                m_Projection = null;
            } 
        }

        private string m_SourceText = null;
        /// <summary>
        /// Gets or sets the OPF source text.
        /// </summary>
        /// <value>The OPF source text.</value>
        public string sourceText {

            get { return m_SourceText; }
            set {

                Assert.IsTrue (m_Player.readyState == PlayerBase.ReadyState.Idle, "Player must be in idle state to set the source text");
                m_SourceUrl = null;     // sourceUrl and sourceText are mutually exclusive
                m_SourceText = value; 
                m_Projection = null;
            } 
        }

        [SerializeField] private float m_RenderScale = defaultRenderScale;
        /// <summary>
        /// Gets or sets the VR render scale.
        /// </summary>
        /// <value>The render scale.</value>
        public float renderScale { get { return m_RenderScale; } set { m_RenderScale = value; UpdateRenderScale (); } }

        [SerializeField] private float m_MonoProjectionScale = defaultMonoProjectionScale;
        /// <summary>
        /// Gets or sets the projection scale for monoscopic playback.
        /// </summary>
        /// <value>The projection scale.</value>
        public float monoProjectionScale { get { return m_MonoProjectionScale; } set { m_MonoProjectionScale = value; UpdateProjectionScale (m_Projection, m_Geometry); } }

        [SerializeField] private float m_StereoProjectionScale = defaultStereoProjectionScale;
        /// <summary>
        /// Gets or sets the projection scale for stereo playback.
        /// </summary>
        /// <value>The projection scale.</value>
        public float stereoProjectionScale { get { return m_StereoProjectionScale; } set { m_StereoProjectionScale = value; UpdateProjectionScale(m_Projection, m_Geometry); } }

        [Tooltip("Should stereo content be played monoscopically?")]
        [SerializeField] private bool m_ForceMonoscopic = false;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Pixvana.Video.Projector"/> should force monoscopic playback.
        /// </summary>
        /// <value><c>true</c> if forcing monoscopic; otherwise, <c>false</c>.</value>
        public bool forceMonoscopic {

            get { return m_ForceMonoscopic; } 

            set {

                // Has the value actually changed?
                if (value != m_ForceMonoscopic) {

                    m_ForceMonoscopic = value; 
                    UpdateStereoMode (m_Projection, m_Geometry); 
                    RaiseForceMonoscopicChanged (m_ForceMonoscopic);
                }
            } 
        }

        [Tooltip("Should the forward orientation be reset during Prepare?")]
        [SerializeField] private bool m_ResetForwardOrientation = false;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Pixvana.Video.Projector"/> should reset its "forward" orientation during Prepare.
        /// </summary>
        /// <value><c>true</c> if resetting forward orientation; otherwise, <c>false</c>.</value>
        public bool resetForwardOrientation { get { return m_ResetForwardOrientation; } set { m_ResetForwardOrientation = value; } }

        [SerializeField] private Material m_ProjectionMaterial = null;
        /// <summary>
        /// Gets or sets the projection material.
        /// </summary>
        /// <value>The projection material.</value>
        public Material projectionMaterial { get { return m_ProjectionMaterial; } set { m_ProjectionMaterial = value; UpdateProjectionMaterial (); } }

        private Material m_YuvMaterial = null;
        private Material m_UvMapYUVMaterial = null;
        private Material m_UvMapRGBMaterial = null;
        private Material m_UvMapYUVDiscontMaterial = null;
        private Material m_UvMapRGBDiscontMaterial = null;
        private Material m_VarisqueezeMaterial = null;

        private Projection m_Projection = null;
        /// <summary>
        /// Gets the projection.
        /// </summary>
        /// <value>The projection.</value>
        public Projection projection { get { return m_Projection; } }

        private bool m_Visible = false;
        /// <summary>
        /// Gets or sets a value indicating whether the projection is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        public bool visible { get { return m_Visible; } set { m_Visible = value; UpdateVisibility (); } }

        private bool m_ForUnitTesting = false;
        /// <summary>
        /// Gets or sets a value indicating whether the projector is being used for unit testing.
        /// </summary>
        /// <value><c>true</c> if unit testing; otherwise, <c>false</c>.</value>
        public bool forUnitTesting { get { return m_ForUnitTesting; } set { m_ForUnitTesting = value; } }

        private ShaderSelector m_shaderSelector = new ShaderSelector();

        // Can the projector currently be prepared?
        private bool CanPrepare {

            get { 

                return (
                    !m_IsPreparing &&
                    m_Player != null &&
                    m_Player.readyState == PlayerBase.ReadyState.Idle &&
                    (!string.IsNullOrEmpty (m_SourceUrl) || (!string.IsNullOrEmpty(m_SourceText)))
                );
            }
        }

        /// <summary>
        /// Occurs when the projector has been prepared.
        /// </summary>
        public event EventHandler<EventArgs> onPrepared;

        /// <summary>
        /// Occurs when force monoscopic mode has changed.
        /// </summary>
        public event EventHandler<ForceMonoscopicChangedEventArgs> onForceMonoscopicChanged;

        private bool m_IsPreparing = false;                             // Track preparing state (do we need more robust states?)
        private List<GameObject> m_Geometry = new List<GameObject>();   // Geometry model(s) to project onto
        private float m_HmdYawOffset = 0.0f;                            // Yaw offset for the HMD during Prepare
        private Quaternion m_HeadingOffset = Quaternion.identity; 
        private bool m_AutoProject = false;
        private bool m_WasPlaying = false;                              // Was the player playing when the HMD was unmounted?

        #region Unity Events

        void Reset()
        {
            // Do we have an attached camera rig?
            if (m_CameraRig == null) {

                m_CameraRig = GetComponent<CameraRig> ();
            }

            // Do we have an attached HMD?
            if (m_Hmd == null) {

                m_Hmd = GetComponent<Hmd> ();
            }

            // Do we have an attached Player?
            if (m_Player == null) {

                m_Player = GetComponent<PlayerBase> ();
            }

            // Assign the default projection material
            EnsureProjectionMaterial ();
        }

        void Awake ()
        {

            // Set render scale
            UpdateRenderScale ();

            // Subscribe to HMD events
            UpdateHmd (m_Hmd);

            // Subscribe to serialized player events
            UpdatePlayer (m_Player);

            // Make sure we have a projection material
            EnsureProjectionMaterial ();

            // If we have a serialized OPF URL or text string, set to auto-project
            m_AutoProject = this.CanPrepare;
        }

        void Start ()
        {

            // Hide geometry
            this.visible = false;

            if (m_AutoProject) {

                Prepare ();
            }
        }

        void Update ()
        {
            UpdateHeading ();
            UpdateAudioOrientation ();
        }

        private void UpdateAudioOrientation () 
        {
            if (m_Player != null &&
                m_Projection != null) {

                // Can't use m_headingOffset, because it includes a shape offset that is only meant to reorient visuals, not audio
                Quaternion audioHeadingOffset = Quaternion.Euler(0.0f, m_HmdYawOffset, 0.0f) * projection.heading;

                m_Player.SetAudioOrientation (m_Hmd.GetHeading(), audioHeadingOffset);
            }
        }

        #endregion

        // TODO, questions about the material should move to the Projection
        private void EnsureProjectionMaterial () {

            // Do we already have a projection material
            if (m_ProjectionMaterial == null) {
                m_ProjectionMaterial = Resources.Load<Material> (defaultMaterialName);
                Debug.Assert (m_ProjectionMaterial != null, "Missing projection material");
            }

            if (m_YuvMaterial == null) {
                m_YuvMaterial = Resources.Load<Material>(yuvMaterialName);
                Debug.Assert(m_YuvMaterial != null, "Missing YUV projection material");
            }

            if (m_UvMapYUVMaterial == null) {
                m_UvMapYUVMaterial = Resources.Load<Material>(uvMapYUVMaterialName);
                Debug.Assert(m_UvMapYUVMaterial != null, "Missing UvMap YUV projection material");
            }

            if (m_UvMapRGBMaterial == null) {
                m_UvMapRGBMaterial = Resources.Load<Material>(uvMapRGBMaterialName);
                Debug.Assert(m_UvMapRGBMaterial != null, "Missing UvMap YUV projection material");
            }

            if (m_UvMapYUVDiscontMaterial == null)
            {
                m_UvMapYUVDiscontMaterial = Resources.Load<Material>(uvMapYUVDiscontMaterialName);
                Debug.Assert(m_UvMapYUVDiscontMaterial != null, "Missing UvMap YUV Discontinuities projection material");
            }

            if (m_UvMapRGBDiscontMaterial == null)
            {
                m_UvMapRGBDiscontMaterial = Resources.Load<Material>(uvMapRGBDiscontMaterialName);
                Debug.Assert(m_UvMapRGBDiscontMaterial != null, "Missing UvMap RGB Discontinuities projection material");
            }

            if (m_VarisqueezeMaterial == null)
            {
                m_VarisqueezeMaterial = Resources.Load<Material>(varisqueezeMaterialName);
                Debug.Assert(m_VarisqueezeMaterial != null, "Missing VariSqueeze projection material");
            }

        }

        private void UpdatePlayer(PlayerBase player) {

            if (m_Player != null) {

                m_Player.onReadyStateChanged -= OnReadyStateChanged;
                m_Player.onTileChanged -= OnTileChanged;
                m_Player.onQualityGroupChanged -= OnQualityGroupChanged;
                m_Player.onReset -= OnReset;
                m_Player.onError -= OnPlayerError;
            }

            m_Player = player;

            if (m_Player != null) {

                m_Player.onReadyStateChanged += OnReadyStateChanged;
                m_Player.onTileChanged += OnTileChanged;
                m_Player.onQualityGroupChanged += OnQualityGroupChanged;
                m_Player.onReset += OnReset;
                m_Player.onError += OnPlayerError;
            }
        }

        private void UpdateHmd(Hmd hmd) {

            if (m_Hmd != null) {

                m_Hmd.onHmdMounted -= OnHmdMounted;
                m_Hmd.onHmdUnmounted -= OnHmdUnmounted;
            }

            m_Hmd = hmd;

            if (m_Hmd != null) {

                m_Hmd.onHmdMounted += OnHmdMounted;
                m_Hmd.onHmdUnmounted += OnHmdUnmounted;
            }
        }

        /// <summary>
        /// Prepares the projector for playback.
        /// </summary>
        public void Prepare () {

            // Validate state before projecting
            Assert.IsTrue (!m_IsPreparing, "Projector is already being prepared");
            Assert.IsNotNull (m_CameraRig, "Camera rig is required");
            Assert.IsNotNull (m_Hmd, "Hmd is required");
            Assert.IsNotNull (m_Player, "Player is required");
            Assert.IsTrue (m_Player.readyState == PlayerBase.ReadyState.Idle, "Player must be in idle state to prepare");
            Assert.IsTrue (!string.IsNullOrEmpty(m_SourceUrl) || !string.IsNullOrEmpty(m_SourceText), "Source url or text is required");

            if (this.CanPrepare) {

                m_IsPreparing = true;

                // Make sure that our camera rig is enabled
                m_CameraRig.enabled = true;

                // Hide geometry during prepare
                this.visible = false;

                // Offset/reset our orientation based on where the user is currently looking.
                m_HmdYawOffset = (m_ResetForwardOrientation ? m_Hmd.GetHeading().y : 0.0f);

                // Reset "was playing" status
                m_WasPlaying = false;

                // Get the OPF data
                if (!string.IsNullOrEmpty (m_SourceUrl)) {

                    // Load from URL
                    StartCoroutine (GetOPFData (m_SourceUrl));

                } else if (!string.IsNullOrEmpty (m_SourceText)) {

                    // Load from string
                    LoadOPFFromJSON (m_SourceText);
                }
            }
        }

        private void UpdateRenderScale () {

            // NOTE: For HoloLens/WindowsMR in Windows Store Apps, setting renderScale to anything other than 1.0 causes
            //       distorted playback. Ignoring for now.
#if !UNITY_WSA_10_0
#if UNITY_2017_2_OR_NEWER
            UnityEngine.XR.XRSettings.eyeTextureResolutionScale = m_RenderScale;
#else
            UnityEngine.VR.VRSettings.renderScale = m_RenderScale;
#endif
#endif
        }

        private void UpdateProjectionScale (Projection proj, List<GameObject> geometry) {

            // For mono, push geometry beyond typical stereoscopic acuity (which is a debatable distance).
            // NOTE: OPF models are always in unit space (-1.0 to 1.0). Since Unity distances are in meters,
            //       we use the local scale to effectively set the distance from the camera to the viewer.
            float scale = (proj.stereoMode == Projection.StereoMode.Mono || m_ForceMonoscopic ? m_MonoProjectionScale : m_StereoProjectionScale);
            Vector3 localScale = new Vector3 (scale, scale, scale);
            geometry.ForEach ((gameObject) => { gameObject.transform.localScale = localScale; });
        }

        private void UpdateProjectionMaterial () {

            if (m_Geometry != null) {

                m_Geometry.ForEach((gameObject) =>
                {
                    // NOTE: We may need to save/restore offsets and scales after resetting a material
                    SetProjectionMaterial(gameObject, m_shaderSelector);
                });
            }
        }

        private void SetShaderResources(Material mat, ShaderSelector ss)
        {
            mat.mainTexture = ss.sourceTextures[0];
            switch (ss.colorModel)
            {
                case PlayerBase.TextureColorModel.RGB:
                    break;
                case PlayerBase.TextureColorModel.YUV:
                    mat.SetTexture("_Y", ss.sourceTextures[0]);
                    mat.SetTexture("_UV", ss.sourceTextures[1]);
                    break;
                default:
                    Debug.LogWarning("unknown texture model: " + ss.colorModel);
                    break;
            }
            if (ss.uvMaps.Length > 0)
            {
                mat.SetTexture("_ToFormat", ss.uvMaps[0]);        // from whatever projection the raw video frames are in, to the format declared in the opf (which is usually equirect)
            }
            if (ss.discontinuities.Length > 1)
            {
                mat.SetTexture("_FromFormat", ss.uvMaps[1]);
            }
            // VariSqueeze support
            if (ss.vsMaps.Length > 0)
            {
                Debug.Log("ss");
                mat.SetTexture("_UVXTex", ss.vsMaps[0]);        // from whatever projection the raw video frames are in, to the format declared in the opf (which is usually equirect)
            
                mat.SetTexture("_UVYTex", ss.vsMaps[1]);

                mat.SetFloat("_Left", ss.left);
                mat.SetFloat("_Right", ss.right);
                mat.SetFloat("_Top", ss.top);
                mat.SetFloat("_Bottom", ss.bottom);
            }
        }

        private void SetProjectionMaterial (GameObject gameObject, ShaderSelector ss)
        {
            m_Player.UpdateShader(ss);

            bool isUvMap = ss.uvMaps.Length > 0;
            bool hasDiscontinuities = ss.discontinuities.Length > 1;
            bool isVsMap = ss.vsMaps.Length > 0;  // VariSqueeze flag

            // choose the shader by selecting the material that has the correct shader
            Material material1 = null;
            Material material2 = null;
            switch (m_Player.textureColorModel)
            {
                case PlayerBase.TextureColorModel.RGB:
                    {
                        if (isUvMap)
                        {
                            material1 = m_UvMapRGBMaterial;
                            if (hasDiscontinuities)
                            {
                                material1 = m_UvMapRGBDiscontMaterial;   // TODO, get a full path of discontinuities going to only run this on the restricted pixels that might be problematic
                                //mat2 = m_uvMapRGBDiscontMaterial;
                            }
                        }
                        else
                        {
                            if (isVsMap)  // VariSqueeze transform?
                            {
                                material1 = m_VarisqueezeMaterial;
                                Debug.Log("m_varisqueezeMaterial");
                            }
                            else
                            {
                                material1 = m_ProjectionMaterial;
                            }
                        }
                        break;
                    }
                case PlayerBase.TextureColorModel.YUV:
                    {
                        if (isUvMap)
                        {
                            material1 = m_UvMapYUVMaterial;
                            if (hasDiscontinuities)
                            {
                                material1 = m_UvMapYUVDiscontMaterial;   // TODO, get a full path of discontinuities going to only run this on the restricted pixels that might be problematic
                                //mat2 = m_uvMapYUVDiscontMaterial;
                            }
                        }
                        else
                             if (isVsMap) // VariSqueeze transform?
                        {   // Note: current VariSqueeze shader does not deal with YUV color space
                            material1 = m_VarisqueezeMaterial;
                            Debug.Log("UV m_varisqueezeMaterial");
                        }
                        else
                        {
                            material1 = m_YuvMaterial;
                        }
                        break;
                    }
            }

            // Copy our materials before we modify them. Otherwise, we'll actually change the
            // underlying Unity .mat file(s). This isn't really a bug, but it is an annoyance.
            // See: https://answers.unity.com/questions/487908/modifying-material-modifies-the-actual-mat-file.html

            Material material1Instance = (material1 != null ? new Material(material1) : null);
            Material material2Instance = (material2 != null ? new Material(material2) : null);

            SetShaderResources(material1Instance, ss);
            if (material2Instance != null) {
                
                SetShaderResources (material2Instance, ss);
            }

            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer> ();
            if (meshRenderer != null) {

                Material[] mats = (material2Instance != null ? 
                    new Material[] { material1Instance, material2Instance } :
                    new Material[] { material1Instance });

                if (m_ForUnitTesting)
                {
                    meshRenderer.sharedMaterials = mats;
                }
                else
                {
                    meshRenderer.materials = mats;
                }
            }
        }

        private void UpdateHeading () {

            // Only bother if we have a projection
            if (m_Projection != null) {

                Tile closestTile = m_Projection.ClosestTileForHeading (m_Hmd.GetHeading (), m_HeadingOffset);
                if (closestTile != null) {

                    m_Player.SetTileID (closestTile.id);
                }
                else
                {
                    Debug.LogError("Unable to find the closest tile");
                }
            }
        }


        private void OnReadyStateChanged(object sender, PlayerBase.ReadyStateChangedEventArgs e)
        {
            Debug.Log ("OnReadyStateChanged: " + e.readyState);

            switch (e.readyState) {

            case PlayerBase.ReadyState.Idle:
                {
                    break;
                }
            case PlayerBase.ReadyState.Preparing:
                {
                    break;
                }
            case PlayerBase.ReadyState.Ready:
                {
                    // The player has finished preparing. It now knows things like the size
                    // of the video frame and the video's duration.
                    UpdateProjection(m_Projection, m_shaderSelector, m_Geometry, true/*signal_prepared*/);

                    // Show geometry
                    this.visible = true;

                    break;
                }
            case PlayerBase.ReadyState.Ended:
                {
                    break;
                }
            case PlayerBase.ReadyState.Error:
                {
                    break;
                }
            default: {

                    Assert.IsTrue (false, "Unknown ready state");
                    break;
                }
            }
        }

        private void OnTileChanged(object sender, PlayerBase.TileChangedEventArgs e)
        {
#if UNITY_EDITOR
            Debug.Log ("OnTileChanged: " + e.tileID);
#endif

            // For v1, we explicitly DO NOT support tile switching while video is not playing.
            if (m_Player.isPlaying &&
                (m_Player.readyState == PlayerBase.ReadyState.Ready)) {

                UpdateGeometryOrientationForTile(m_Projection.TileWithID(e.tileID), m_Geometry);
            }
        }

        void OnQualityGroupChanged(object sender, PlayerBase.QualityGroupChangedEventArgs e)
        {
            Debug.Assert(m_Geometry.Count == 2, "OnQualityGroupChanged: Must have left and right eye geometry");

            if (m_Geometry.Count == 2)
            {

                GameObject leftEyeGeometry = m_Geometry[0];
                GameObject rightEyeGeometry = m_Geometry[1];

                // Calculate per-eye texture size
                // NOTE: We use the reported quality group size, because the player size is always the maximum of all qualities.
                int textureWidth = (m_Projection.stereoMode == Projection.StereoMode.StereoLeftRight ?
                    (e.qualityGroup.videoWidth / 2) :
                    e.qualityGroup.videoWidth);
                int textureHeight = (m_Projection.stereoMode == Projection.StereoMode.StereoTopBottom ?
                    (e.qualityGroup.videoHeight / 2) :
                    e.qualityGroup.videoHeight);

                // Update new frame size
                // NOTE: This is mostly (exclusively?) to update the pixel-accurate padding around frustum frames. This should
                //       be replaced in a future version with resolution independent padding.
                m_Projection.format.UpdateGeometry(leftEyeGeometry, textureWidth, textureHeight);
                m_Projection.format.UpdateGeometry(rightEyeGeometry, textureWidth, textureHeight);

                // in case the media width or height has changed, this changes the scale and offset of the material
                ConfigureMaterial(leftEyeGeometry, Eye.Left, m_Projection);
                ConfigureMaterial(rightEyeGeometry, Eye.Right, m_Projection);
            }
        }

        void OnPlayerError(object sender, PlayerBase.ErrorEventArgs e)
        {
            if (onError != null)
            {
                onError(sender, new Pixvana.ErrorEventArgs(e.message));
            }
        }

        void RaiseError(string message, bool log = true)
        {
            if (log)
            {
                Debug.LogError ("Error: " + message);
            }
            if (onError != null)
            {
                onError(this, new Pixvana.ErrorEventArgs(message));
            }
        }

        void OnReset(object sender, EventArgs e) {

            // The player has been reset, so update current state
            m_IsPreparing = false;
            m_WasPlaying = false;

            ClearGeometry();
        }

        private void OnHmdMounted(object sender, EventArgs e)
        {
            Debug.Log ("OnHmdMounted");

            if (m_WasPlaying && m_Player != null && !m_Player.isPlaying) {

                m_Player.Play ();
            }
        }

        private void OnHmdUnmounted(object sender, EventArgs e)
        {
            Debug.Log ("OnHmdUnmounted");

            // Remember playback state so we can restore when the HMD is remounted
            m_WasPlaying = m_Player.isPlaying;

            if (m_Player != null && m_Player.isPlaying) {

                m_Player.Pause ();
            }
        }

        private IEnumerator GetOPFData (string uri)
        {

            Debug.Assert ((uri != null) && (uri.Length > 0), "uri is required");

            WWW www = new WWW (uri);

            yield return www;

            if (!string.IsNullOrEmpty(www.error))
            {

                RaiseError(www.error + ", uri: \"" + uri + "\"");

                m_IsPreparing = false;

            }
            else
            {

                Debug.Log("Reading: " + uri);

                LoadOPFFromJSON(www.text, uri);

            }
        }

        private void LoadOPFFromJSON(string jsonString, string sourceUri = null) {

            if (string.IsNullOrEmpty (jsonString))
            {
                RaiseError ("jsonString is required");
            }
            else {
                try
                {
                    m_Projection = new Projection(jsonString, (string.IsNullOrEmpty(sourceUri) ? null : new Uri(sourceUri)));

                    Debug.Log(m_Projection);

                    PreparePlayer();
                }
                catch (System.Exception exc)
                {
                    RaiseError(exc.Message, false);
                    throw;
                }
            }
        }

        private void PreparePlayer() {

            Debug.Assert (m_Projection != null, "A projection is required to prepare the player");

            // Set the source url and initial heading
            m_Player.sourceUrl = m_Projection.url.AbsoluteUri;

            // TODO: fix this.
            // UpdateHeading calls ClosestTileForHeading, passing it m_HeadingOffset.
            // But m_HeadingOffset is not set until UpdateProjection has been called
            // which happens in response to RaisePrepared (considerably after now).
            UpdateHeading();
            UpdateAudioOrientation ();

            // Build player configuration
            Dictionary<string, object> configuration = m_Hmd.PlayerConfiguration ();

            // Force frame synchronization if we have more than one tile
            configuration.Add (Player.ForceFrameSyncKey, (m_Projection.Tiles.Count > 1));

#if UNITY_ANDROID && !UNITY_EDITOR
            // On Android, modify player parameters to increase buffering if we're *NOT*
            // using a tiled format.
            if (m_Projection.Tiles.Count < 2) {

                configuration.Add (Player.MaxInitialBitrateKey, 15000000);                  // 15Mbps
                configuration.Add (Player.MinDurationForQualityIncreaseMsKey, 10000);       // 10 seconds
                configuration.Add (Player.MaxDurationForQualityDecreaseMsKey, 25000);       // 25 seconds
                configuration.Add (Player.MinDurationToRetainAfterDiscardMsKey, 25000);     // 25 seconds
                configuration.Add (Player.BandwidthFractionKey, 0.75f);
                configuration.Add (Player.MinBufferMsKey, 15000);                           // 15 seconds
                configuration.Add (Player.MaxBufferMsKey, 30000);                           // 30 seconds
                configuration.Add (Player.BufferForPlaybackMsKey, 2500);                    // 2.5 seconds
                configuration.Add (Player.BufferForPlaybackAfterRebufferMsKey, 5000);       // 5 seconds
            }
#endif

            // yuv is always preferable to RGB as the delivery format out of the player
            // When the player delivers yuv we only need to run the yuv->rgb conversion
            // (and other steps too) on the pixels that are visible in the HMD
            //configuration.Add(Player.PreferYUVBuffersKey, true);

            // Add OPF audio information
            configuration.Add(Player.SpatialChannelsKey, m_Projection.audio.spatialChannels);
            configuration.Add(Player.HeadLockedChannelsKey, m_Projection.audio.headLockedChannels);
            configuration.Add(Player.SpatialFormatKey, Audio.StringForSpatialFormat(m_Projection.audio.spatialFormat));

            m_Projection.videoTransform.UpdateShader(m_shaderSelector, () =>
            {
                m_Player.Prepare(configuration);
            });
        }

        private void UpdateVisibility () {

            // Show/hide projection geometry
            m_Geometry.ForEach ((gameObject) => { gameObject.SetActive (m_Visible); });
        }

        private void ClearGeometry() {

            m_Geometry.ForEach((gameObject) => { gameObject.transform.SetParent(null); Destroy(gameObject); });
            m_Geometry.Clear();
            m_shaderSelector = new ShaderSelector();
        }

        private void UpdateGeometryOrientationForTile(Tile tile, List<GameObject> geometry)
        {
            Debug.Log("UpdateGeometryOrientationForTile: " + tile + ", m_HeadingOffset: " + m_HeadingOffset);
            if (tile != null) {

                Quaternion rotation = (m_HeadingOffset * tile.heading);
                geometry.ForEach((gameObject) => { gameObject.transform.rotation = rotation; });
            }
        }

        private void UpdateProjection(Projection projection, ShaderSelector shaderSelector, List<GameObject> geometry, bool signalPrepared) {

            // Configure the projection background color, but only if it's not fully transparent
            if (projection.backgroundColor.a > 0.0f)
            {
                m_CameraRig.leftEyeCamera.clearFlags = CameraClearFlags.SolidColor;
                m_CameraRig.leftEyeCamera.backgroundColor = projection.backgroundColor;
                m_CameraRig.rightEyeCamera.clearFlags = CameraClearFlags.SolidColor;
                m_CameraRig.rightEyeCamera.backgroundColor = projection.backgroundColor;
            }

            // Calculate per-eye texture size
            //            Debug.Log("UpdateProjection, stereo: " + (proj.stereoMode == Projection.StereoMode.Mono ? "false" : "true") + ", player dimenions: <" + m_Player.videoWidth + ", " + m_Player.videoHeight + ">");
            int textureWidth = (projection.stereoMode == Projection.StereoMode.StereoLeftRight ?
                (m_Player.videoWidth / 2) :
                m_Player.videoWidth);
            int textureHeight = (projection.stereoMode == Projection.StereoMode.StereoTopBottom ?
                (m_Player.videoHeight / 2) :
                m_Player.videoHeight);

            bool isUvMapWithDiscontinuities = shaderSelector.uvMaps.Length > 1;

            // Create left-eye geometry
            // NOTE: Be sure to set the projection material before configuring the geometry, since it also modifies the material.
            GameObject leftEyeGeometry = projection.format.CreateGeometry(textureWidth, textureHeight, projection.stereoMode, true);
            if (isUvMapWithDiscontinuities) {
                Tools.AddIntersectedUVs (leftEyeGeometry.GetComponent<MeshFilter> ().mesh, 0, shaderSelector.discontinuities);
            }
            leftEyeGeometry.SetActive(m_Visible);
            SetProjectionMaterial(leftEyeGeometry, shaderSelector);
            ConfigureMaterial(leftEyeGeometry, Eye.Left, projection);
            geometry.Add(leftEyeGeometry);

            // Create right-eye geometry
            // NOTE: Be sure to set the projection material before configuring the geometry, since it also modifies the material.
            GameObject rightEyeGeometry = projection.format.CreateGeometry(textureWidth, textureHeight, projection.stereoMode, false);
            if (isUvMapWithDiscontinuities) {
                Tools.AddIntersectedUVs (rightEyeGeometry.GetComponent<MeshFilter> ().mesh, 0, shaderSelector.discontinuities);
            }
            rightEyeGeometry.SetActive(m_Visible);
            SetProjectionMaterial(rightEyeGeometry, shaderSelector);
            ConfigureMaterial(rightEyeGeometry, Eye.Right, projection);
            geometry.Add(rightEyeGeometry);

            // Update the culling and projection scale for stereo/mono mode
            UpdateStereoMode(projection, geometry);

            // Cache the heading offset (using the left eye)
            // TODO: this needs to be computed before UpdateProjection
            // because it is used to set the initial tile for PreparePlayer
//            Debug.Log("leftEyeGeometry.GetComponent<Shape>().offset: " + leftEyeGeometry.GetComponent<Shape>().offset);
            m_HeadingOffset = (Quaternion.Euler(0.0f, m_HmdYawOffset, 0.0f) * projection.heading * leftEyeGeometry.GetComponent<Shape>().offset);

            // Set initial geometry orientation
            UpdateGeometryOrientationForTile(projection.TileWithID(m_Player.requestedTileID), geometry);

            // Let the player know about our target geometry
            // NOTE: The player will automatically assign the video texture to each target's MeshRenderer.
            m_Player.targetObjects = geometry.ToArray();

            if (signalPrepared)
            {
                m_IsPreparing = false;

                // The projector is now prepared
                RaisePrepared();
            }

        }

        private void ConfigureMaterial(GameObject geometry, Eye eye, Projection projection) {

            Vector2 textureScale = Vector2.zero;
            Vector2 textureOffset = Vector2.zero;
            bool isVideoOrientation = m_Player.isYDown();
            float width_scale = (float)m_Player.mediaWidth / (float)m_Player.videoWidth;
            float height_scale = (float)m_Player.mediaHeight / (float)m_Player.videoHeight;

            switch (projection.stereoMode) {

            case Projection.StereoMode.Undefined:
            case Projection.StereoMode.Mono:
            case Projection.StereoMode.StereoInterleaved:
                {
                    textureScale = new Vector2 (width_scale, isVideoOrientation ? -height_scale : height_scale);
                    textureOffset = new Vector2 (0.0f, isVideoOrientation ? height_scale : 0.0f);
                    break;
                }
            case Projection.StereoMode.StereoTopBottom:
                {
                    textureScale = new Vector2 (width_scale, isVideoOrientation ? -height_scale * 0.5f : height_scale * 0.5f);
                    textureOffset = (eye == Eye.Left ?
                        new Vector2 (0.0f, height_scale * 0.5f) :
                        new Vector2 (0.0f, isVideoOrientation ? height_scale : 0.0f));
                    break;
                }
            case Projection.StereoMode.StereoLeftRight:
                {
                    textureScale = new Vector2 (width_scale * 0.5f, isVideoOrientation ? -height_scale : height_scale);
                    textureOffset = (eye == Eye.Left ?
                        new Vector2 (0.0f, isVideoOrientation ? height_scale : 0.0f) :
                        new Vector2 (width_scale * 0.5f, isVideoOrientation ? height_scale : 0.0f));
                    break;
                }
            }

            // Update mesh renderer
            MeshRenderer meshRenderer = geometry.GetComponent<MeshRenderer> ();
            if (m_ForUnitTesting)
            {
                meshRenderer.sharedMaterial.SetTextureScale(mainTexturePropertyName, textureScale);
                meshRenderer.sharedMaterial.SetTextureOffset(mainTexturePropertyName, textureOffset);
            }
            else
            {
                meshRenderer.material.SetTextureScale(mainTexturePropertyName, textureScale);
                meshRenderer.material.SetTextureOffset(mainTexturePropertyName, textureOffset);
            }

        }

        private void UpdateStereoMode (Projection proj, List<GameObject> geometry) {

            Debug.Assert (geometry.Count == 2, "UpdateStereoMode: Must have left and right eye geometry");

            if (geometry.Count == 2) {

                GameObject leftEyeGeometry = geometry[0];
                GameObject rightEyeGeometry = geometry[1];

                // Assign camera culling layers
                leftEyeGeometry.layer = (proj.stereoMode == Projection.StereoMode.Mono || m_ForceMonoscopic ?
                    unityDefaultLayerIndex :
                    m_CameraRig.leftEyeLayerIndex);
                rightEyeGeometry.layer = (proj.stereoMode == Projection.StereoMode.Mono || m_ForceMonoscopic ?
                    unityDefaultLayerIndex :
                    m_CameraRig.rightEyeLayerIndex);

                UpdateProjectionScale (proj, geometry);
            }
        }

#region Projector Events

        protected void RaisePrepared() {

            // Invoke the onPrepared event
            if (onPrepared != null) {

                onPrepared (this, EventArgs.Empty);
            }
        }

        protected void RaiseForceMonoscopicChanged(bool forceMonoscopic) {

            // Invoke the onForceMonoscopicChanged event
            if (onForceMonoscopicChanged != null) {

                onForceMonoscopicChanged (this, new ForceMonoscopicChangedEventArgs(forceMonoscopic));
            }
        }

#endregion
    }
}
