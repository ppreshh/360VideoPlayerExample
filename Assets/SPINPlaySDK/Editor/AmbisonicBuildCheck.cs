#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

class AmbisonicBuildCheck : IPreprocessBuild
{
    class DevicePluginExclusion
    {
        public List<string> devices = new List<string>();
        public List<string> pluginFileNames = new List<string>();
    }

    private const string ANDROID_PLUGINS_PATH = "Assets/SPINPlaySDK/Plugins/Android";  // Path to the SPIN Play SDK Android plugins folder
    private const string GOOGLE_VR_SDK_PATH = "Assets/GoogleVR";                       // Default path to the Google VR SDK for Unity

    // Unity device name strings
    private const string ALL_DEVICE_NAME = "(all)";                                    // Not a real device name...matches all devices
    private const string OCULUS_DEVICE_NAME = "Oculus";
    private const string DAYDREAM_DEVICE_NAME = "daydream";
    private const string CARDBOARD_DEVICE_NAME = "cardboard";

    private List<DevicePluginExclusion> m_DevicePluginExclusions = new List<DevicePluginExclusion>();

    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildTarget target, string path) {

        if (target == BuildTarget.Android) {

            // Get our currently-enabled devices
            string[] enabledDevices = UnityEditorInternal.VR.VREditor.GetVREnabledDevicesOnTargetGroup (BuildTargetGroup.Android);

            ConfigureAndroidPluginExclusions ();

            foreach (DevicePluginExclusion devicePluginExclusion in m_DevicePluginExclusions) {

                bool enable = true;
                foreach (string device in enabledDevices) {

                    if (devicePluginExclusion.devices.Contains(ALL_DEVICE_NAME) ||
                        devicePluginExclusion.devices.Contains (device)) {

                        enable = false;
                        break;
                    }
                }

                ExcludePlugins (target, devicePluginExclusion.pluginFileNames, enable);
            }
        }
    }

    private void ConfigureAndroidPluginExclusions()
    {
        // There's no great way to determine if the GoogleVR SDK for Unity has been imported, so we'll simply
        // check to see if the standard assets folder exists.
        bool googleVrSdkImported = System.IO.Directory.Exists (GOOGLE_VR_SDK_PATH);

        // If Google Daydream or Cardboard support is enabled in Unity, or if the GoogleVR SDK for Unity has
        // been imported, we can't include the Google VR common or base libraries because they will conflict.
        m_DevicePluginExclusions.Add(new DevicePluginExclusion() {
            devices = (googleVrSdkImported ?
                new List<string>() { ALL_DEVICE_NAME } :
                new List<string>() { DAYDREAM_DEVICE_NAME, CARDBOARD_DEVICE_NAME }
            ),
            pluginFileNames = new List<string>() { "sdk-common-1.40.0.aar", "sdk-base-1.40.0.aar" } 
        });
    }

    private void ExcludePlugins(BuildTarget target, List<string> pluginFileNames, bool enable)
    {
        foreach (string pluginFileName in pluginFileNames) {

            PluginImporter plugin = AssetImporter.GetAtPath (string.Format ("{0}/{1}", ANDROID_PLUGINS_PATH, pluginFileName)) as PluginImporter;
            if (plugin != null) {

                plugin.SetCompatibleWithPlatform (target, enable);
            }
        }
    }
}
#endif