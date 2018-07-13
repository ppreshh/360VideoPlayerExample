using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

namespace Pixvana.Video
{
    
    // For PlayerBase and subclasses
    [CustomEditor (typeof(PlayerBase), true)]
    public class PlayerBaseEditor : Editor
    {
        
        public override void OnInspectorGUI ()
        {
            // Show standard script field
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((PlayerBase)target), typeof(PlayerBase), false);
            GUI.enabled = true;

            PlayerBase playerBase = (PlayerBase)target;

            // Is a projector attached, enabled, and controlling this player?
            Projector projector = playerBase.GetComponent<Projector> ();
            bool isProjectorInControl = (projector != null && projector.isActiveAndEnabled && projector.player == playerBase);

            SerializedObject serializedObject = new UnityEditor.SerializedObject (playerBase);

            serializedObject.Update ();

            if (isProjectorInControl) {
                
                EditorGUILayout.HelpBox ("Some values driven by Projector.", MessageType.None);
            }

            EditorGUI.BeginDisabledGroup (isProjectorInControl);

            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_SourceUrl"));

            EditorGUILayout.Space ();

            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_TargetObjects"), true);

            EditorGUI.EndDisabledGroup ();

            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_AutoPlay"));
            EditorGUILayout.PropertyField (serializedObject.FindProperty ("m_Loop"));

            serializedObject.ApplyModifiedProperties ();
        }
    }
}
