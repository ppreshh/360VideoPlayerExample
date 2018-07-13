// To avoid modifying this file, these can also be defined under Edit/Project Settings/Player/Other Settings/Scripting Define Symbols
//#define OCULUS_UTILITIES_INSTALLED      // Uncomment if Oculus Utilities are installed
//#define STEAMVR_INSTALLED               // Uncomment if SteamVR is installed

using Pixvana.Video;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

// See: http://elevr.com/cross-platform-vrar-development-in-unity-for-vive-and-hololens/

/// <summary>
/// Namespace that contains VR functionality.
/// </summary>
namespace Pixvana.VR
{
    /// <summary>
    /// HMD types.
    /// </summary>
    public enum HmdType
    {
        /// <summary>
        /// Unknown HMD.
        /// </summary>
        Unknown         = -1,
        /// <summary>
        /// An Oculus Rift.
        /// </summary>
        OculusRift      =  0,
        /// <summary>
        /// A Gear VR.
        /// </summary>
        OculusGearVR    =  1,
        /// <summary>
        /// An Oculus Go.
        /// </summary>
        OculusGo        =  2,
        /// <summary>
        /// An HTC Vive.
        /// </summary>
        HTCVive         =  3,
        /// <summary>
        /// A Microsoft HoloLens.
        /// </summary>
        HoloLens        =  4,
        /// <summary>
        /// A Microsoft Windows Mixed Reality HMD.
        /// </summary>
        WindowsMR       =  5,
        /// <summary>
        /// A Google Daydream HMD.
        /// </summary>
        Daydream        =  6,
        /// <summary>
        /// A Google Cardboard HMD.
        /// </summary>
        Cardboard       =  7,
        Max_HmdType
    }

    /// <summary>
    /// A generalized class for HMD type, position, and orientation.
    /// </summary>
    public class Hmd : MonoBehaviour
    {

        /// <summary>
        /// Gets a value indicating whether this <see cref="Pixvana.VR.Hmd"/> has been built with native integration.
        /// </summary>
        /// <value><c>true</c> if native integration is available; otherwise, <c>false</c>.</value>
        public bool nativeIntegrationAvailable {

            get {

                #if OCULUS_UTILITIES_INSTALLED || STEAMVR_INSTALLED
                return true;
                #else
                return false;
                #endif
            }
        }

        [Tooltip("Should the HMD try to use native integration?")]
        [SerializeField] private bool m_UseNativeIntegration = false;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Pixvana.VR.Hmd"/> should use native integration.
        /// </summary>
        /// <value><c>true</c> to use native integration; otherwise, <c>false</c>.</value>
        public bool useNativeIntegration { get { return m_UseNativeIntegration; } set { m_UseNativeIntegration = value; } }

        private HmdType m_Type = HmdType.Unknown;
        /// <summary>
        /// Gets the detected HMD type.
        /// </summary>
        /// <value>The HMD type.</value>
        public HmdType Type { get { return m_Type; } set { m_Type = value; UpdateHmd(); } }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Pixvana.VR.Hmd"/> supports a native monoscopic mode.
        /// </summary>
        /// <value><c>true</c> if supports monoscopic mode; otherwise, <c>false</c>.</value>
        public bool supportsMonoscopic {

            get {

                bool supportsMonoscopic = false;
                #if OCULUS_UTILITIES_INSTALLED
                if (m_UseNativeIntegration && (m_Type == HmdType.OculusRift || m_Type == HmdType.OculusGearVR || m_Type == HmdType.OculusGo)) {

                supportsMonoscopic = true;
                }
                #endif
                return supportsMonoscopic; 
            } 
        }

        private bool m_Monoscopic = false;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Pixvana.VR.Hmd"/> projects a monoscopic view.
        /// </summary>
        /// <value><c>true</c> if monoscopic; otherwise, <c>false</c>.</value>
        public bool monoscopic {

            get {

                // Always return false if we don't support a native monoscopic mode
                return (this.supportsMonoscopic ? m_Monoscopic : false);
            }

            set {

                // Only bother if we support a native monoscopic mode
                m_Monoscopic = (this.supportsMonoscopic ? value : false);
                UpdateMonoscopic ();
            }
        }

        /// <summary>
        /// Occurs when an HMD is put on the user's head.
        /// </summary>
        public event EventHandler<EventArgs> onHmdMounted;

        /// <summary>
        /// Occurs when an HMD is taken off the user's head.
        /// </summary>
        public event EventHandler<EventArgs> onHmdUnmounted;

        void Awake ()
        {

            // Is VR mode enabled?
#if UNITY_2017_2_OR_NEWER
            if (UnityEngine.XR.XRSettings.enabled)
            {
#else
            if (VRSettings.enabled) 
            {
#endif
                LogDevices();

                DetectHmdType();
                OculusAwake();
                //UpdateMonoscopic ();
            }
        }

        private void UpdateHmd()
        {
            // Is VR mode enabled?
#if UNITY_2017_2_OR_NEWER
            if (UnityEngine.XR.XRSettings.enabled)
            {
#else
            if (VRSettings.enabled) 
            {
#endif
                LogDevices();

                OculusAwake();
            }
        }

        private void LogDevices()
        {
#if UNITY_2017_2_OR_NEWER
            string devices = string.Join(", ", UnityEngine.XR.XRSettings.supportedDevices);
            Debug.Log("Supported VR devices: " + devices);
            Debug.Log("Loaded device: " + UnityEngine.XR.XRSettings.loadedDeviceName);
#else
            string devices = string.Join (", ", VRSettings.supportedDevices);
            Debug.Log ("Supported VR devices: " + devices);
            Debug.Log ("Loaded device: " + VRSettings.loadedDeviceName);
#endif
        }

        private void DetectHmdType() {

            if (SystemInfo.deviceModel == "Oculus Pacific") {

                m_Type = HmdType.OculusGo;

            } else {

#if UNITY_2017_2_OR_NEWER
                switch (UnityEngine.XR.XRSettings.loadedDeviceName)
                {
#else
                switch (VRSettings.loadedDeviceName)
                {
#endif
                    case "Oculus":
                    {
#if UNITY_ANDROID
                        // On Android
                        m_Type = HmdType.OculusGearVR;
#else
                        m_Type = HmdType.OculusRift;
#endif
                        break;
                    }
                case "OpenVR":
                    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_EDITOR_64
                        // Running on Windows
                        m_Type = HmdType.HTCVive;
#endif
                        break;
                    }
                case "HoloLens":
                    {
#if UNITY_WSA_10_0
                        // UWP app
                        m_Type = HmdType.HoloLens;
#endif
                        break;
                    }
                case "WindowsMR":
                    {
#if UNITY_WSA_10_0
                        // UWP app
                        m_Type = HmdType.WindowsMR;
#endif
                        break;
                    }
                case "daydream":
                    {
#if UNITY_ANDROID
                        m_Type = HmdType.Daydream;
#endif
                        break;
                    }
                case "cardboard":
                    {
#if UNITY_ANDROID || UNITY_IPHONE
                        m_Type = HmdType.Cardboard;
#endif
                        break;
                    }
                }
            }

            Debug.Log ("Detected HMD type: " + m_Type.ToString ());
        }

        private void UpdateMonoscopic ()
        {

#if OCULUS_UTILITIES_INSTALLED
            if (m_UseNativeIntegration && (m_Type == HmdType.OculusRift || m_Type == HmdType.OculusGearVR || m_Type == HmdType.OculusGo)) {

            // Enable Oculus native monoscopic mode
            OVRManager.instance.monoscopic = m_Monoscopic;
            }
#endif
        }

        public Dictionary<string, object> PlayerConfiguration () {

            Dictionary<string, object> configuration = new Dictionary<string, object> ();

#if OCULUS_UTILITIES_INSTALLED
            if (m_UseNativeIntegration && m_Type == HmdType.OculusRift) {

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WSA_10_0
            // Add audio output device ID for Windows
            configuration.Add (Player.AudioOutIdKey, OVRManager.audioOutId);
#endif
            }
#endif

            return configuration;
        }

#region Oculus Utilities Functionality

        private void OculusAwake ()
        {

#if OCULUS_UTILITIES_INSTALLED
            if (m_UseNativeIntegration && (m_Type == HmdType.OculusRift || m_Type == HmdType.OculusGearVR || m_Type == HmdType.OculusGo)) {

            OVRManager.HMDMounted += RaiseHmdMounted;
            OVRManager.HMDUnmounted += RaiseHmdUnmounted;
            }
#endif
        }

#endregion

        /// <summary>
        /// Gets the current HMD position.
        /// </summary>
        /// <returns>The position.</returns>
        public Vector3 GetPosition ()
        {
            // For now, get the main camera position
            return (m_Type != HmdType.Unknown ?
#if UNITY_2017_2_OR_NEWER
                UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.CenterEye) :
#else
                InputTracking.GetLocalPosition (VRNode.CenterEye) :
#endif
                //                Camera.main.gameObject.transform.position :
                Vector3.zero);
        }

        /// <summary>
        /// Gets the current HMD heading.
        /// </summary>
        /// <returns>The heading.</returns>
        public Vector3 GetHeading ()
        {
            // For now, use Unity's heading
            return (m_Type != HmdType.Unknown ?
#if UNITY_2017_2_OR_NEWER
                UnityEngine.XR.InputTracking.GetLocalRotation (UnityEngine.XR.XRNode.CenterEye).eulerAngles :
#else
                InputTracking.GetLocalRotation (UnityEngine.VR.VRNode.CenterEye).eulerAngles :
#endif
                Vector3.zero);
        }

#region HMD Events

        protected void RaiseHmdMounted() {

            // Invoke the onHmdMounted event
            if (onHmdMounted != null) {

                onHmdMounted (this, EventArgs.Empty);
            }
        }

        protected void RaiseHmdUnmounted() {

            // Invoke the onHmdUnmounted event
            if (onHmdUnmounted != null) {

                onHmdUnmounted (this, EventArgs.Empty);
            }
        }

#endregion
    }
}