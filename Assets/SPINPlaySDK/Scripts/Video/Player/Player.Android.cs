#if UNITY_ANDROID && !UNITY_EDITOR

using SimpleJSON;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Pixvana.Video
{

    public partial class Player {

        #region Configuration keys

        /// <summary>
        /// A boolean value indicating whether the player should attempt to play formats that exceed reported capabilities
        /// </summary>
        public const string AllowExceedsCapabilitiesKey             = "AllowExceedsCapabilities";

        /// <summary>
        /// An integer value indicating the maximum bitrate (in bits per second) that should be assumed when a bandwidth estimate is unavailable
        /// </summary>
        public const string MaxInitialBitrateKey                    = "MaxInitialBitrate";

        /// <summary>
        /// An integer value indicating the minimum duration of buffered data required for the selected track to switch to one of higher quality
        /// </summary>
        public const string MinDurationForQualityIncreaseMsKey      = "MinDurationForQualityIncreaseMs";

        /// <summary>
        /// An integer value indicating the maximum duration of buffered data required for the selected track to switch to one of lower quality
        /// </summary>
        public const string MaxDurationForQualityDecreaseMsKey      = "MaxDurationForQualityDecreaseMs";

        /// <summary>
        /// An integer value indicating the minimum duration of media that must be retained when switching tiles or quality groups
        /// </summary>
        public const string MinDurationToRetainAfterDiscardMsKey    = "MinDurationToRetainAfterDiscardMs";

        /// <summary>
        /// A float value indicating the fraction of the available bandwidth that the selection should consider available for use
        /// </summary>
        public const string BandwidthFractionKey                    = "BandwidthFraction";

        /// <summary>
        /// An integer value indicating the minimum duration of media that the player will attempt to ensure is buffered at all times
        /// </summary>
        public const string MinBufferMsKey                          = "MinBufferMs";

        /// <summary>
        /// An integer value indicating the maximum duration of media that the player will attempt to buffer
        /// </summary>
        public const string MaxBufferMsKey                          = "MaxBufferMs";

        /// <summary>
        /// An integer value indicating the duration of media that must be buffered for playback to start or resume following a user action such as a seek
        /// </summary>
        public const string BufferForPlaybackMsKey                  = "BufferForPlaybackMs";

        /// <summary>
        /// An integer value indicating the duration of media that must be buffered for playback to resume after a rebuffer
        /// </summary>
        public const string BufferForPlaybackAfterRebufferMsKey     = "BufferForPlaybackAfterRebufferMs";

        #endregion

        // How often should we request the current time?
        private const long CurrentTimeRequestMs         = 250;

        #region AndroidPlayer Java bridge values

        // Boolean
        private const string BoolTrue                   = "TRUE";
        private const string BoolFalse                  = "FALSE";

        // playbackStates
        private const string StateIdle                  = "STATE_IDLE";
        private const string StateBuffering             = "STATE_BUFFERING";
        private const string StateReady                 = "STATE_READY";
        private const string StateEnded                 = "STATE_ENDED";

        #endregion

        private int m_AndroidApiLevel = -1;
        private AndroidJavaObject m_ActivityContext = null;
        private IntPtr m_UnityTexturePtr = IntPtr.Zero;
        IntPtr m_AndroidSurface = (IntPtr)0;
        private bool m_RenderedFirstFrame = false;
        private string m_LastPlaybackState = string.Empty;
        private bool m_PlayerPrepared = false;
        private bool m_PlayerSeeking = false;
        private long m_NextCurrentTimeRequestMs = 0L;
        private string m_PreparingTileId = null;
        private QualityGroup m_PreparingQualityGroup = null;
        private AudioTrack m_PreparingAudioTrack = null;
        private bool m_IsStalled = false;
        private long m_TileUpdateNanoTimeStamp = 0L;
        private string m_LastFormatTileId = null;

        private enum MediaSurfaceEventType
        {
            Initialize          = 0,
            Shutdown            = 1,
            Update              = 2,
            Finish              = 3,
            Max_EventType
        };

        private static void IssuePluginEvent(MediaSurfaceEventType eventType)
        {
            GL.IssuePluginEvent((int)eventType);
        }

        private AndroidJavaObject m_AndroidPlayer = null;
        private AndroidJavaObject androidPlayer
        {
            get {

                if (m_AndroidPlayer == null) {

                    // Initialize an output surface
                    OVR_Media_Surface_Init ();
                    IssuePluginEvent (MediaSurfaceEventType.Initialize);

                    // Grab Unity's Android Activity
                    if (m_ActivityContext == null) {

                        using (AndroidJavaClass activityClass = new AndroidJavaClass ("com.unity3d.player.UnityPlayer")) {

                            if (activityClass != null) {

                                m_ActivityContext = activityClass.GetStatic<AndroidJavaObject> ("currentActivity");

                            } else {

                                Debug.Log ("Couldn't get UnityPlayer activity class");
                            }
                        }
                    }

                    // Initialize and configure the Android player
                    if (m_AndroidPlayer == null) {

                        using (AndroidJavaClass pluginClass = new AndroidJavaClass ("com.pixvana.player.AndroidPlayer")) {

                            if (pluginClass != null) {

                                // Get a reference to the shared player instance
                                m_AndroidPlayer = pluginClass.CallStatic<AndroidJavaObject> ("instance");

                                // Set the Unity Activity as context
                                m_AndroidPlayer.Call ("setContext", m_ActivityContext);

                                // Set the name of the callback GameObject
                                // NOTE: Need to make this configurable.
                                m_AndroidPlayer.Call ("setGameObjectName", gameObject.name);

                            } else {

                                Debug.Log ("Couldn't get AndroidPlayer class");
                            }
                        }
                    }
                }

                return m_AndroidPlayer;
            }
        }

        public void DisposeAndroidPlayer()
        {
            if (m_AndroidPlayer != null) {

                // This cleans-up AndroidPlayer's internal ExoPlayer reference. It does not clean-up
                // the AndroidPlayer instance itself.
                m_AndroidPlayer.Call ("dispose");
            }
        }

        public override CompatiblilityInfo isCompatible {
            
            get {

                // Make sure we're running on Android
                if (Application.platform != RuntimePlatform.Android) {
                    
                    return new CompatiblilityInfo (false, "Player can only run on Android");
                }

                // Make sure we're running a supported API level
                if (m_AndroidApiLevel < 0) {
                    
                    using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION")) {
                        
                        m_AndroidApiLevel = version.GetStatic<int>("SDK_INT");
                    }
                }
                if (m_AndroidApiLevel < 19) {

                    return new CompatiblilityInfo (false, "Player requires Android 4.4 'KitKat' (API level 19) or later");
                }

                return base.isCompatible;
            }
        }

        public override void ResetPlayer()
        {
            m_RenderedFirstFrame = false;
            m_LastPlaybackState = string.Empty;
            m_PlayerPrepared = false;
            m_PlayerSeeking = false;
            m_NextCurrentTimeRequestMs = 0L;
            m_PreparingTileId = null;
            m_PreparingQualityGroup = null;
            m_PreparingAudioTrack = null;
            m_IsStalled = false;
            m_TileUpdateNanoTimeStamp = 0L;
            m_LastFormatTileId = null;

            // Reset the media surface texture reference
            OVR_Media_Surface_ResetTexture ();

            DisposeAndroidPlayer ();

            base.ResetPlayer ();
        }

        public override void Prepare(Dictionary<string, object> configuration)
        {
            base.Prepare (configuration);

            m_PlayerPrepared = false;

            JSONObject configurationObject = new JSONObject ();

            // Any additional configuration parameters?
            if (configuration != null) {

                if (configuration.ContainsKey (PreferredLanguageKey)) {
                    configurationObject.Add (PreferredLanguageKey, new JSONString (((CultureInfo)configuration [PreferredLanguageKey]).TwoLetterISOLanguageName));
                }
                if (configuration.ContainsKey (AllowExceedsCapabilitiesKey)) {
                    configurationObject.Add (AllowExceedsCapabilitiesKey, new JSONBool ((bool)configuration [AllowExceedsCapabilitiesKey]));
                }
                if (configuration.ContainsKey (MaxInitialBitrateKey)) {
                    configurationObject.Add (MaxInitialBitrateKey, new JSONNumber ((int)configuration [MaxInitialBitrateKey]));
                }
                if (configuration.ContainsKey (MinDurationForQualityIncreaseMsKey)) {
                    configurationObject.Add (MinDurationForQualityIncreaseMsKey, new JSONNumber ((int)configuration [MinDurationForQualityIncreaseMsKey]));
                }
                if (configuration.ContainsKey (MaxDurationForQualityDecreaseMsKey)) {
                    configurationObject.Add (MaxDurationForQualityDecreaseMsKey, new JSONNumber ((int)configuration [MaxDurationForQualityDecreaseMsKey]));
                }
                if (configuration.ContainsKey (MinDurationToRetainAfterDiscardMsKey)) {
                    configurationObject.Add (MinDurationToRetainAfterDiscardMsKey, new JSONNumber ((int)configuration [MinDurationToRetainAfterDiscardMsKey]));
                }
                if (configuration.ContainsKey (BandwidthFractionKey)) {
                    configurationObject.Add (BandwidthFractionKey, new JSONNumber ((float)configuration [BandwidthFractionKey]));
                }
                if (configuration.ContainsKey (MinBufferMsKey)) {
                    configurationObject.Add (MinBufferMsKey, new JSONNumber ((int)configuration [MinBufferMsKey]));
                }
                if (configuration.ContainsKey (MaxBufferMsKey)) {
                    configurationObject.Add (MaxBufferMsKey, new JSONNumber ((int)configuration [MaxBufferMsKey]));
                }
                if (configuration.ContainsKey (BufferForPlaybackMsKey)) {
                    configurationObject.Add (BufferForPlaybackMsKey, new JSONNumber ((int)configuration [BufferForPlaybackMsKey]));
                }
                if (configuration.ContainsKey (BufferForPlaybackAfterRebufferMsKey)) {
                    configurationObject.Add (BufferForPlaybackAfterRebufferMsKey, new JSONNumber ((int)configuration [BufferForPlaybackAfterRebufferMsKey]));
                }
                if (configuration.ContainsKey(SpatialChannelsKey)) {
                    configurationObject.Add(SpatialChannelsKey, new JSONNumber((int)configuration[SpatialChannelsKey]));
                }
                if (configuration.ContainsKey(HeadLockedChannelsKey)) {
                    configurationObject.Add(HeadLockedChannelsKey, new JSONNumber((int)configuration[HeadLockedChannelsKey]));
                }
                if (configuration.ContainsKey(SpatialFormatKey)) {
                    configurationObject.Add(SpatialFormatKey, new JSONString((string)configuration[SpatialFormatKey]));
                }

                // Pass configuration to player
                this.androidPlayer.Call("setConfiguration", configurationObject.ToString());
            }

            // Pass configuration to player
            this.androidPlayer.Call("setConfiguration", configurationObject.ToString());

            // Prepare the player
            this.androidPlayer.Call("prepare", this.sourceUrl, 0L, false);
        }

        private void OnDestroy()
        {
            if (m_AndroidSurface != (IntPtr)0) {

                // Shutdown the media surface
                IssuePluginEvent (MediaSurfaceEventType.Shutdown);
            }

            DisposeAndroidPlayer ();
        }

        void Update()
        {
            // Only during playback and after we've rendered the first frame
            if (m_RenderedFirstFrame &&
                this.readyState == ReadyState.Ready) {

                // Update the media surface
                IssuePluginEvent (MediaSurfaceEventType.Update);

                // Waiting for a tile change?
                if (m_TileUpdateNanoTimeStamp > 0L) {

                    // What is the presentation time of the last updated frame?
                    long lastUpdateNanoTimeStamp = OVR_Media_LastUpdateNanoTimeStamp ();

                    // Do we need to switch to the new tile?
                    if (lastUpdateNanoTimeStamp >= m_TileUpdateNanoTimeStamp) {

                        this.currentTileID = m_LastFormatTileId;

                        m_TileUpdateNanoTimeStamp = 0L;
                    }
                }

                // Update the current time (only allowed while we're playing)
                if (this.isPlaying) {

                    // Throttle time update requests...no need to do it at the display frame rate
                    long nowMs = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    if (nowMs > m_NextCurrentTimeRequestMs) {

                        this.currentTime = SecondsFromMilliseconds (this.androidPlayer.Call<long> ("getCurrentPosition"));
                        m_NextCurrentTimeRequestMs = nowMs + CurrentTimeRequestMs;
                    }
                }
            }
        }

        // Time helpers

        private double SecondsFromMilliseconds(long milliseconds)
        {
            return (double)milliseconds / 1000.0;
        }

        private long MillisecondsFromSeconds(double seconds)
        {
            return (long)(seconds * 1000.0);
        }

        // Player controls

        public override void Play()
        {
            base.Play ();

            this.androidPlayer.Call ("play");
        }

        public override void Pause()
        {
            base.Pause ();

            this.androidPlayer.Call ("pause");
        }

        public override void Seek(double time, bool approximate)
        {
            m_PlayerSeeking = true;

            // Make sure time is within bounds
            time = Math.Max(0.0, Math.Min (time, this.duration));

            base.Seek (time, approximate);

            this.androidPlayer.Call ("seekTo", MillisecondsFromSeconds(time));
        }

        public override void SetTileID(string tileID)
        {
            base.SetTileID (tileID);

            this.androidPlayer.Call ("setTileId", tileID);
        }

        public override void SetQualityGroup(QualityGroup qualityGroup)
        {
            base.SetQualityGroup(qualityGroup);

            this.androidPlayer.Call ("setQualityGroupName", qualityGroup.name);
        }

        protected override void OnEnableAutoQuality()
        {
            base.OnEnableAutoQuality ();

            this.androidPlayer.Call ("enableAutoQuality");
        }

        public override void SetAudioOrientation (Vector3 orientation, Quaternion orientationOffset)
        {
            if (m_AndroidPlayer != null) {

                Vector3 eulerOffset = orientationOffset.eulerAngles;
                Quaternion audioOrientation = Quaternion.Euler (orientation.x + eulerOffset.x, 360.0f - (orientation.y - eulerOffset.y), orientation.z + eulerOffset.z);

                // setOrientation's parameter order is w, x, y, z of the quaternion
                this.androidPlayer.Call ("setOrientation", audioOrientation.w, audioOrientation.x, audioOrientation.y, audioOrientation.z);
            }
        }

        public override void SetAudioTrack(AudioTrack audioTrack)
        {
            base.SetAudioTrack(audioTrack);

            this.androidPlayer.Call ("setAudioTrackId", audioTrack.id);
        }

        public void AssignTexture(int width, int height)
        {
            // Generate and assign an output texture
            // We do this to ensure the correct texture format
            Texture2D texture = new Texture2D (width, height, TextureFormat.RGB24, false);
            ClearTexture (texture);
            texture.filterMode = FilterMode.Trilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            this.texture = texture;

            // Get the native output texture pointer
            m_UnityTexturePtr = texture.GetNativeTexturePtr ();
            Debug.Log ("Unity texture id: " + m_UnityTexturePtr);

            OVR_Media_Surface_SetTextureId (m_UnityTexturePtr);
            OVR_Media_Surface_SetTextureParms (width, height);

            // Get a reference to the player's output surface (not the Unity texture)
            // NOTE: This does not create a new player Surface...it will re-use its existing Surface.
            //       Will also reset if the dimensions change, but not if the texture pointer changes. Need to fix.
            m_AndroidSurface = OVR_Media_Surface_GetObject();
            Debug.Log ("Player surface: " + m_AndroidSurface);

            // Set the output surface for the player
            IntPtr setSurfaceMethodId = AndroidJNI.GetMethodID (m_AndroidPlayer.GetRawClass (), "setSurface", "(Landroid/view/Surface;)V");
            jvalue[] parms = new jvalue[1];
            parms [0] = new jvalue ();
            parms [0].l = m_AndroidSurface;
            AndroidJNI.CallVoidMethod (m_AndroidPlayer.GetRawObject (), setSurfaceMethodId, parms);
        }

        private void ClearTexture (Texture2D texture)
        {
            // Reset all pixels
            Color32 resetColor = new Color32 (0, 0, 0, 0);
            Color32[] resetColorArray = texture.GetPixels32 ();

            for (int i = 0; i < resetColorArray.Length; i++) {
                resetColorArray [i] = resetColor;
            }

            texture.SetPixels32 (resetColorArray);
            texture.Apply ();
        }

        // Player events
        public void onPlayerStateChanged(string message)
        {
            // Player state is encoded as "playWhenReady|playbackState"

            string[] arguments = message.Split ('|');
            if (arguments.Length == 2) {

                bool playWhenReady = arguments [0].Equals (BoolTrue);
                string playbackState = arguments [1];

                if (playWhenReady) {

                    if (!this.isPlaying) {

                        this.isPlaying = true;
                        RaisePlay ();

                        // Do we have an initial tile ID to report?
                        if (m_PreparingTileId != null) {

//                            this.currentTileID = m_PreparingTileId;
                            m_PreparingTileId = null;
                        }

                        // Do we have an initial quality group to report?
                        if (m_PreparingQualityGroup != null) {

                            this.currentQualityGroup = m_PreparingQualityGroup;
                            m_PreparingQualityGroup = null;
                        }

                        // Do we have an initial audio track to report?
                        if (m_PreparingAudioTrack != null) {

                            this.currentAudioTrack = m_PreparingAudioTrack;
                            m_PreparingAudioTrack = null;
                        }
                    }

                } else {

                    if (this.isPlaying) {

                        this.isPlaying = false;
                        RaisePause ();
                    }
                }

                // Playback state change?
                if (!playbackState.Equals (m_LastPlaybackState)) {

                    switch (playbackState) {

                    case StateReady:
                        {
                            // NOTE: ExoPlayer can exit "ready" state during things like a seek, so be diligent
                            //       about not repeating initialization tasks when we return to ready.
                            if (!m_PlayerPrepared) {

                                // The player has been prepared
                                this.duration = SecondsFromMilliseconds(this.androidPlayer.Call<long> ("getDuration"));
                                m_PlayerPrepared = true;
                                OnPrepared();
                            }

                            CheckSeeked ();

                            // Were we stalled?
                            if (m_IsStalled) {

                                RaiseStallRecover ();
                                m_IsStalled = false;
                            }
                            break;
                        }
                    case StateEnded:
                        {
                            CheckSeeked ();

                            // The media has ended
                            // NOTE: PlayerBase will handle looping or ending the video.
                            OnLoop();
                            break;
                        }
                    case StateBuffering:
                        {
                            // The media is buffering

                            // We're only stalled if we're playing (and not seeking).
                            m_IsStalled = (this.isPlaying && !m_PlayerSeeking);
                            if (m_IsStalled) {
                                
                                RaiseStall ();
                            }
                            break;
                        }
                    }
                }

                m_LastPlaybackState = playbackState;
            }
        }

        private void CheckSeeked()
        {
            // Were we seeking?
            // NOTE: Seeks can complete in at least READY and ENDED states, so handle outside the switch.
            if (m_PlayerSeeking) {

                m_PlayerSeeking = false;
                RaiseSeeked (SecondsFromMilliseconds(this.androidPlayer.Call<long> ("getCurrentPosition")));
            }
        }

        public void onQualityGroupsChanged(string message)
        {
            int maxVideoWidth = 0;
            int maxVideoHeight = 0;

            // Note that quality groups are in order from highest-to-lowest average bandwidth
            List<QualityGroup> qualityGroups = new List<QualityGroup> ();

            JSONNode jsonNode = JSON.Parse (message);

            if (jsonNode != null && jsonNode is JSONArray) {

                JSONArray jsonArray = jsonNode as JSONArray;

                foreach (JSONObject qualityGroupObject in jsonArray) {

                    QualityGroup qualityGroup = new QualityGroup (qualityGroupObject);

                    maxVideoWidth = Math.Max (maxVideoWidth, qualityGroup.videoWidth);
                    maxVideoHeight = Math.Max (maxVideoHeight, qualityGroup.videoHeight);

                    qualityGroups.Add(qualityGroup);
                }
            }

            // Update values that are required before OnPrepared()
            this.qualityGroups = qualityGroups;
            this.videoWidth = maxVideoWidth;
            this.videoHeight = maxVideoHeight;
            this.mediaWidth = maxVideoWidth;
            this.mediaHeight = maxVideoHeight;

            AssignTexture (maxVideoWidth, maxVideoHeight);
        }

        public void onAudioFormatsChanged(string message)
        {
            List<AudioTrack> audioTracks = new List<AudioTrack> ();

            JSONNode jsonNode = JSON.Parse (message);

            if (jsonNode != null && jsonNode is JSONArray) {

                JSONArray jsonArray = jsonNode as JSONArray;

                foreach (JSONObject audioTrackObject in jsonArray) {

                    AudioTrack audioTrack = new AudioTrack (audioTrackObject);

                    audioTracks.Add(audioTrack);
                }
            }

            // Update values that are required before OnPrepared()
            this.audioTracks = audioTracks;
        }

        public void onRenderedFirstFrame(string message)
        {
            // The first frame has been rendered
            m_RenderedFirstFrame = true;
        }

        public void onLoadingChanged(string message)
        {
            if (message.Equals (BoolTrue)) {

                //Debug.Log ("Loading...");
            }
        }

        public void onDownstreamFormatChanged(string message)
        {
            // Format name is encoded as "tileId|qualityGroupName"

            string[] arguments = message.Split ('|');
            if (arguments.Length == 2) {

                string tileId = arguments [0];
                QualityGroup qualityGroup = QualityGroupWithName(arguments[1]);

                // Report yet?
                if (m_PlayerPrepared) {

                    // Inform immediately
                    // NOTE: Won't raise a changed event unless the value actually changes
//                    this.currentTileID = tileId;
                    this.currentQualityGroup = qualityGroup;

                } else {

                    // Queue to inform after ready
                    m_PreparingTileId = tileId;
                    m_PreparingQualityGroup = qualityGroup;
                }
            }
        }

        public void onAudioFormatChanged(string message)
        {
            // Message contains the audio track ID

            AudioTrack audioTrack = AudioTrackWithId(message);

            // Report yet?
            if (m_PlayerPrepared) {

                // Inform immediately
                // NOTE: Won't raise a changed event unless the value actually changes
                this.currentAudioTrack = audioTrack;

            } else {

                // Queue to inform after ready
                m_PreparingAudioTrack = audioTrack;
            }
        }

        // NOTE: Added "Exo" to natural method name to avoid collision with PlayerBase method
        public void onExoDroppedFrames(string message)
        {
            RaiseDroppedFrame(Int32.Parse(message));
        }

        public void onPlayerError(string message)
        {
            this.readyState = ReadyState.Error;
            RaiseError (message);
        }

        public void onLoadError(string message)
        {
            this.readyState = ReadyState.Error;
            RaiseError (message);
        }

        public void onBandwidthSample(string message)
        {
            // Bandwidth sample is encoded as "elapsedMs|bytes|bitrate"
            // NOTE: This reports all media bytes (audio, video, etc.), but it does *not*
            //       include the manifest file.

            string[] arguments = message.Split ('|');
            if (arguments.Length == 3) {

                int elapsedMs = Convert.ToInt32 (arguments [0]);
                long bytes = Convert.ToInt64 (arguments [1]);
//                long bitrate = Convert.ToInt64 (arguments [2]);

                RaiseIOCompleted ((int)bytes, SecondsFromMilliseconds(elapsedMs));
            }
        }

        public void onScheduledTileAtTime(string message)
        {
            // Format name is encoded as "tileId|presentationTimeUs"

            string[] arguments = message.Split ('|');
            if (arguments.Length == 2) {

                m_LastFormatTileId = arguments [0];
                long presentationTimeUs = Convert.ToInt64 (arguments [1]);

                // Watch for this tile change
                m_TileUpdateNanoTimeStamp = presentationTimeUs;
            }
        }

        // Imports for Media Surface plug-in

        [DllImport("OculusMediaSurface")]
        private static extern void OVR_Media_Surface_Init();

        [DllImport("OculusMediaSurface")]
        private static extern void OVR_Media_Surface_SetEventBase(int eventBase);

        [DllImport("OculusMediaSurface")]
        private static extern IntPtr OVR_Media_Surface_GetObject();

        [DllImport("OculusMediaSurface")]
        private static extern void OVR_Media_Surface_SetTextureId( IntPtr texPtr );

        [DllImport("OculusMediaSurface")]
        private static extern void OVR_Media_Surface_ResetTexture();

        [DllImport("OculusMediaSurface")]
        private static extern void OVR_Media_Surface_SetTextureParms( int width, int height );

        // Get the last surface nano update time
        [DllImport("OculusMediaSurface")]
        private static extern long OVR_Media_LastUpdateNanoTimeStamp();
    }
}
#endif
