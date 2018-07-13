using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

namespace Pixvana.VR
{
    
    // For Hmd and subclasses
    [CustomEditor (typeof(Hmd), true)]
    public class HmdEditor : Editor
    {
        
        public override void OnInspectorGUI ()
        {
            // Show standard script field
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((Hmd)target), typeof(Hmd), false);
            GUI.enabled = true;

            Hmd hmd = (Hmd)target;

            SerializedObject serializedObject = new UnityEditor.SerializedObject (hmd);

            serializedObject.Update ();

            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_UseNativeIntegration"));

            // Is native integration available but not enabled?
            if (!hmd.nativeIntegrationAvailable &&
                hmd.useNativeIntegration) {

                EditorGUILayout.HelpBox ("To use native integration, uncomment #defines at the top of Hmd.cs.", MessageType.None);
            }

            serializedObject.ApplyModifiedProperties ();
        }
    }
}
