using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pixvana
{

    [InitializeOnLoad]
    public class Settings : EditorWindow
    {

        private class Setting
        {
            public delegate void SetRecommended ();

            private const string ignoreKeyPrefix                = "spinPlaySDK.ignore.";

            private BuildTarget[] m_BuildTargets = null;

			private string m_Name = null;
			public string name { get { return m_Name; } }

            private string m_IgnoreKey = null;
            private Func<bool> m_UsesRecommendedValue = null;
            private SetRecommended m_setRecommendedValue = null;

			private Func<string> m_RecommendedValueString = null;
			public string recommendedValueString { get { return (m_RecommendedValueString ()); } }

			private Func<string> m_ValueString = null;
			public string valueString { get { return (m_ValueString ()); } }

			public bool isIgnored { get { return (EditorPrefs.HasKey (ignoreKeyPrefix + m_IgnoreKey)); } }
            public bool usesRecommendedValue { get { return (m_UsesRecommendedValue()); } }
            public bool appliesToCurrentBuildTarget { get { return (m_BuildTargets == null ? true : Array.Exists<BuildTarget> (m_BuildTargets, element => element == EditorUserBuildSettings.activeBuildTarget)); } }

			public Setting(
                BuildTarget[] buildTargets,
				string name,
				string ignoreKey,
				Func<string> recommendedValueString,
				Func<string> valueString,
				Func<bool> usesRecommendedValue,
				SetRecommended setRecommendedValue) {

				Debug.Assert (!string.IsNullOrEmpty (name), "name is required");
                Debug.Assert (!string.IsNullOrEmpty (ignoreKey), "ignoreKey is required");
				Debug.Assert (recommendedValueString != null, "recommendedValueString cannot be null");
				Debug.Assert (valueString != null, "valueString cannot be null");
                Debug.Assert (usesRecommendedValue != null, "usesRecommendedValue cannot be null");
                Debug.Assert (setRecommendedValue != null, "setRecommendedValue cannot be null");

                m_BuildTargets = buildTargets;
				m_Name = name;
                m_IgnoreKey = ignoreKey;
				m_RecommendedValueString = recommendedValueString;
				m_ValueString = valueString;
                m_UsesRecommendedValue = usesRecommendedValue;
                m_setRecommendedValue = setRecommendedValue;
            }

            public void SetRecommendedValue() {

				// Only set if the value isn't being ignored and the value isn't already correct
				if (!this.isIgnored &&
					!this.usesRecommendedValue)
				{
					m_setRecommendedValue();
				}
            }

            public void Ignore() {

				// Only ignore if the value hasn't already been ignored
				if (!this.isIgnored)
				{
					EditorPrefs.SetBool(ignoreKeyPrefix + m_IgnoreKey, true);
				}
            }

            public void ClearIgnore() {

                EditorPrefs.DeleteKey (ignoreKeyPrefix + m_IgnoreKey);
            }
        }

        #region UI Strings

        private const string useRecommended                             = "Use recommended ({0})";
        private const string currentValue                               = " (current = {0})";

        #endregion

        #region Editor Preference Keys

        private const string buildTargetKey                             = "buildTarget";
        private const string virtualRealitySupportedKey                 = "virtualRealitySupported";
        private const string showUnitySplashScreenKey                   = "showUnitySplashScreen";
        private const string defaultIsFullScreenKey                     = "defaultIsFullScreen";
        private const string runInBackgroundKey                         = "runInBackground";
        private const string displayResolutionDialogKey                 = "displayResolutionDialog";
        private const string resizableWindowKey                         = "resizableWindow";
        private const string visibleInBackgroundKey                     = "visibleInBackground";
        private const string colorSpaceKey                              = "colorSpace";
        private const string multithreadedRenderingKey                  = "multithreadedRendering";
        private const string recommendedAndroidSdkVersionKey            = "androidRecommendedSdkVersion";
        private const string anisoLevelKey                              = "anisoFilter";

        #endregion

        #region Default Settings Values

        private const BuildTarget recommendedBuildTarget                = BuildTarget.StandaloneWindows64;
        private const bool recommendedVirtualRealitySupported           = true;
        private const bool recommendedShowUnitySplashScreen             = false;
        private const bool recommendedDefaultIsFullScreen               = false;
        private const bool recommendedRunInBackground                   = true;
        private const ResolutionDialogSetting recommendedDisplayResolutionDialog = ResolutionDialogSetting.HiddenByDefault;
        private const bool recommendedResizableWindow                   = true;
        private const bool recommendedVisibleInBackground               = true;
        private const ColorSpace recommendedColorSpace                  = ColorSpace.Gamma;
        private const bool recommendedMultithreadedRendering            = false;
        private const AndroidSdkVersions recommendedAndroidSdkVersion   = AndroidSdkVersions.AndroidApiLevel23;
        private const AnisotropicFiltering recommendedAnisotropicFiltering = AnisotropicFiltering.Enable;

        #endregion

        private static List<Setting> m_Settings = null;
		private static Settings m_SettingsWindow;

        static Settings()
        {
            EditorApplication.update += Update;
        }

        [MenuItem("Window/SPIN Play SDK")]
        static void ShowWindow()
        {
            m_SettingsWindow = GetWindow<Settings> (true);
            m_SettingsWindow.minSize = new Vector2 (325.0f, 550.0f);
            //                settingsWindow.titleContent = ...
        }

        Vector2 scrollPosition;

        string GetResourcePath()
        {
            MonoScript ms = MonoScript.FromScriptableObject(this);
            string path = AssetDatabase.GetAssetPath(ms);
            path = Path.GetDirectoryName(path);
            return path.Substring(0, path.Length - "Editor".Length) + "Textures/";
        }

        private static void ConfigureSettings()
        {

            if (m_Settings != null) {
                
                return;
            }

            m_Settings = new List<Setting> ();

            
            //#if UNITY_STANDALONE_WIN
            //// EditorUserBuildSettings.activeBuildTarget
            //m_Settings.Add(new Setting("Build Target", buildTargetKey,
            //    () => { return recommendedBuildTarget.ToString(); },
            //    () => { return EditorUserBuildSettings.activeBuildTarget.ToString(); },
            //    () => { return (EditorUserBuildSettings.activeBuildTarget == recommendedBuildTarget); },
            //    () => { EditorUserBuildSettings.SwitchActiveBuildTarget(recommendedBuildTarget); }
            //));
            //#endif

            // PlayerSettings.virtualRealitySupported
            m_Settings.Add(new Setting(null,
                "Virtual Reality Supported", virtualRealitySupportedKey,
                () => { return recommendedVirtualRealitySupported.ToString(); },
                () => { return PlayerSettings.virtualRealitySupported.ToString(); },
                () => { return (PlayerSettings.virtualRealitySupported == recommendedVirtualRealitySupported); },
                () => { PlayerSettings.virtualRealitySupported = recommendedVirtualRealitySupported; }
            ));

            // The Unity splashscreen can only be controlled in the pro/paid versions
            if (Application.HasProLicense ()) {
                // PlayerSettings.showUnitySplashScreen
                m_Settings.Add (new Setting (null,
                    "Show Unity Splash Screen", showUnitySplashScreenKey,
					() => { return recommendedShowUnitySplashScreen.ToString (); },
					#if (UNITY_5_4 || UNITY_5_3)
					() => { return PlayerSettings.showUnitySplashScreen.ToString (); },
					() => { return (PlayerSettings.showUnitySplashScreen == recommendedShowUnitySplashScreen); },
					() => { PlayerSettings.showUnitySplashScreen = recommendedShowUnitySplashScreen; }
					#else
					() => { return PlayerSettings.SplashScreen.show.ToString (); },
					() => { return (PlayerSettings.SplashScreen.show == recommendedShowUnitySplashScreen); },
					() => { PlayerSettings.SplashScreen.show = recommendedShowUnitySplashScreen; }
					#endif
				));
            }

            // PlayerSettings.defaultIsFullScreen
            m_Settings.Add(new Setting(new BuildTarget[] { BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64 },
                "Default is Fullscreen", defaultIsFullScreenKey,
                () => { return recommendedDefaultIsFullScreen.ToString(); },
                () => { return PlayerSettings.defaultIsFullScreen.ToString(); },
                () => { return (PlayerSettings.defaultIsFullScreen == recommendedDefaultIsFullScreen); },
                () => { PlayerSettings.defaultIsFullScreen = recommendedDefaultIsFullScreen; }
            ));

            // PlayerSettings.runInBackground
            m_Settings.Add(new Setting(new BuildTarget[] { BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64 },
                "Run In Background", runInBackgroundKey,
                () => { return recommendedRunInBackground.ToString(); },
                () => { return PlayerSettings.runInBackground.ToString(); },
                () => { return (PlayerSettings.runInBackground == recommendedRunInBackground); },
                () => { PlayerSettings.runInBackground = recommendedRunInBackground; }
            ));

            // PlayerSettings.displayResolutionDialog
            m_Settings.Add(new Setting(new BuildTarget[] { BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64 }, 
                "Display Resolution Dialog", displayResolutionDialogKey,
                () => { return recommendedDisplayResolutionDialog.ToString(); },
                () => { return PlayerSettings.displayResolutionDialog.ToString(); },
                () => { return (PlayerSettings.displayResolutionDialog == recommendedDisplayResolutionDialog); },
                () => { PlayerSettings.displayResolutionDialog = recommendedDisplayResolutionDialog; }
            ));

            // PlayerSettings.resizableWindow
            m_Settings.Add(new Setting(new BuildTarget[] { BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64 }, 
                "Resizable Window", resizableWindowKey,
                () => { return recommendedResizableWindow.ToString(); },
                () => { return PlayerSettings.resizableWindow.ToString(); },
                () => { return (PlayerSettings.resizableWindow == recommendedResizableWindow); },
                () => { PlayerSettings.resizableWindow = recommendedResizableWindow; }
            ));

            // PlayerSettings.visibleInBackground
            m_Settings.Add(new Setting(new BuildTarget[] { BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64 }, 
                "Visible In Background", visibleInBackgroundKey,
                () => { return recommendedVisibleInBackground.ToString(); },
                () => { return PlayerSettings.visibleInBackground.ToString(); },
                () => { return (PlayerSettings.visibleInBackground == recommendedVisibleInBackground); },
                () => { PlayerSettings.visibleInBackground = recommendedVisibleInBackground; }
            ));

            // PlayerSettings.colorSpace
            m_Settings.Add(new Setting(null, 
                "Color Space", colorSpaceKey,
                () => { return recommendedColorSpace.ToString(); },
                () => { return PlayerSettings.colorSpace.ToString(); },
                () => { return (PlayerSettings.colorSpace == recommendedColorSpace); },
                () => { PlayerSettings.colorSpace = recommendedColorSpace; }
            ));

            // PlayerSettings.MTRendering
            m_Settings.Add(new Setting(new BuildTarget[] { BuildTarget.Android }, 
                "Multithreaded Rendering", multithreadedRenderingKey,
                () => { return recommendedMultithreadedRendering.ToString(); },
                () => { return PlayerSettings.MTRendering.ToString(); },
                () => { return (PlayerSettings.MTRendering == recommendedMultithreadedRendering); },
                () => { PlayerSettings.MTRendering = recommendedMultithreadedRendering; }
            ));

            // PlayerSettings.Android.minSdkVersion
            m_Settings.Add(new Setting(new BuildTarget[] { BuildTarget.Android }, 
                "Minimum API Level", recommendedAndroidSdkVersionKey,
                () => { return recommendedAndroidSdkVersion.ToString(); },
                () => { return PlayerSettings.Android.minSdkVersion.ToString(); },
                () => { return (PlayerSettings.Android.minSdkVersion >= recommendedAndroidSdkVersion); },
                () => { PlayerSettings.Android.minSdkVersion = recommendedAndroidSdkVersion; }
            ));

            m_Settings.Add(new Setting(null,
                "Anisotropic Textures", anisoLevelKey,
                () => { return recommendedAnisotropicFiltering.ToString(); },
                () => { return QualitySettings.anisotropicFiltering.ToString(); },
                () => { return (QualitySettings.anisotropicFiltering == recommendedAnisotropicFiltering); },
                () => { QualitySettings.anisotropicFiltering = recommendedAnisotropicFiltering; }
               ));
        }

        static void Update()
        {
            ConfigureSettings ();

            // Do we need to show the settings window?
            foreach (Setting setting in m_Settings)
            {
                if (!setting.isIgnored && setting.appliesToCurrentBuildTarget && !setting.usesRecommendedValue)
                {
                    ShowWindow();
                    break;
                }
            }

            EditorApplication.update -= Update;
        }

        void OnEnable()
        {
            this.titleContent = new GUIContent("SPIN Play SDK Settings");
        }

        public void OnGUI()
        {
            var resourcePath = GetResourcePath();
            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(resourcePath + "logo.png");
            var rect = GUILayoutUtility.GetRect(position.width, 150, GUI.skin.box);
            if (logo)
                GUI.DrawTexture(rect, logo, ScaleMode.ScaleToFit);

            EditorGUILayout.HelpBox("Recommended settings for the SPIN Play SDK", MessageType.Warning);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

			int numItems = 0;
			foreach (Setting setting in m_Settings)
			{
                if (!setting.isIgnored && setting.appliesToCurrentBuildTarget && !setting.usesRecommendedValue)
				{
					numItems++;

					GUILayout.Label(setting.name + string.Format(currentValue, setting.valueString));

					GUILayout.BeginHorizontal();

					if (GUILayout.Button(string.Format (useRecommended, setting.recommendedValueString)))
					{
						setting.SetRecommendedValue();
					}

					GUILayout.FlexibleSpace();

					if (GUILayout.Button("Ignore"))
					{
						setting.Ignore();
					}

					GUILayout.EndHorizontal();

				}
			}

			// Begin global buttons

            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear All Ignores"))
            {
				foreach (Setting setting in m_Settings)
				{
                    if (setting.appliesToCurrentBuildTarget)
                    {
                        setting.ClearIgnore ();
                    }
				}
            }

            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();

            if (numItems > 0)
            {
                if (GUILayout.Button("Accept All"))
                {
					foreach (Setting setting in m_Settings)
					{
                        if (setting.appliesToCurrentBuildTarget) 
                        {
                            setting.SetRecommendedValue ();			
                        }
					}

                    EditorUtility.DisplayDialog("Accept All", "All recommended settings have been applied.", "OK");

                    Close();
                }

                if (GUILayout.Button("Ignore All"))
                {
                    if (EditorUtility.DisplayDialog("Ignore All", "Are you sure?", "Ignore All", "Cancel"))
                    {
						foreach (Setting setting in m_Settings)
						{
                            if (setting.appliesToCurrentBuildTarget)
                            {
                                setting.Ignore ();
                            }
						}

                        Close();
                    }
                }
            }
            else if (GUILayout.Button("Close"))
            {
                Close();
            }

            GUILayout.EndHorizontal();
        }
    }
}