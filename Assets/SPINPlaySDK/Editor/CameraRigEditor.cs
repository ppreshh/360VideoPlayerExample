using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

namespace Pixvana.Video
{

    // For CameraRig
    [CustomEditor (typeof(CameraRig), false)]
    public class CameraRigEditor : Editor
    {

        public override void OnInspectorGUI ()
        {
            // Show standard script field
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((CameraRig)target), typeof(CameraRig), false);
            GUI.enabled = true;

            CameraRig cameraRig = (CameraRig)target;

            SerializedObject serializedObject = new UnityEditor.SerializedObject (cameraRig);

            serializedObject.Update ();

            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_Depth"));

            EditorGUILayout.Space ();

            // NOTE: Unity's user-defined layers start at index 8

            cameraRig.leftEyeLayerIndex = EditorGUILayout.LayerField ("Left Eye Layer", cameraRig.leftEyeLayerIndex);
            if (cameraRig.leftEyeLayerIndex < 8) {

                EditorGUILayout.HelpBox ("Add or set a user-defined layer for the left eye.", MessageType.Warning);
            }

            cameraRig.rightEyeLayerIndex = EditorGUILayout.LayerField ("Right Eye Layer", cameraRig.rightEyeLayerIndex);
            if (cameraRig.rightEyeLayerIndex < 8) {

                EditorGUILayout.HelpBox ("Add or set a user-defined layer for the right eye.", MessageType.Warning);
            }

            // Make sure that the left and right user-defined layers are unique
            if (cameraRig.leftEyeLayerIndex > 7 &&
                cameraRig.rightEyeLayerIndex > 7 &&
                cameraRig.leftEyeLayerIndex == cameraRig.rightEyeLayerIndex) {

                EditorGUILayout.HelpBox ("Left and right eye layers must be unique for correct stereo playback.", MessageType.Warning);
            }

            EditorGUILayout.Space ();

            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_Hmd"));
            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_LockToHmd"));

            serializedObject.ApplyModifiedProperties ();
        }
    }
}
