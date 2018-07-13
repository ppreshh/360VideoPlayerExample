#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WSA_10_0

#define LOAD_AUDIO360
#define USE_AUDIO360

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using SimpleJSON;
using System.Globalization;
using System.Threading;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Pixvana.Video
{

    public partial class Player
    {

#if UNITY_EDITOR_WIN
        private const string dllPath = "SPINPlaySDK/Plugins/x86_64/";
#else
#if !UNITY_WSA_10_0
        private const string dllPath = "Plugins/";
#endif
#endif

        private const int autoQualityGroupIndex         = -1;
        
#region Configuration keys
        
        /// <summary>
        /// Contains the GUID of the IMMDevice for the Windows audio endpoint to use
        /// </summary>
        public const string AudioOutIdKey               = "AudioOutId";
        
#endregion
        
#region Dll imports
        
        // name of the dll we load
        private const string _dll_name = "icarus-three.dll";

#if LOAD_AUDIO360 || USE_AUDIO360
        private const string _audio360_dll_name = "Audio360.dll";
#endif

        [StructLayout(LayoutKind.Sequential)]
        private struct quality_info
        {
            public int id_name_len;
            public int width;
            public int height;
            public int framerate_num;
            public int framerate_denom;
            public int bandwidth;
        };

        private enum PlayerCodes
        {
            kNoOp = 0,
            kIssuePluginEvent = 1,
            kMediaEnded = 2,
            kError = 3,
            kTexturesReady = 4,
            kFirstFrameReady = 5,
            kHaveFrame = 6,
            kAsyncNotification = 7
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct player_info
        {
            // if ret val == PlayerCodes.kIssuePluginEvent, PlayerCodes.kFirstFrameReady
            // or PlayerCodes.kHaveFrame.  these are the values to pass
            // to GL.IssuePluginEvent
            public IntPtr plugin_event_proc;
            public int plugin_event_code;

            // if ret val == PlayerCodes.kTexturesReady, these are valid and represent the texture
            public int textureWidth;
            public int textureHeight;
            public IntPtr nativeTexture1;
            public IntPtr nativeTexture2;

            public int mediaWidth;
            public int mediaHeight;

            // if ret val == kFirstFrameReady or kPrepared tile_id is the tile number corresponding to
            // this frame - that is, this is the first frame from some previous call to either
            // new_player or set_tile.  current_quality_level and is_auto_quality are the currently
            // playing quality level and whether or not the player is auto-switching quality levels.
            public int tile_id;
            public int current_quality_level;
            public int is_auto_quality;

            // if ret val == kFirstFrameReady, kHaveFrame, or kPrepared this is the
            // play time, in milliseconds, of the when the next frame will be displayed (the end
            // of when this current frame will be visible).  If ret val == PlayerCodes.kSeeked
            // then this is the time of the first frame whose display time is equal to or greater
            // than the time requested in the seek call (the start of when the frame is displayed).
            // This PlayerCodes.kSeeked is returned immediately prior to the frame itself being
            // delivered (signalled by either kFirstFrameReady or kHaveFrame).
            public int current_pos_in_ms;
        };

        private static bool reset = false;

#if !UNITY_WSA_10_0 || UNITY_EDITOR_WIN

        // outside of UWP we manually load the dll.  WTF is wrong with Unity engining?

        private delegate int dll_init(IntPtr example_texture, string log_file_path, string ambisonics_lib_name, int reset);

        private delegate void new_player([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] string_keys,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] string_vals,
            int string_count,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] int_keys,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.SysInt)] int[] int_vals,
            int int_count);

        // delegates (and types) for each function that we call in the dll
        private delegate void delete_player();
        
        private delegate void set_tile(int tile_id, string tile_name);
        
        private delegate int play();
        
        private delegate int pause();
        
        private delegate void seek(int time_in_ms, int approximate);
        
        private delegate int get_media_duration();
        
        private delegate int get_last_error(StringBuilder data, ref int max_len);

        private delegate void get_async_event(StringBuilder key, ref int max_key_len, StringBuilder str_val, ref int max_str_val_len, ref int int_val1, ref int int_val2);
        
        private delegate void shutdown_player();
        
        private delegate int get_num_quality_levels();
                
        private delegate void get_indexed_quality(int q_index, StringBuilder id_name, ref quality_info qi);
        
        private delegate void set_quality(int q_index);
        
        // returns a PlayerCode, and sets corresponding pi values
        private delegate int player_update(ref player_info pi);

        private delegate void set_orientation1(float x, float y, float z, float w);
        
        private static dll_init dll_init_del = null;
        private static new_player new_player_del = null;
        private static delete_player delete_player_del;
        private static set_tile set_tile_del = null;
        private static play play_del = null;
        private static pause pause_del = null;
        private static seek seek_del = null;
        private static get_media_duration get_media_duration_del = null;
        private static get_last_error get_last_error_del = null;
        private static get_async_event get_async_event_del = null;
        private static shutdown_player shutdown_player_del = null;
        private static player_update player_update_del = null;
        private static get_num_quality_levels get_num_quality_levels_del = null;
        private static get_indexed_quality get_indexed_quality_del = null;
        private static set_quality set_quality_del = null;
        private static set_orientation1 set_orientation_del = null;
        
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);
        
        [DllImport("kernel32", SetLastError = true)]
        private static extern int FreeLibrary(IntPtr lib);
        
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
        
        // one-time, on-demand, get the HINSTANCE for the dll
        private static IntPtr _dll_ptr = IntPtr.Zero;
#if LOAD_AUDIO360
        private static IntPtr _audio_dll_ptr = IntPtr.Zero;
#endif
        private static IntPtr get_dll_ptr()
        {
            string plugin_path = dllPath + _dll_name;
#if LOAD_AUDIO360
            string audio_plugin_path = dllPath + _audio360_dll_name;
#endif

            if (_dll_ptr == IntPtr.Zero)
            {
                string dll_path = System.IO.Path.Combine(Application.dataPath, plugin_path).Replace('/', '\\');
#if LOAD_AUDIO360
                string audio_dll_path = System.IO.Path.Combine(Application.dataPath, audio_plugin_path).Replace('/', '\\');
#endif

#if UNITY_EDITOR_WIN
                string log_file_path = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).ToString(), "pv_dbg_file.txt");
#else
                string log_file_path = System.IO.Path.Combine(Application.dataPath, "pv_dbg_file.txt");
#endif

#if LOAD_AUDIO360
                _audio_dll_ptr = LoadLibrary(audio_dll_path);
                if (_audio_dll_ptr == IntPtr.Zero) {
                    int err = Marshal.GetLastWin32Error();
                    Debug.Log("failed to load library: " + audio_plugin_path + ", err: " + err);
                    throw new Exception();
                }
#endif

                _dll_ptr = LoadLibrary(dll_path);
                if (_dll_ptr == IntPtr.Zero)
                {
                    int err = Marshal.GetLastWin32Error();
                    Debug.Log("failed to load library: " + dll_path + ", err: " + err);
                    throw new Exception();
                }
#if UNITY_EDITOR_WIN || UNITY_64
                IntPtr init_addr = GetProcAddress(_dll_ptr, "dll_init");
#else
                IntPtr init_addr = GetProcAddress(_dll_ptr, "_dll_init@16");
#endif
                if (init_addr == IntPtr.Zero)
                {
                    Debug.Log("cannot locate dll_init in library");
                    _dll_ptr = IntPtr.Zero;
                    throw new Exception();
                }
                dll_init_del = (dll_init)Marshal.GetDelegateForFunctionPointer(init_addr, typeof(dll_init));
                string ambisonics_lib_name = "";
#if USE_AUDIO360
                ambisonics_lib_name = _audio360_dll_name;
#endif
                if (dll_init_del(new Texture2D(16, 16).GetNativeTexturePtr(), log_file_path.Replace('/', '\\'), ambisonics_lib_name, reset ? 1 : 0) == 0)
                {
                    Debug.Log("dll_init failed");
                    _dll_ptr = IntPtr.Zero;
                }
            }
            return _dll_ptr;
        }

        private static string get_import_name<T>()
        {
#if UNITY_64
            return typeof(T).Name;
#else
            if (Application.isEditor)
                return typeof(T).Name;
            
            string nm = typeof(T).Name;
            int num_args = 0;
            
            if (nm == "delete_player"
                || nm == "get_media_duration"
                || nm == "get_num_quality_levels"
                || nm == "pause"
                || nm == "play"
                || nm == "shutdown_player")
            {
                num_args = 0;
            }
            else if (nm == "player_update"
                || nm == "set_quality")
            {
                num_args = 1;
            }
                else if (nm == "dll_init"
                || nm == "get_last_error"
                || nm == "set_tile"
                || nm == "seek")
            {
                num_args = 2;
            }
            else if (nm == "get_indexed_quality")
            {
                num_args = 3;
            }
            else if (nm == "set_orientation1")
            {
                num_args = 4;
            }
            else if (nm == "new_player"
                || nm == "get_async_event")
            {
                num_args = 6;
            }
            else
            {
                Debug.Log("function " + nm + " unknown");
                throw new Exception();
            }
            return "_" + nm + "@" + (num_args * 4);
#endif
        }

        // get a delegate for a void-returning function where the 'T' supplied is
        // one of the delegates (we require these delegate names to match the name
        // of the C++ function in the dll)
        private static Delegate GetDelegate<T>()
        {
            IntPtr addr = GetProcAddress(get_dll_ptr(), get_import_name<T>());
            if (addr == IntPtr.Zero)
            {
                Debug.Log("cannot find address for function " + typeof(T).Name + " in dll " + _dll_name);
                throw new Exception();
            }
            return Marshal.GetDelegateForFunctionPointer(addr, typeof(T));
        }

        private static void init_all_dll_delegates()
        {
            new_player_del = (new_player)GetDelegate<new_player>();
            delete_player_del = (delete_player)GetDelegate<delete_player>();
            set_tile_del = (set_tile)GetDelegate<set_tile>();
            play_del = (play)GetDelegate<play>();
            pause_del = (pause)GetDelegate<pause>();
            seek_del = (seek)GetDelegate<seek>();
            get_media_duration_del = (get_media_duration)GetDelegate<get_media_duration>();
            get_last_error_del = (get_last_error)GetDelegate<get_last_error>();
			get_async_event_del = (get_async_event)GetDelegate<get_async_event>();
            shutdown_player_del = (shutdown_player)GetDelegate<shutdown_player>();
            player_update_del = (player_update)GetDelegate<player_update>();
            get_num_quality_levels_del = (get_num_quality_levels)GetDelegate<get_num_quality_levels>();
            get_indexed_quality_del = (get_indexed_quality)GetDelegate<get_indexed_quality>();
            set_quality_del = (set_quality)GetDelegate<set_quality>();
            set_orientation_del = (set_orientation1)GetDelegate<set_orientation1>();
        }

#else
        // running real UWP code were Visual Studio does the build and packaging (and maganges to put the dll in a place where it looks for it during runtime)

        [DllImport(_dll_name)]
        private static extern int dll_init(IntPtr example_texture, string log_file_path, string ambisonics_lib_name, int reset);

        private static int dll_init_del(IntPtr example_texture, string log_file_path, string ambisonics_lib_name, int reset)
        {
            return dll_init(example_texture, log_file_path, ambisonics_lib_name, reset);
        }

        [DllImport(_dll_name)]
        private static extern void new_player([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] string_keys,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] string_vals,
            int string_count,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] int_keys,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.SysInt)] int[] int_vals,
            int int_count);

        private static void new_player_del(string[] string_keys, string[] string_vals, int string_count,
            string[] int_keys, int[] int_vals, int int_count)
        {
            new_player (string_keys, string_vals, string_count, int_keys, int_vals, int_count);
        }

        [DllImport(_dll_name)]
        private static extern void delete_player();

        private static void delete_player_del()
        {
            delete_player();
        }

        [DllImport(_dll_name)]
        private static extern void set_tile(int tile_id, string tile_name);

        private static void set_tile_del(int tile_id, string tile_name)
        {
            set_tile(tile_id, tile_name);
        }

        [DllImport(_dll_name)]
        private static extern void set_orientation1(float x, float y, float z, float w);

        private static void set_orientation_del(float x, float y, float z, float w)
        {
            set_orientation1(x, y, z, w);
        }

        [DllImport(_dll_name)]
        private static extern int play();

        private static int play_del()
        {
            return play ();
        }

        [DllImport(_dll_name)]
        private static extern int pause();

        private static int pause_del()
        {
            return pause ();
        }

        [DllImport(_dll_name)]
        private static extern void seek(int time_in_ms, int approximate);

        private static void seek_del(int time_in_ms, int approximate)
        {
            seek(time_in_ms, approximate);
        }

        [DllImport(_dll_name)]
        private static extern int get_media_duration();

        private static int get_media_duration_del()
        {
            return get_media_duration ();
        }

        [DllImport(_dll_name)]
        private static extern int get_last_error(StringBuilder data, ref int max_len);

        private static int get_last_error_del(StringBuilder data, ref int max_len)
        {
            return get_last_error(data, ref max_len);
        }

        [DllImport(_dll_name)]
        private static extern void get_async_event(StringBuilder key, ref int max_key_len, StringBuilder str_val, ref int max_str_val_len, ref int int_val1, ref int int_val2);

        private static void get_async_event_del(StringBuilder key, ref int max_key_len, StringBuilder str_val, ref int max_str_val_len, ref int int_val1, ref int int_val2)
        {
            get_async_event(key, ref max_key_len, str_val, ref max_str_val_len, ref int_val1, ref int_val2);
        }

        [DllImport(_dll_name)]
        private static extern void shutdown_player();

        private static void shutdown_player_del()
        {
            shutdown_player();
        }

        [DllImport(_dll_name)]
        private static extern int get_num_quality_levels();

        private static int get_num_quality_levels_del()
        {
            return get_num_quality_levels();
        }

        [DllImport(_dll_name)]
        private static extern void get_indexed_quality(int q_index, StringBuilder id_name, ref quality_info qi);

        private static void get_indexed_quality_del(int q_index, StringBuilder id_name, ref quality_info qi)
        {
            get_indexed_quality(q_index, id_name, ref qi);
        }

        [DllImport(_dll_name)]
        private static extern void set_quality(int q_index);

        private static void set_quality_del(int q_index)
        {
            set_quality(q_index);
        }

        // returns a PlayerCode, and sets corresponding pi values
        [DllImport(_dll_name)]
        private static extern int player_update(ref player_info pi);

        private static int player_update_del(ref player_info pi)
        {
            return player_update(ref pi);
        }

#endif

#endregion

        public override CompatiblilityInfo isCompatible 
        {
            get 
            {
                if (Application.platform != RuntimePlatform.WindowsPlayer &&
                    Application.platform != RuntimePlatform.WindowsEditor &&
                    Application.platform != RuntimePlatform.WSAPlayerX64 &&
                    Application.platform != RuntimePlatform.WSAPlayerX86)
                    return new CompatiblilityInfo(false, "WindowsPlayer can only run on Windows");
                if (!SystemInfo.operatingSystem.Contains("Windows 10"))
                    return new CompatiblilityInfo(false, "WindowsPlayer requires Windows 10");
                return new CompatiblilityInfo(true, "");
            } 
        }

        private bool _destroyed = false;
        private Dictionary<string, int> _name_to_int = new Dictionary<string, int>();
        private Dictionary<int, string> _int_to_tile = new Dictionary<int, string>();
        private bool m_ForceFrameSync = false;
        
#region Player Control

        public override bool isYDown()
        {
            return true;
        }
        
        private void DoAwake()
        {
#if !UNITY_WSA_10_0 || UNITY_EDITOR_WIN
            init_all_dll_delegates();
#else
                string ambisonics_lib_name = "";
#if USE_AUDIO360
                ambisonics_lib_name = _audio360_dll_name;
#endif
            if (dll_init_del(new Texture2D(16, 16).GetNativeTexturePtr(), "pv_dbg_file.txt", ambisonics_lib_name, reset ? 1 : 0) == 0)
                Debug.Log("dll_init failed");
#endif
        }
        
        public override void Awake()
        {
            base.Awake ();
            DoAwake();
            reset = true;   // any future call to dll_init will use reset = true;
        }
        
        public override void ResetPlayer()
        {
            // TODO: For now, the Windows player appears to require one turn of the game loop (or so)
            //       before it can safely call Play again. Need to diagnose.
        
            delete_player_del();
        
            // Reset to initial state
            _name_to_int = new Dictionary<string, int>();
            _int_to_tile = new Dictionary<int, string>();
        
            base.ResetPlayer ();
        }
        
        public override void Prepare(Dictionary<string, object> configuration)
        {
            base.Prepare (configuration);
        
            _name_to_int.Clear();
            int tile_id = store_tile(this.requestedTileID);
        
            List<string> string_keys = new List<string>();
            List<string> string_vals = new List<string>();
            List<string> int_keys = new List<string>();
            List<int> int_vals = new List<int>();
        
            string_keys.Add("url"); string_vals.Add(this.sourceUrl);
            int_keys.Add("tile_id"); int_vals.Add(tile_id);
            string_keys.Add("tile_name"); string_vals.Add(this.requestedTileID);
            int_keys.Add("start_time_in_ms"); int_vals.Add(0);
            int_keys.Add("auto_play"); int_vals.Add(0);
        
            // Any additional configuration parameters?
            if (configuration != null) {
        
                // Specific audio out ID? This is the GUID of the IMMDevice for the Windows audio endpoint.
                string audioOutId = (configuration.ContainsKey (AudioOutIdKey) ?
                    configuration [AudioOutIdKey] as string :
                    null);
                if (audioOutId != null) {

                    string_keys.Add("audio_out_id"); string_vals.Add(audioOutId);
                }
        
                m_ForceFrameSync = (configuration.ContainsKey (ForceFrameSyncKey) ?
                    (bool)configuration [ForceFrameSyncKey] :
                    false);

                if (configuration.ContainsKey (PreferYUVBuffersKey) && (bool)configuration[PreferYUVBuffersKey]) {
                    int_keys.Add("use_yuv"); int_vals.Add(1);
                }

                if (configuration.ContainsKey(PreferredLanguageKey))
                {
                    string_keys.Add("requested_audio_lang"); string_vals.Add(new JSONString(((CultureInfo)configuration[PreferredLanguageKey]).TwoLetterISOLanguageName));
                }
                string_keys.Add("default_audio_lang"); string_vals.Add(Thread.CurrentThread.CurrentCulture.Name);

            }
        
            new_player_del(string_keys.ToArray(), string_vals.ToArray(), string_vals.Count, int_keys.ToArray(), int_vals.ToArray(), int_vals.Count);
        }
        
        public override void Play()
        {
            base.Play ();
            if (play_del () != 0) {
                this.isPlaying = true;
                RaisePlay ();
            }
        }
        
        public override void Pause()
        {
            base.Pause ();
            if (pause_del () != 0) {
                this.isPlaying = false;
                RaisePause ();
            }
        }
        
        public override void Seek(double time, bool approximate)
        {
            // Make sure time is within bounds
            time = Math.Max(0.0, Math.Min (time, this.duration));
        
            base.Seek (time, approximate);
            seek_del((int)(time * 1000.0), approximate ? 1 : 0);
        }
        
        public override void SetTileID(string tileID)
        {
            base.SetTileID (tileID);
        
            int tile_id = store_tile(this.requestedTileID);
            set_tile_del(tile_id, this.requestedTileID);
        }
        
        public override void SetQualityGroup(QualityGroup qualityGroup)
        {
            base.SetQualityGroup(qualityGroup);
        
            set_quality_del (qualityGroups.IndexOf(qualityGroup));
        }
        
        protected override void OnEnableAutoQuality()
        {
            base.OnEnableAutoQuality ();
        
            set_quality_del (autoQualityGroupIndex);
        }

        public override void SetAudioOrientation (Vector3 orientation, Quaternion orientationOffset)
        {
            // NOTE: Because of an apparent bug in the Facebook Spatial Audio Rendering SDK 1.3 & 1.4 (reported to Facebook)
            //       where the vertical top/bottom orientation is flipped, we need to reorient the coordinate space to work
            //       around this issue. If later versions of the Audio360.dll fix this problem, this will need to be revisited.
            Vector3 eulerOffset = orientationOffset.eulerAngles;
            Quaternion audioOrientation = Quaternion.Euler(orientation.x + eulerOffset.x, orientation.y - eulerOffset.y, -orientation.z + eulerOffset.z);

            set_orientation_del(audioOrientation.x, audioOrientation.y, audioOrientation.z, audioOrientation.w);
        }
        
#endregion
        
#region Tile ID int-to-string Management
        
        private int store_tile(string id)
        {
            if (!_name_to_int.ContainsKey(id))
                _name_to_int.Add(id, _name_to_int.Count);
            int idIndex = _name_to_int[id];
            _int_to_tile[idIndex] = id;
            return idIndex;
        }
        
#endregion
        
#region Unity Events
        
        private IEnumerator EndOfFrameSync()
        {
            yield return new WaitForEndOfFrame();
        
            // force a small read of the screen's texture.
            // This forces any pending paint operations to complete
            // prior to the read.
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 16, 16), 0, 0);
            tex.Apply();
            Destroy(tex);
        }

        private void HandleAsyncEvent()
        {
            StringBuilder key = new StringBuilder(128);
            int key_len = key.Capacity;
            StringBuilder str_val = new StringBuilder(128);
            int str_val_len = key.Capacity;
            int int_val1 = 0;
            int int_val2 = 0;
            get_async_event_del (key, ref key_len, str_val, ref str_val_len, ref int_val1, ref int_val2);

            String _key = key.ToString ().Substring (0, key_len);
            //String _str_val = str_val.ToString ().Substring (0, str_val_len);	- for when we get events that return keys as strings

            if (_key == "seeked") {
                RaiseSeeked (int_val1 / 1000.0);
            } else if (_key == "io_completed") {
                int bytes_read = int_val1;
                double time_in_secs = int_val2 / 1000.0;
                //Debug.Log ("read " + bytes_read + " bytes in " + time_in_secs + " seconds");
                RaiseIOCompleted (bytes_read, time_in_secs);
            } else if (_key == "stall") {
                // no data, a stall occurred - we are now buffering
                Debug.Log("stalling");
                RaiseStall();
            } else if (_key == "stall_recover") {
                // no data, we recovered from a previous stall and are now playing again
                Debug.Log("recovering from a previous stall");
                RaiseStallRecover();
            } else if (_key == "dropped_frame") {
                // one or more video frames was decoded late, and we skipped them to try to catch up
                // to the audio
                int num_frames_dropped = int_val1;
                Debug.Log("dropped " + num_frames_dropped + " frame" + (num_frames_dropped == 1 ? "" : "s"));
                RaiseDroppedFrame(num_frames_dropped);
            }
        }
        
        void Update ()
        {
            // Force updates to the screen/hmd to happen synchronously.
            // If no game object contains logic like this, it is possible
            // for the gpu operations scheduled by Unity's update pass
            // to not get flushed to the gpu prior to the next update pass.
            // All such passes will eventually get flushed, but the timing
            // of each pass's update can vary without logic of this type.
            // Because the underlying dll contains logic that updates
            // the contents of the video's texture buffer, we need those
            // updates to stay synchronized with screen updates - particularly
            // in any case where we radically change the geometry that
            // we are projecting onto, which can happen when we assign
            // a new value to this.currentTileID.
            if (m_ForceFrameSync)
            {
                StartCoroutine (EndOfFrameSync ());
            }
        
            // Manage plug-in events
            player_info pi = new player_info();
            while (true)
            {
                PlayerCodes pc = (PlayerCodes)player_update_del(ref pi);
                switch (pc)
                {
                case PlayerCodes.kNoOp:
                    return;
        
                case PlayerCodes.kIssuePluginEvent:
                    GL.IssuePluginEvent(pi.plugin_event_proc, pi.plugin_event_code);
                    return;
        
                case PlayerCodes.kMediaEnded:
                    OnLoop ();
                    return;
        
                case PlayerCodes.kError: {
                    StringBuilder err_msg = new StringBuilder(4096);
                    int len = err_msg.Capacity;
                    get_last_error_del(err_msg, ref len); // ignoring return error code for now
                    delete_player_del();
                    this.readyState = ReadyState.Error;
                    RaiseError(err_msg.ToString().Substring(0, len));
                }   break;
        
                case PlayerCodes.kTexturesReady:
        
                    // Capture video width, height, duration, and texture before transitioning to ready state
                    this.videoWidth = pi.textureWidth;
                    this.videoHeight = pi.textureHeight;
                    this.mediaWidth = pi.mediaWidth;
                    this.mediaHeight = pi.mediaHeight;
                    this.duration = get_media_duration_del() / 1000.0;
        
                    // Wrap our shared texture
                    if (pi.nativeTexture2 == IntPtr.Zero) {
                        Texture2D video_texture = Texture2D.CreateExternalTexture (pi.textureWidth, pi.textureHeight, TextureFormat.RGBA32, false, true, pi.nativeTexture1);
                        video_texture.filterMode = FilterMode.Bilinear;
                        video_texture.wrapMode = TextureWrapMode.Clamp;
                        video_texture.anisoLevel = 1;
                        this.texture = video_texture;
                    }
                    else {
                        Texture2D video_texture_Y = Texture2D.CreateExternalTexture (pi.textureWidth, pi.textureHeight, TextureFormat.RGBA32, false, true, pi.nativeTexture1);
                        Texture2D video_texture_UV = Texture2D.CreateExternalTexture (pi.textureWidth/2, pi.textureHeight/2, TextureFormat.RGBA32, false, true, pi.nativeTexture2);
                        video_texture_Y.filterMode = video_texture_UV.filterMode = FilterMode.Bilinear;
                        video_texture_Y.wrapMode = video_texture_UV.wrapMode = TextureWrapMode.Clamp;
                        video_texture_Y.anisoLevel =video_texture_UV.anisoLevel = 1;
                        SetYUVTextures(video_texture_Y, video_texture_UV);
                    }

                    List<QualityGroup> qualityGroups = new List<QualityGroup> ();
        
                    int num_qualities = get_num_quality_levels_del ();
                    for (int i = 0; i < num_qualities; i++) {
                        quality_info qi = new quality_info ();
                        StringBuilder id_name = new StringBuilder (64);
                        qi.id_name_len = id_name.Capacity;
                        qi.width = 0;
                        qi.height = 0;
                        qi.framerate_num = 0;
                        qi.framerate_denom = 1;
                        qi.bandwidth = 0;
                        get_indexed_quality_del (i, id_name, ref qi);
        
                        // Track PlayerBase version of quality group
                        qualityGroups.Add (new QualityGroup (id_name.ToString().Substring(0, qi.id_name_len), qi.width, qi.height, qi.framerate_num, qi.framerate_denom, qi.bandwidth));
                    }
                    this.qualityGroups = qualityGroups;
        
                    // Removing auto quality reporting, as state is maintained in PlayerBase. We can reevaluate later if necessary.
                    //this.autoQuality = pi.is_auto_quality != 0;
        
                    OnPrepared();
        
                    break;
        
                case PlayerCodes.kFirstFrameReady:
        
                    // Update current tile
                    this.currentTileID = _int_to_tile[pi.tile_id];

                    this.mediaWidth = pi.mediaWidth;
                    this.mediaHeight = pi.mediaHeight;
        
                    // Update current quality
                    this.currentQualityGroup = this.qualityGroups [pi.current_quality_level];

                    goto case PlayerCodes.kHaveFrame;
        
                case PlayerCodes.kHaveFrame:
        
                    GL.IssuePluginEvent(pi.plugin_event_proc, pi.plugin_event_code);
                    this.currentTime = pi.current_pos_in_ms / 1000.0;
                    return;
        
                case PlayerCodes.kAsyncNotification:
                    HandleAsyncEvent ();
                    break;
                }
        
            }
        }
        
#endregion
        
        void OnDestroy()
        {
            DoDestroy();
        }
        
        public void DoDestroy()
        {
            if (!_destroyed)
            {
                delete_player_del();
                shutdown_player_del();
                _destroyed = true;

#if !UNITY_WSA_10_0 || UNITY_EDITOR_WIN
                if (_dll_ptr != IntPtr.Zero)
                {
                    FreeLibrary(_dll_ptr);
                    _dll_ptr = IntPtr.Zero;
#if LOAD_AUDIO360

                    FreeLibrary(_audio_dll_ptr);
                    _audio_dll_ptr = IntPtr.Zero;
#endif
                }
#endif
            }
        }
    }
}

#endif
