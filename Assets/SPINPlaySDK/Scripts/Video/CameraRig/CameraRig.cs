using Pixvana.VR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

namespace Pixvana.Video
{
    /// <summary>
    /// Controls stereoscopic cameras
    /// </summary>
    public class CameraRig : MonoBehaviour
    {

        private const int defaultDepth                  = 100;              // Default camera depth (-100 thru 100, larger values draw over lower values)
        private const string leftEyeCameraName          = "LeftEyeCamera";  // Name of the left eye camera
        private const string rightEyeCameraName         = "RightEyeCamera"; // Name of the right eye camera
        private const string expectedLeftEyeLayerName   = "Left";           // Expected name of the left eye layer
        private const string expectedRightEyeLayerName  = "Right";          // Expected name of the right eye layer

        #region Properties

        private Camera m_LeftEyeCamera = null;
        /// <summary>
        /// Gets the left eye camera.
        /// </summary>
        /// <value>The left eye camera.</value>
        public Camera leftEyeCamera { get { return m_LeftEyeCamera; } }

        private Camera m_RightEyeCamera = null;
        /// <summary>
        /// Gets the right eye camera.
        /// </summary>
        /// <value>The right eye camera.</value>
        public Camera rightEyeCamera { get { return m_RightEyeCamera; } }

        [Tooltip("A camera with a larger depth is drawn on top of a camera with a smaller depth [ -100, 100 ].")]
        [SerializeField] private int m_Depth = defaultDepth;
        /// <summary>
        /// Gets or sets the camera depth [ -100, 100 ]
        /// </summary>
        /// <value>The camera depth.</value>
        public int depth { get { return m_Depth; } set { m_Depth = value; ConfigureCameras (); } }

        [SerializeField] private int m_LeftEyeLayerIndex = 0;
        /// <summary>
        /// Gets or sets the left eye layer index.
        /// </summary>
        /// <value>The layer index.</value>
        public int leftEyeLayerIndex { get { return m_LeftEyeLayerIndex; } set { m_LeftEyeLayerIndex = value; ConfigureCameras (); } }

        [SerializeField] private int m_RightEyeLayerIndex = 0;
        /// <summary>
        /// Gets or sets the right eye layer index.
        /// </summary>
        /// <value>The layer index.</value>
        public int rightEyeLayerIndex { get { return m_RightEyeLayerIndex; } set { m_RightEyeLayerIndex = value; ConfigureCameras (); } }

        [SerializeField] private Hmd m_Hmd = null;
        /// <summary>
        /// Gets or sets the HMD.
        /// </summary>
        /// <value>The HMD.</value>
        public Hmd hmd { get { return m_Hmd; } set { m_Hmd = value; } }

        [Tooltip("Should the projection be locked to the HMD position?")]
        [SerializeField] private bool m_LockToHmd = true;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Pixvana.Video.CameraRig"/> should lock to the <see cref="Pixvana.VR.Hmd"/>.
        /// </summary>
        /// <value><c>true</c> if locking to hmd; otherwise, <c>false</c>.</value>
        public bool lockToHmd { get { return m_LockToHmd; } set { m_LockToHmd = value; } }

        #endregion

        #region Unity events

        void Reset()
        {
            ValidateCameras();
        }

        void Start ()
        {
            // NOTE: We validate cameras during Start() to ensure that the cameras have been
            //       deserialized before we look for them.
            ValidateCameras();

            UpdateCameras ();
        }

        private void OnEnable()
        {
            UpdateVisibility();
        }

        private void OnDisable()
        {
            UpdateVisibility();
        }

        private void FixedUpdate()
        {
            UpdateCameras ();
        }

        void LateUpdate ()
        {
            if (this.enabled && m_LockToHmd) {

                // Consider InputTracking.disablePositionalTracking.
                // See: https://docs.unity3d.com/ScriptReference/VR.InputTracking-disablePositionalTracking.html
                // NOTE: Might not want to disable all positional tracking depending on the app use case,
                //       so inverting the tracked position for now (so that we're always at 0, 0, 0).
                transform.localPosition = (m_Hmd != null ?
                    -m_Hmd.GetPosition () :
#if UNITY_2017_2_OR_NEWER
                    -UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.CenterEye));
#else
                    -InputTracking.GetLocalPosition (VRNode.CenterEye));
#endif
            }
        }

#endregion

        private void ValidateCameras()
        {
            // Make sure we have valid cameras
            m_LeftEyeCamera = m_LeftEyeCamera ?? ValidateCamera (leftEyeCameraName);
            m_RightEyeCamera = m_RightEyeCamera ?? ValidateCamera (rightEyeCameraName);

            // See if we have expected layer names
            // NOTE: Unity's user-defined layers start at index 8
            if (m_LeftEyeLayerIndex < 8)
            {

                int leftEyeLayerIndex = LayerMask.NameToLayer(expectedLeftEyeLayerName);
                m_LeftEyeLayerIndex = (leftEyeLayerIndex > -1 ? leftEyeLayerIndex : 0);
            }
            Debug.Assert(m_LeftEyeLayerIndex > 7, "Missing left eye layer");

            if (m_RightEyeLayerIndex < 8)
            {

                int rightEyeLayerIndex = LayerMask.NameToLayer(expectedRightEyeLayerName);
                m_RightEyeLayerIndex = (rightEyeLayerIndex > -1 ? rightEyeLayerIndex : 0);
            }
            Debug.Assert(m_RightEyeLayerIndex > 7, "Missing right eye layer");

            ConfigureCameras();

            UpdateVisibility();
            
            // Do we have an attached HMD?
            m_Hmd = m_Hmd ?? FindObjectOfType<Hmd>();
        }

        private Camera ValidateCamera(string cameraName)
        {
            Camera camera = null;

            Transform cameraTransform = transform.Find(cameraName);
            if (cameraTransform != null)
            {
                camera = cameraTransform.gameObject.GetComponent<Camera>();
            }
            else
            {
                // Create camera
                GameObject cameraGameObject = new GameObject(cameraName);
                camera = cameraGameObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32 (15, 15, 15, 255);
                cameraGameObject.transform.SetParent(gameObject.transform);
            }

            return camera;
        }

        private void UpdateVisibility ()
        {
            bool showCamera = this.enabled;

            if (m_LeftEyeCamera != null)
            {
                m_LeftEyeCamera.enabled = showCamera;
            }

            if (m_RightEyeCamera != null)
            {
                m_RightEyeCamera.enabled = showCamera;
            }
        }

        private void ConfigureCameras ()
        {
            if (m_LeftEyeCamera != null)
            {
                // Include everything except the right eye layer
                m_LeftEyeCamera.cullingMask = ~(1 << m_RightEyeLayerIndex);
                m_LeftEyeCamera.stereoTargetEye = StereoTargetEyeMask.Left;
                m_LeftEyeCamera.depth = m_Depth;
            }

            if (m_RightEyeCamera != null)
            {
                // Include everything except the left eye layer
                m_RightEyeCamera.cullingMask = ~(1 << m_LeftEyeLayerIndex);
                m_RightEyeCamera.stereoTargetEye = StereoTargetEyeMask.Right;
                m_RightEyeCamera.depth = m_Depth;
            }
        }

        private void UpdateCameras ()
        {
            if (this.enabled) {
                
                // Left camera
                if (m_LeftEyeCamera != null) {

#if UNITY_2017_2_OR_NEWER
                    m_LeftEyeCamera.transform.localPosition = UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.LeftEye);
                    m_LeftEyeCamera.transform.localRotation = UnityEngine.XR.InputTracking.GetLocalRotation (UnityEngine.XR.XRNode.LeftEye);
#else
                    m_LeftEyeCamera.transform.localPosition = InputTracking.GetLocalPosition (VRNode.LeftEye);
                    m_LeftEyeCamera.transform.localRotation = InputTracking.GetLocalRotation (VRNode.LeftEye);
#endif
                }

                // Right camera
                if (m_RightEyeCamera != null) {

#if UNITY_2017_2_OR_NEWER
                    m_RightEyeCamera.transform.localPosition = UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.RightEye);
                    m_RightEyeCamera.transform.localRotation = UnityEngine.XR.InputTracking.GetLocalRotation (UnityEngine.XR.XRNode.RightEye);
#else
                    m_RightEyeCamera.transform.localPosition = InputTracking.GetLocalPosition (VRNode.RightEye);
                    m_RightEyeCamera.transform.localRotation = InputTracking.GetLocalRotation (VRNode.RightEye);
#endif
                }
            }
        }
    }
}