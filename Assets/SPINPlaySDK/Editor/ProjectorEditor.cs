using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

namespace Pixvana.Video
{

    // For Projector
    [CustomEditor (typeof(Projector), false)]
    public class ProjectorEditor : Editor
    {

        public override void OnInspectorGUI ()
        {
            // Show standard script field
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((Projector)target), typeof(Projector), false);
            GUI.enabled = true;

            Projector projector = (Projector)target;

            SerializedObject serializedObject = new UnityEditor.SerializedObject (projector);

            serializedObject.Update ();

            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_CameraRig"));
            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_Hmd"));
            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_Player"));

            EditorGUILayout.Space ();

            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_SourceUrl"));

            EditorGUILayout.Space ();

            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_RenderScale"));
            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_MonoProjectionScale"));
            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_StereoProjectionScale"));
            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_ForceMonoscopic"));
            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_ResetForwardOrientation"));
            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_ProjectionMaterial"));

            serializedObject.ApplyModifiedProperties ();
        }
    }
}
