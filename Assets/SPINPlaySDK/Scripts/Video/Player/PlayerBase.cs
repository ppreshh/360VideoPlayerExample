//#define LOG_PLAYER_EVENTS

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

#if !UNITY_5_5_OR_NEWER
#error The SPIN Play SDK requires Unity 5.5 or higher.
#endif

/// <summary>
/// Namespace that contains all video functionality.
/// </summary>
namespace Pixvana.Video
{
    [DisallowMultipleComponent]
    /// <summary>
    /// The base class for all video players.
    /// </summary>
    public abstract partial class PlayerBase : MonoBehaviour
    {
        /// <summary>
        /// Represents an unknown size.
        /// </summary>
        public const int UNKNOWN_SIZE = -1;			// An integer size is unknown

        /// <summary>
        /// Represents an unknown time.
        /// </summary>
        public const double UNKNOWN_TIME = -1.0;	// A double time in unknown

        // Player defaults
        private const bool defaultAutoPlay          = true;
        private const bool defaultAutoQuality       = true;
        private const bool defaultLoop              = false;

        /// <summary>
        /// Ready state of the player.
        /// </summary>
        public enum ReadyState
        {
            /// <summary>
            /// The player is neither prepared nor being prepared (default state).
            /// </summary>
            Idle,
            /// <summary>
            /// The player is being prepared.
            /// </summary>
            Preparing,
            /// <summary>
            /// The player is ready to play, pause, and seek.
            /// </summary>
            Ready,
            /// <summary>
            /// The player has ended all playback.
            /// </summary>
            Ended,
            /// <summary>
            /// The player has experienced a fatal error.
            /// </summary>
            Error,
            Max_ReadyState
        }

        /// <summary>
        /// Information about compatbility of the player with the current platform
        /// </summary>
        public struct CompatiblilityInfo
        {
            /// <summary>
            /// Gets a value indicating whether the player is compatible with the current platform.
            /// </summary>
            public bool isCompatible;
            /// <summary>
            /// The description of any incompatibility (may be shown to the user).
            /// </summary>
            public string description;

            /// <summary>
            /// Initializes a new instance of the <see cref="Pixvana.Video.PlayerBase.CompatiblilityInfo"/> class with isCompatible status and description.
            /// </summary>
            /// <param name="isCompatible"><c>true</c> if compatible; otherwise <c>false</c>.</param>
            /// <param name="description">A description of any incompatibility (may be shown to the user).</param>
            public CompatiblilityInfo(bool isCompatible, string description) {

                this.isCompatible = isCompatible;
                this.description = description;
            }
        }

        public enum TextureColorModel
        {
            RGB,
            YUV
        }

        #region Properties

        [Tooltip("The URL of the source media")]
        [SerializeField] private string m_SourceUrl = null;
        /// <summary>
        /// Gets or sets the URL of the source media (.mpd or .mp4 file).
        /// </summary>
        /// <value>The source URL.</value>
        /// <exception cref="InvalidReadyStateException">if <see cref="readyState"/> is not <see cref="ReadyState.Idle"/> on set.</exception>
        public string sourceUrl { 
            
            get { return m_SourceUrl; } 
            set { 

                AssertReadyState(new ReadyState[] { ReadyState.Idle }, PlayState.Ignore, "Ready state must be {0} to set sourceUrl.");
                m_SourceUrl = value; 
            } 
        }

        [Tooltip("Target objects for video rendering")]
        [SerializeField] private GameObject[] m_TargetObjects = null;
        /// <summary>
        /// Gets or sets the target objects to display rendered video.
        /// </summary>
        /// <value>The target objects to display rendered video.</value>
        /// <exception cref="InvalidReadyStateException">if <see cref="readyState"/> is not <see cref="ReadyState.Idle"/>, <see cref="ReadyState.Preparing"/>, <see cref="ReadyState.Ready"/>, or <see cref="ReadyState.Ended"/> on set.</exception>
        public GameObject[] targetObjects {

            get { return m_TargetObjects; }
            set {

                AssertReadyState(new ReadyState[] { ReadyState.Idle, ReadyState.Preparing, ReadyState.Ready, ReadyState.Ended }, PlayState.Ignore, "Ready state must be {0} to set targetObjects.");
                m_TargetObjects = value; UpdateTexure (); 
            }
        }

        [Tooltip("Should the video automatically play?")]
        [SerializeField] private bool m_AutoPlay = defaultAutoPlay;
        /// <summary>
        /// Gets or sets a value indicating whether the video should automatically play.
        /// </summary>
        /// <value><c>true</c> to automatically play; otherwise, <c>false</c>.</value>
        /// <exception cref="InvalidReadyStateException">if <see cref="readyState"/> is not <see cref="ReadyState.Idle"/>, <see cref="ReadyState.Preparing"/>, <see cref="ReadyState.Ready"/>, or <see cref="ReadyState.Ended"/> on set.</exception>
        public bool autoPlay {

            get { return m_AutoPlay; }
            set {

                AssertReadyState (new ReadyState[] { ReadyState.Idle, ReadyState.Preparing, ReadyState.Ready, ReadyState.Ended }, PlayState.Ignore, "Ready state must be {0} to set autoPlay.");
                if (m_ReadyState != ReadyState.Idle && m_ReadyState != ReadyState.Preparing) {

                    Debug.LogWarning ("When ready state is not Idle or Preparing, setting autoPlay has no effect");
                }
                m_AutoPlay = value;
            } 
        }

        [Tooltip("Should the video loop when it reaches the end?")]
        [SerializeField] private bool m_Loop = defaultLoop;
        /// <summary>
        /// Gets or sets a value indicating whether this the video should automatically loop.
        /// </summary>
        /// <value><c>true</c> to automatically loop; otherwise, <c>false</c>.</value>
        /// <exception cref="InvalidReadyStateException">if <see cref="readyState"/> is not <see cref="ReadyState.Idle"/>, <see cref="ReadyState.Preparing"/>, <see cref="ReadyState.Ready"/>, or <see cref="ReadyState.Ended"/> on set.</exception>
        public bool loop {

            get { return m_Loop; }
            set {

                AssertReadyState (new ReadyState[] { ReadyState.Idle, ReadyState.Preparing, ReadyState.Ready, ReadyState.Ended }, PlayState.Ignore, "Ready state must be {0} to set loop");
                if (m_ReadyState != ReadyState.Idle && m_ReadyState != ReadyState.Preparing && m_ReadyState != ReadyState.Ready) {

                    Debug.LogWarning ("When ready state is not Idle, Preparing, or Ready, setting loop has no effect");
                }
                m_Loop = value; 
            } 
        }

        public TextureColorModel textureColorModel {
            get { return m_TextureModel; }
        }

        /// <summary>
        /// The y-orientation of frames that this player delivers.
        /// </summary>
        /// <value>If isYDown is true then video frames delivered by this player are in the
        /// normal y orientation of video.  In this case the pixel at the lowest address in memory
        /// is a pixel at the visual top of the image, and the last pixel at the last address
        /// in memory is at the visual bottom of the image.  If isYDown is false then the
        /// frames are in the normal y orientation for still images and appear flipped vertically
        /// // from the isYDown == true case.</value>
        public virtual bool isYDown()
        {
            return false;
        }

        private ReadyState m_ReadyState = ReadyState.Idle;
        /// <summary>
        /// Gets the current player state.
        /// </summary>
        /// <value>The state of the player.</value>
        public ReadyState readyState { get { return m_ReadyState; } protected set { m_ReadyState = value; RaiseReadyStateChanged (m_ReadyState); } }

        /// <summary>
        /// Gets compatibility information for the player.
        /// </summary>
        /// <value>The compatibility information.</value>
        public virtual CompatiblilityInfo isCompatible { get { return new CompatiblilityInfo(true, null); } }

        private bool m_IsPlaying = false;
        /// <summary>
        /// Gets a value indicating whether the player is currently playing.
        /// </summary>
        /// <value><c>true</c> if playing; otherwise, <c>false</c>.</value>
        public bool isPlaying
        { 
            get { return (m_ReadyState == ReadyState.Ready ?
                m_IsPlaying :
                false); }
            protected set { m_IsPlaying = value; }
        }

        private List<QualityGroup> m_QualityGroups = null;
        /// <summary>
        /// Gets the available quality groups.
        /// </summary>
        /// <value>The quality groups.</value>
        public List<QualityGroup> qualityGroups
        { 
            get { return ((m_ReadyState == ReadyState.Idle) || (m_ReadyState == ReadyState.Preparing) ?
                null :
                m_QualityGroups); }
            protected set { m_QualityGroups = value; }
        }

        private bool m_AutoQuality = defaultAutoQuality;
        /// <summary>
        /// Gets or sets a value indicating whether the video should automatically adapt its quality.
        /// </summary>
        /// <value><c>true</c> to automatically adapt quality; otherwise, <c>false</c>.</value>
        /// <exception cref="InvalidReadyStateException">if <see cref="readyState"/> is not <see cref="ReadyState.Ready"/> or <see cref="ReadyState.Ended"/> on set.</exception>
        public bool autoQuality
        {
            get { return ((m_ReadyState == ReadyState.Idle) || (m_ReadyState == ReadyState.Preparing) ?
                defaultAutoQuality :
                m_AutoQuality); }
            set
            {
                AssertReadyState(new ReadyState[] { ReadyState.Ready, ReadyState.Ended }, PlayState.Ignore, "Ready state must be {0} to set autoQuality.");
                if (m_ReadyState != ReadyState.Ready) {

                    Debug.LogWarning ("When ready state is not Ready, setting autoQuality has no effect");
                }
                m_AutoQuality = value;
                if (m_AutoQuality) {

                    // Enable auto quality
                    OnEnableAutoQuality ();
                } 
                else
                {
                    // Set current quality
                    SetQualityGroup (m_CurrentQualityGroup);
                }
            }
        }

        private List<AudioTrack> m_AudioTracks = null;
        /// <summary>
        /// Gets the available audio tracks.
        /// </summary>
        /// <value>The audio tracks.</value>
        public List<AudioTrack> audioTracks
        { 
            get { return ((m_ReadyState == ReadyState.Idle) || (m_ReadyState == ReadyState.Preparing) ?
                null :
                m_AudioTracks); }
            protected set { m_AudioTracks = value; }
        }

        // NOTE: This is the currently-playing audio track, not the requested/upcoming audio track
        private AudioTrack m_CurrentAudioTrack = null;
        /// <summary>
        /// Gets the currently-playing audio track.
        /// </summary>
        /// <value>The audio track.</value>
        public AudioTrack currentAudioTrack
        {
            get { return ((m_ReadyState == ReadyState.Idle) || (m_ReadyState == ReadyState.Preparing) ?
                null :
                m_CurrentAudioTrack); }

            protected set {

                // Has the value actually changed?
                if (value != m_CurrentAudioTrack) {

                    m_CurrentAudioTrack = value;
                    RaiseAudioTrackChanged (m_CurrentAudioTrack);            
                }
            }
        }

        // Return UNKNOWN_SIZE until the player is in Ready state or later
        private int m_VideoWidth = UNKNOWN_SIZE;
        /// <summary>
        /// Gets the width of the video frame.
        /// </summary>
        /// <value>The width of the video frame.</value>
        public int videoWidth 
        { 
            get { return ((m_ReadyState == ReadyState.Idle) || (m_ReadyState == ReadyState.Preparing) ?
                    UNKNOWN_SIZE :
                    m_VideoWidth); }
            protected set { m_VideoWidth = value; }
        }

        // Return UNKNOWN_SIZE until the player is in Ready state or later
        private int m_VideoHeight = UNKNOWN_SIZE;
        /// <summary>
        /// Gets the height of the video frame.
        /// </summary>
        /// <value>The height of the video frame.</value>
        public int videoHeight
        { 
            get { return ((m_ReadyState == ReadyState.Idle) || (m_ReadyState == ReadyState.Preparing) ?
                UNKNOWN_SIZE :
                m_VideoHeight); }
            protected set { m_VideoHeight = value; }
        }

        // Return UNKNOWN_SIZE until the player is in Ready state or later
        private int m_MediaWidth = UNKNOWN_SIZE;
        /// <summary>
        /// Gets the width of the valid pixels in the video frame
        /// </summary>
        /// <value>The size of the video texture will be videoWidth by videoHeight,
        /// but these textures may contain "padding" on the right and bottom sides of the
        /// texture.  The valid part of the texture that contains real video data is
        /// mediaWidth by mediaHeight</value>
        public int mediaWidth
        {
            get { return ((m_ReadyState == ReadyState.Idle) || (m_ReadyState == ReadyState.Preparing) ?
              UNKNOWN_SIZE :
              m_MediaWidth);
            }
            protected set { m_MediaWidth = value; }
        }

        // Return UNKNOWN_SIZE until the player is in Ready state or later
        private int m_MediaHeight = UNKNOWN_SIZE;
        /// <summary>
        /// Gets the height of the valid pixels in the video frame
        /// </summary>
        /// <value>The size of the video texture will be videoWidth by videoHeight,
        /// but these textures may contain "padding" on the right and bottom sides of the
        /// texture.  The valid part of the texture that contains real video data is
        /// mediaWidth by mediaHeight</value>
        public int mediaHeight
        {
            get { return ((m_ReadyState == ReadyState.Idle) || (m_ReadyState == ReadyState.Preparing) ?
              UNKNOWN_SIZE :
              m_MediaHeight);
            }
            protected set { m_MediaHeight = value; }
        }

        // Return UNKNOWN_TIME until the player is in Ready state or later
        private double m_Duration = UNKNOWN_TIME;
        /// <summary>
        /// Gets the duration of the video.
        /// </summary>
        /// <remarks>The duration will return <see cref="UNKNOWN_TIME"/> until <see cref="readyState"/> is <see cref="ReadyState.Ready"/>.</remarks>
        /// <value>The duration of the video (in seconds).</value>
        public double duration
        { 
            get { return ((m_ReadyState == ReadyState.Idle) || (m_ReadyState == ReadyState.Preparing) ?
                UNKNOWN_TIME :
                m_Duration); }
            protected set { m_Duration = value; }
        }

        // Return UNKNOWN_TIME until the player is in Ready state or later.
        // Then, raise the current time changed event whenever this is set by a subclass.
        private double m_CurrentTime = UNKNOWN_TIME;
        /// <summary>
        /// Gets the current playback time.
        /// </summary>
        /// <value>The current playback time (in seconds).</value>
        public double currentTime
        { 
            get { return ((m_ReadyState == ReadyState.Idle) || (m_ReadyState == ReadyState.Preparing) ?
                UNKNOWN_TIME :
                m_CurrentTime); }
            
            protected set {

                // Has the value actually changed?
                if (value != m_CurrentTime) {
                    
                    m_CurrentTime = value;
                    RaiseCurrentTimeChanged (m_CurrentTime);
                }
            }
        }

        // Return null until the player is in Ready state or later
        private Texture2D m_Texture1 = null;
        private Texture2D m_Texture2 = null;
        private TextureColorModel m_TextureModel = TextureColorModel.RGB;

        /// <summary>
        /// Gets the texture that is used to render the video frame.
        /// </summary>
        /// <value>The texture for the video frame.</value>
        public Texture2D texture 
        { 
            get { return ((m_ReadyState == ReadyState.Idle) || (m_ReadyState == ReadyState.Preparing) ?
                null :
                m_Texture1); }
            protected set { m_Texture1 = value; m_TextureModel = TextureColorModel.RGB;  UpdateTexure(); }
        }

        public Texture2D[] textures
        {
            get
            {
                switch (m_TextureModel)
                {
                    case TextureColorModel.RGB:
                        {
                            return new Texture2D[] { m_Texture1 };
                        }
                    case TextureColorModel.YUV:
                        {
                            return new Texture2D[] { m_Texture1, m_Texture2 };
                        }

                }
                Debug.LogError("unknown texture color model");
                return new Texture2D[0];
            }
        }

        public void SetYUVTextures(Texture2D yTexture, Texture2D uvTexture)
        {
            m_Texture1 = yTexture;
            m_Texture2 = uvTexture;
            m_TextureModel = TextureColorModel.YUV;
        }

        // The current tile ID
        // NOTE: This is the currently-playing tile ID, not the requested/upcoming tile ID
        private string m_CurrentTileID = null;
        /// <summary>
        /// Gets the currently visible tile ID.
        /// </summary>
        /// <value>The currently visible tile ID.</value>
        public string currentTileID
        { 
            get { return ((m_ReadyState == ReadyState.Idle) || (m_ReadyState == ReadyState.Preparing) ?
                null :
                m_CurrentTileID); }

            protected set {

                // Has the value actually changed?
                if (!value.Equals(m_CurrentTileID)) {

                    m_CurrentTileID = value;
                    RaiseTileChanged (m_CurrentTileID);                }
            }
        }

        // The last requested tile ID
        // NOTE: This is the last requested tile ID, not the current/playing tile ID
        private string m_RequestedTileID = null;
        /// <summary>
        /// Gets the last requested tile ID.
        /// </summary>
        /// <value>The last requested tile ID.</value>
        public string requestedTileID
        {
            get { return m_RequestedTileID; }
        }

        // NOTE: This is the currently-playing quality group, not the requested/upcoming quality group
        private QualityGroup m_CurrentQualityGroup = null;
        /// <summary>
        /// Gets the currently-playing quality group.
        /// </summary>
        /// <value>The quality group.</value>
        public QualityGroup currentQualityGroup
        {
            get { return ((m_ReadyState == ReadyState.Idle) || (m_ReadyState == ReadyState.Preparing) ?
                null :
                m_CurrentQualityGroup); }

            protected set {

                // Has the value actually changed?
                if (value != m_CurrentQualityGroup) {

                    m_CurrentQualityGroup = value;
                    RaiseQualityGroupChanged (m_CurrentQualityGroup);            
                }
            }
        }

        /// <summary>
        /// Occurs when <see cref="readyState"/> changed.
        /// </summary>
        public event EventHandler<ReadyStateChangedEventArgs> onReadyStateChanged;

        /// <summary>
        /// Occurs when the player has been reset.
        /// </summary>
        public event EventHandler<EventArgs> onReset;

        /// <summary>
        /// Occurs when the video has begun to play.
        /// </summary>
        public event EventHandler<EventArgs> onPlay;

        /// <summary>
        /// Occurs when the video has been paused.
        /// </summary>
        public event EventHandler<EventArgs> onPause;

        /// <summary>
        /// Occurs when seek has completed.
        /// </summary>
        public event EventHandler<SeekedEventArgs> onSeeked;

        /// <summary>
        /// Occurs when <see cref="currentTime"/> changed.
        /// </summary>
        public event EventHandler<CurrentTimeChangedEventArgs> onCurrentTimeChanged;

        /// <summary>
        /// Occurs when the video has looped.
        /// </summary>
        public event EventHandler<EventArgs> onLoop;

        /// <summary>
        /// Occurs when <see cref="currentTileID"/> changed.
        /// </summary>
        public event EventHandler<TileChangedEventArgs> onTileChanged;

        /// <summary>
        /// Occurs when the current quality group changed.
        /// </summary>
        public event EventHandler<QualityGroupChangedEventArgs> onQualityGroupChanged;

        /// <summary>
        /// Occurs when the current audio track changed.
        /// </summary>
        public event EventHandler<AudioTrackChangedEventArgs> onAudioTrackChanged;

        /// <summary>
        /// Occurs when asyncronous errors are detected in the player.
        /// </summary>
        public event EventHandler<ErrorEventArgs> onError;

        /// <summary>
        /// Occurs when the underlying player has read a block of data from the source
        /// </summary>
        public event EventHandler<IOCompletedEventArgs> onIOCompleted;

        /// <summary>
        /// Occurs when the underlying player has stalled and begun buffering
        /// </summary>
        public event EventHandler<EventArgs> onStall;

        /// <summary>
        /// Occurs when the underlying player has recovered from a previous stall
        /// </summary>
        public event EventHandler<EventArgs> onStallRecover;

        /// <summary>
        /// Occurs when the underlying player has skipped one or more frames in an attempt to keep up with the clock
        /// </summary>
        public event EventHandler<DroppedFramesEventArgs> onDroppedFrames;

#endregion

        #region Unity Events

        public virtual void Awake() {

            CheckCompabitility ();
        }

        #endregion

        #region Player Control

        /// <summary>
        /// Resets the player to an initial idle state.
        /// </summary>
        public virtual void ResetPlayer() {

            // NOTE: ResetPlayer can be called at any time.
            // ALSO NOTE: This method is named ResetPlayer to avoid collision with MonoBehavior's Reset

            // Reset state
            m_SourceUrl = null;
            m_TargetObjects = null;
            m_AutoPlay = defaultAutoPlay;
            m_Loop = defaultLoop;
            m_IsPlaying = false;
            m_QualityGroups = null;
            m_VideoWidth = UNKNOWN_SIZE;
            m_VideoHeight = UNKNOWN_SIZE;
            m_Duration = UNKNOWN_TIME;
            m_CurrentTime = UNKNOWN_TIME;
            m_Texture1 = null;
            m_Texture2 = null;
            m_TextureModel = TextureColorModel.RGB;
            m_CurrentTileID = null;
            m_RequestedTileID = null;
            m_AutoQuality = defaultAutoQuality;
            m_CurrentQualityGroup = null;
            m_IsPlaying = false;

            // Unity maintains a native heap that is related to--but disconnected from--the .NET managed
            // heap. Unity uses its native heap to manage assets like textures, meshes, shaders, etc.
            // While .NET's garbage collection process will automatically clean-up and deallocate unused
            // *managed* references, it won't always clean-up the related resources in Unity's native heap.
            //
            // Without cleaning the native heap, resources like textures will continue to accumulate until
            // out-of-memory errors begin to occur. To force a clean-up of Unity's native heap, call
            // Resources.UnloadUnusedAssets(). Normally, Unity does this when a new scene is loaded, but if
            // an app uses a single scene, it needs to call this periodically. We choose to do it during
            // a full player reset.
            //
            // Unfortunately, there is very little official information from Unity that explains these concepts.
            // Most of this has been gleaned from various blog and forum posts. The best of these posts is:
            // http://www.supersegfault.com/managing-memory-in-unity3d/

            Resources.UnloadUnusedAssets();

            // Set readyState property so subscribers are informed
            this.readyState = ReadyState.Idle;

            RaiseReset();
        }

        /// <summary>
        /// Prepares the player for playback.
        /// </summary>
        /// <exception cref="InvalidReadyStateException">if <see cref="readyState"/> is not <see cref="ReadyState.Idle"/>.</exception>
        /// <exception cref="ArgumentNullException">if <see cref="sourceUrl"/> is null.</exception>
        /// <exception cref="ArgumentNullException">if <see cref="requestedTileID"/> is null.</exception>
        public virtual void Prepare() {

            Prepare (null);
        }

        /// <summary>
        /// Prepares the player for playback with the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="InvalidReadyStateException">if <see cref="readyState"/> is not <see cref="ReadyState.Idle"/>.</exception>
        /// <exception cref="ArgumentNullException">if <see cref="sourceUrl"/> is null.</exception>
        /// <exception cref="ArgumentNullException">if <see cref="requestedTileID"/> is null.</exception>
        public virtual void Prepare(Dictionary<string, object> configuration) {

            // Validate state before transitioning to the Preparing state
            AssertReadyState(new ReadyState[] { ReadyState.Idle }, PlayState.Ignore, "Ready state must be {0} to call Prepare.");
            AssertArgumentNotNull (m_SourceUrl, "sourceUrl");
            AssertArgumentNotNull (m_RequestedTileID, "requestedTileID");

            this.readyState = ReadyState.Preparing;
        }

        protected virtual void OnPrepared() {

            // Quality groups, video width, video height, media width, media height, duration, texture, currentTime, currentTileID, and currentQualityGroup 
            // must be available before transitioning to the Ready state.
            Assert.IsTrue (m_QualityGroups != null && m_QualityGroups.Count > 0, "Could not determine quality group(s)");
            Assert.IsTrue (m_VideoWidth != UNKNOWN_SIZE && m_VideoHeight != UNKNOWN_SIZE, "Could not determine video size");
            Assert.IsTrue (m_MediaWidth != UNKNOWN_SIZE && m_MediaHeight != UNKNOWN_SIZE, "Could not determine media size");
            Assert.IsTrue (m_Duration != UNKNOWN_TIME, "Could not determine video duration");
            Assert.IsNotNull (m_Texture1, "Could not create video texture");

            this.readyState = ReadyState.Ready;

            // Automatically start playback?
            if (m_AutoPlay) {

                Play ();
            }
        }

        /// <summary>
        /// Called when auto quality is enabled.
        /// </summary>
        protected virtual void OnEnableAutoQuality() {

            Assert.IsTrue (m_ReadyState == ReadyState.Ready, "Must be in ready state to set auto quality");
        }

        protected virtual void OnLoop() {

            Assert.IsTrue (m_ReadyState == ReadyState.Ready, "Must be in ready state to loop");

            if (m_Loop) {

                RaiseLoop();
                Seek(0.0);

            } else {

                // Playback has ended
                this.readyState = ReadyState.Ended;
            }
        }

        /// <summary>
        /// Plays the video.
        /// </summary>
        /// <exception cref="InvalidReadyStateException">if <see cref="readyState"/> is not <see cref="ReadyState.Ready"/>.</exception>
        public virtual void Play() {

            AssertReadyState (new ReadyState[] { ReadyState.Ready }, PlayState.Ignore, "Ready state must be {0} to call Play.");
            if (m_TargetObjects == null || m_TargetObjects.Length < 1) {
                
                Debug.LogWarning ("Add one or more target objects to see rendered video");
            }

            // Start playing media
        }

        /// <summary>
        /// Pauses the video.
        /// </summary>
        /// <exception cref="InvalidReadyStateException">if <see cref="readyState"/> is not <see cref="ReadyState.Ready"/>.</exception>
        public virtual void Pause() {

            AssertReadyState(new ReadyState[] { ReadyState.Ready }, PlayState.Ignore, "Ready state must be {0} to call Pause.");

            // Pause media
        }

        /// <summary>
        /// Seeks to a specific time.
        /// </summary>
        /// <param name="time">The time to seek.</param>
        /// <exception cref="InvalidReadyStateException">if <see cref="readyState"/> is not <see cref="ReadyState.Ready"/>.</exception>
        /// <exception cref="InvalidPlayStateException">if <see cref="isPlaying"/> is not true.</exception>
        /// <exception cref="ArgumentOutOfRangeException">if <paramref name="time"/> is not &gt;= 0.0 and &lt;= <see cref="duration"/>.</exception>
        public virtual void Seek(double time, bool approximate = false) {

            AssertReadyState (new ReadyState[] { ReadyState.Ready }, PlayState.Playing, "Ready state must be {0} to call Seek.");
            AssertArgumentInRange ((time >= 0.0) && (time <= m_Duration), "time", string.Format("Seek time must be >= 0.0 and <= duration ({0}).", m_Duration));

            // Seek media
        }

        // This sets the current tile ID
        // NOTE: This is the tile that *should* be current (but might not be yet). Callers should
        //       be able to call this as frequently as necessary to provide updates. The player
        //       itself can decide how/when to actually switch to the new tile.
        /// <summary>
        /// Sets the requested tile ID.
        /// </summary>
        /// <param name="tileID">The tile ID.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="tileID"/> is null.</exception>
        /// <exception cref="InvalidReadyStateException">if <see cref="readyState"/> is not <see cref="ReadyState.Idle"/>, <see cref="ReadyState.Preparing"/>, <see cref="ReadyState.Ready"/>, or <see cref="ReadyState.Ended"/> on set.</exception>
        public virtual void SetTileID(string tileID) {

            AssertArgumentNotNull (tileID, "tileID");

            // Track the last requested tile ID
            m_RequestedTileID = tileID;

            // Decide when to switch to the new tile, then raise the tile changed event
        }

        // Like SetTileID, this is the quality group that *should* be current (but might not be yet).
        /// <summary>
        /// Sets the requested quality group.
        /// </summary>
        /// <param name="qualityGroup">Quality group.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="qualityGroup"/> is null.</exception>
        /// <exception cref="InvalidReadyStateException">if <see cref="readyState"/> is not <see cref="ReadyState.Ready"/> or <see cref="ReadyState.Ended"/> on set.</exception>
        public virtual void SetQualityGroup(QualityGroup qualityGroup) {

            AssertArgumentNotNull (qualityGroup, "qualityGroup");
            AssertReadyState(new ReadyState[] { ReadyState.Ready, ReadyState.Ended }, PlayState.Ignore, "Ready state must be {0} to call SetQualityGroup.");

            // When a specific quality is requested, auto quality becomes disabled
            m_AutoQuality = false;

            // Decide when to switch to the new quality group, then raise the quality group changed event
        }

        /// <summary>
        /// Gets a quality group with the specified name.
        /// </summary>
        /// <returns>The quality group.</returns>
        /// <param name="name">The name.</param>
        public QualityGroup QualityGroupWithName(string name) {

            QualityGroup foundQualityGroup = null;

            if (m_QualityGroups != null) {

                foreach (QualityGroup qualityGroup in m_QualityGroups) {

                    if (qualityGroup.name.Equals (name)) {

                        foundQualityGroup = qualityGroup;
                        break;
                    }
                }
            }

            return foundQualityGroup;
        }

        /// <summary>
        /// Sets the audio orientation.
        /// </summary>
        /// <param name="orientation">The current orientation.</param>
        /// <param name="orientationOffset">The current orientation offset.</param>
        public virtual void SetAudioOrientation(Vector3 orientation, Quaternion orientationOffset) { }

        // Like SetTileID, this is the audio track that *should* be current (but might not be yet).
        /// <summary>
        /// Sets the requested audio track.
        /// </summary>
        /// <param name="audioTrack">Audio track.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="audioTrack"/> is null.</exception>
        /// <exception cref="InvalidReadyStateException">if <see cref="readyState"/> is not <see cref="ReadyState.Ready"/> or <see cref="ReadyState.Ended"/> on set.</exception>
        public virtual void SetAudioTrack(AudioTrack audioTrack) {

            AssertArgumentNotNull (audioTrack, "audioTrack");
            AssertReadyState(new ReadyState[] { ReadyState.Ready, ReadyState.Ended }, PlayState.Ignore, "Ready state must be {0} to call SetAudioTrack.");

            // Decide when to switch to the new audio track, then raise the audio track changed event
        }

        /// <summary>
        /// Gets a audio track with the specified ID.
        /// </summary>
        /// <returns>The audio track.</returns>
        /// <param name="id">The ID.</param>
        public AudioTrack AudioTrackWithId(string id) {

            AudioTrack foundAudioTrack = null;

            if (m_AudioTracks != null) {

                foreach (AudioTrack audioTrack in m_AudioTracks) {

                    if (audioTrack.id.Equals (id)) {

                        foundAudioTrack = audioTrack;
                        break;
                    }
                }
            }

            return foundAudioTrack;
        }

        private void UpdateTexure() {

            if (m_TargetObjects != null) {
                
                foreach (GameObject targetObject in m_TargetObjects) {

                    MeshRenderer meshRenderer = targetObject.GetComponent<MeshRenderer>();
                    if (meshRenderer != null) {

                        meshRenderer.sharedMaterial.mainTexture = m_Texture1;
                        switch (m_TextureModel)
                        {
                            case TextureColorModel.RGB:
                                break;
                            case TextureColorModel.YUV:
                                meshRenderer.sharedMaterial.SetTexture("_Y", m_Texture1);
                                meshRenderer.sharedMaterial.SetTexture("_UV", m_Texture2);
                                break;
                            default:
                                Debug.LogWarning("unknown texture model: " + m_TextureModel);
                                break;
                        }

                    } else {
                        
                        Debug.LogWarning("No MeshRenderer found on target object");
                    }
                }
            }
        }

        public void UpdateShader(Pixvana.Video.ShaderSelector ss)
        {
            ss.colorModel = m_TextureModel;
            ss.sourceTextures = this.textures;
        }

#endregion

#region Player Events

        private void RaiseReadyStateChanged(ReadyState readyState) {

            Assert.IsTrue (readyState < ReadyState.Max_ReadyState, "Invalid readyState");

#if LOG_PLAYER_EVENTS
            Debug.Log("onReadyStateChanged: readyState = " + readyState.ToString());
#endif

            // Invoke the onReadyStateChanged event
            if (onReadyStateChanged != null) {

                onReadyStateChanged (this, new ReadyStateChangedEventArgs (readyState));
            }
        }

        protected void RaiseReset()
        {

#if LOG_PLAYER_EVENTS
            Debug.Log("onReset");
#endif

            // Invoke the onReset event
            if (onReset != null)
            {

                onReset(this, EventArgs.Empty);
            }
        }

        protected void RaisePlay() {

            AssertReadyState (new ReadyState[] { ReadyState.Ready }, PlayState.Playing, "Ready state must be {0} to raise onPlay.");

#if LOG_PLAYER_EVENTS
            Debug.Log("onPlay");
#endif

            // Invoke the onPlay event
            if (onPlay != null) {

                onPlay (this, EventArgs.Empty);
            }
        }

        protected void RaisePause() {

            AssertReadyState (new ReadyState[] { ReadyState.Ready }, PlayState.Paused, "Ready state must be {0} to raise onPause.");

#if LOG_PLAYER_EVENTS
            Debug.Log("onPause");
#endif

            // Invoke the OnPause event
            if (onPause != null) {

                onPause (this, EventArgs.Empty);
            }
        }

        protected void RaiseSeeked(double seekTime) {

            AssertReadyState (new ReadyState[] { ReadyState.Ready }, PlayState.Playing, "Ready state must be {0} to raise onSeeked.");

#if LOG_PLAYER_EVENTS
            Debug.Log("onSeeked: seekTime = " + seekTime.ToString());
#endif

            // Invoke the onSeeked event
            if (onSeeked != null) {

                onSeeked (this, new SeekedEventArgs(seekTime));
            }
        }

        private void RaiseCurrentTimeChanged(double currentTime) {

            AssertReadyState (new ReadyState[] { ReadyState.Ready }, PlayState.Playing, "Ready state must be {0} to raise onCurrentTimeChanged.");
            Assert.IsTrue (currentTime >= 0.0, "currentTime must be equal to or greater than 0");

#if LOG_PLAYER_EVENTS
            Debug.Log("onCurrentTimeChanged: currentTime = " + currentTime.ToString());
#endif

            // Invoke the onCurrentTimeChanged event
            if (onCurrentTimeChanged != null) {

                onCurrentTimeChanged (this, new CurrentTimeChangedEventArgs (currentTime));
            }
        }

        protected void RaiseLoop() {

            AssertReadyState (new ReadyState[] { ReadyState.Ready }, PlayState.Playing, "Ready state must be {0} to raise onLoop.");

#if LOG_PLAYER_EVENTS
            Debug.Log("onLoop");
#endif

            // Invoke the onLoop event
            if (onLoop != null) {

                onLoop (this, EventArgs.Empty);
            }
        }

        private void RaiseTileChanged (string tileID)
        {
            AssertReadyState (new ReadyState[] { ReadyState.Ready }, PlayState.Playing, "Ready state must be {0} to raise onTileChanged.");
            Assert.IsNotNull (tileID, "tileID cannot be null");

#if LOG_PLAYER_EVENTS
            Debug.Log("onTileChanged: tileID = " + tileID);
#endif

            // Invoke the onTileChanged event
            if (onTileChanged != null) {

                onTileChanged (this, new TileChangedEventArgs (tileID));
            }
        }

        private void RaiseQualityGroupChanged (QualityGroup qualityGroup)
        {
            AssertReadyState (new ReadyState[] { ReadyState.Ready }, PlayState.Playing, "Ready state must be {0} to raise onQualityGroupChanged.");
            Assert.IsNotNull (qualityGroup, "qualityGroup cannot be null");

#if LOG_PLAYER_EVENTS
            Debug.Log("onQualityGroupChanged: qualityGroup = " + qualityGroup.name);
#endif

            // Invoke the onQualityGroupChanged event
            if (onQualityGroupChanged != null) {

                onQualityGroupChanged (this, new QualityGroupChangedEventArgs (qualityGroup));
            }
        }

        private void RaiseAudioTrackChanged (AudioTrack audioTrack)
        {
            AssertReadyState (new ReadyState[] { ReadyState.Ready }, PlayState.Playing, "Ready state must be {0} to raise onAudioTrackChanged.");
            Assert.IsNotNull (audioTrack, "audioTrack cannot be null");

            #if LOG_PLAYER_EVENTS
            Debug.Log("onAudioTrackChanged: audioTrack = " + audioTrack.id);
            #endif

            // Invoke the onAudioTrackChanged event
            if (onAudioTrackChanged != null) {

                onAudioTrackChanged (this, new AudioTrackChangedEventArgs (audioTrack));
            }
        }

        protected void RaiseError(string message)
        {
            AssertReadyState (new ReadyState[] { ReadyState.Error }, PlayState.Ignore, "Ready state must be {0} to raise onError.");

            Debug.LogError(message);

#if LOG_PLAYER_EVENTS
            Debug.Log("onError: message = " + message);
#endif

            // Invoke the onError event
            if (onError != null) {

                onError (this, new ErrorEventArgs (message));
            }
        }

        protected void RaiseIOCompleted(int bytes, double time)
        {
            if (onIOCompleted != null) {
                onIOCompleted (this, new IOCompletedEventArgs (bytes, time));
            }
        }

        protected void RaiseStall()
        {
            if (onStall != null) {
                onStall (this, new EventArgs ());
            }
        }

        protected void RaiseStallRecover()
        {
            if (onStallRecover != null) {
                onStallRecover (this, new EventArgs ());
            }
        }

        protected void RaiseDroppedFrame(int numFramesDropped)
        {
            if (onDroppedFrames != null) {
                onDroppedFrames (this, new DroppedFramesEventArgs (numFramesDropped));
            }
        }

#endregion

        #region Assertions & Checks

        private void CheckCompabitility() {

            CompatiblilityInfo compatibilityInfo = this.isCompatible;
            if (!compatibilityInfo.isCompatible) {

                // Player is not compatible
                throw new InvalidOperationException (compatibilityInfo.description);
            }
        }

        protected enum PlayState
        {
            Ignore,
            Playing,
            Paused,
            Max_PlayState
        }

        // NOTE: validPlayState is only asserted when the current readyState is Ready
        protected void AssertReadyState(ReadyState[] validReadyStates, PlayState validPlayState, string message) {

            bool isValid = false;

            foreach (ReadyState validReadyState in validReadyStates) {

                if (m_ReadyState == validReadyState) {

                    // Special assertion for readyState == Ready
                    if ((m_ReadyState == ReadyState.Ready &&
                        validPlayState != PlayState.Ignore))
                    {
                        isValid = ((validPlayState == PlayState.Playing && m_IsPlaying) ||
                            (validPlayState == PlayState.Paused && !m_IsPlaying));
                    }
                    else
                    {
                        isValid = true;
                    }

                    if (isValid)
                    {
                        break;
                    }
                }
            }

            if (!isValid) {

                throw new InvalidOperationException (string.Format(message, GetStringForStates(m_ReadyState, validReadyStates, validPlayState)));
            }
        }

        protected string GetStringForStates(ReadyState readyState, ReadyState[] validReadyStates, PlayState validPlayState) {

            string validStates = "";

            foreach (ReadyState validReadyState in validReadyStates) {

                validStates += string.Format ("{0}{1}", (validStates.Length > 0 ? " | " : ""), validReadyState.ToString ());

                if (readyState == validReadyState) {

                    // Special assertion for readyState == Ready
                    if ((readyState == ReadyState.Ready &&
                        validPlayState != PlayState.Ignore))
                    {
                        validStates += string.Format(" ({0})", (validPlayState == PlayState.Playing ? "playing" : "paused"));
                    }
                }
            }

            return validStates;
        }

        protected void AssertArgumentInRange(bool isTrue, string paramName, string message) {

            if (!isTrue) {

                throw new ArgumentOutOfRangeException (paramName, message);
            }
        }

        protected void AssertArgumentNotNull(object obj, string paramName) {

            if (obj == null) {

                throw new ArgumentNullException (paramName);
            }
        }

#endregion
    }
}
