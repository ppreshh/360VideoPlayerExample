using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pixvana
{
    public class ViveInputSettings
    {
        private const string ViveTriggerName                = "ViveTrigger";
        private const string ViveMenuButtonName             = "ViveMenuButton";
        private const string ViveLeftPadButtonName          = "ViveLeftPadButton";
        private const string ViveRightPadButtonName         = "ViveRightPadButton";
        private const string ViveLeftPadHorizontalAxisName  = "ViveLeftPadHorizontalAxis";
        private const string ViveRightPadHorizontalAxisName = "ViveRightPadHorizontalAxis";
        private const string ViveLeftPadVerticalAxisName    = "ViveLeftPadVerticalAxis";
        private const string ViveRightPadVerticalAxisName   = "ViveRightPadVerticalAxis";

        [MenuItem("Tools/SPIN Play SDK/Add Vive InputManager axes")]
        private static void ViveInputSettingsMenuOption()
        {
            EnforceInputManagerBindings ();
        }

        private static void EnforceInputManagerBindings()
        {
            try
            {
                BindAxis(new Axis() {
                    name = ViveTriggerName,
                    positiveButton = "joystick button 14",
                    altPositiveButton = "joystick button 15"
                });
                BindAxis(new Axis() {
                    name = ViveMenuButtonName,
                    positiveButton = "joystick button 0",
                    altPositiveButton = "joystick button 2"
                });
                BindAxis(new Axis() {
                    name = ViveLeftPadButtonName,
                    positiveButton = "joystick button 8"
                });
                BindAxis(new Axis() {
                    name = ViveRightPadButtonName,
                    positiveButton = "joystick button 9"
                });
                BindAxis(new Axis() {
                    name = ViveLeftPadHorizontalAxisName,
                    sensitivity = 1,
                    type = 2,
                    axis = 0
                });
                BindAxis(new Axis() {
                    name = ViveRightPadHorizontalAxisName,
                    sensitivity = 1,
                    type = 2,
                    axis = 3
                });
                BindAxis(new Axis() {
                    name = ViveLeftPadVerticalAxisName,
                    sensitivity = 1,
                    type = 2,
                    axis = 1
                });
                BindAxis(new Axis() {
                    name = ViveRightPadVerticalAxisName,
                    sensitivity = 1,
                    type = 2,
                    axis = 4
                });
            }
            catch
            {
                Debug.LogError("Failed to apply Vive input manager bindings.");
            }
        }

        private class Axis
        {
            public string name = String.Empty;
            public string descriptiveName = String.Empty;
            public string descriptiveNegativeName = String.Empty;
            public string negativeButton = String.Empty;
            public string positiveButton = String.Empty;
            public string altNegativeButton = String.Empty;
            public string altPositiveButton = String.Empty;
            public float gravity = 1000.0f;
            public float dead = 0.001f;
            public float sensitivity = 1000.0f;
            public bool snap = false;
            public bool invert = false;
            public int type = 0; // 0 = key or mouse button, 1 = mouse movement, 2 = joystick axis
            public int axis = 0; // 0 = x axis, 1 = y axis, otherwise axis index
            public int joyNum = 0;
        }

        private static void BindAxis(Axis axis)
        {
            SerializedObject serializedObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
            SerializedProperty axesProperty = serializedObject.FindProperty("m_Axes");

            SerializedProperty axisIter = axesProperty.Copy();
            axisIter.Next(true);
            axisIter.Next(true);
            while (axisIter.Next(false))
            {
                if (axisIter.FindPropertyRelative("m_Name").stringValue == axis.name)
                {
                    // Axis already exists. Don't create binding.
                    return;
                }
            }

            axesProperty.arraySize++;
            serializedObject.ApplyModifiedProperties();

            SerializedProperty axisProperty = axesProperty.GetArrayElementAtIndex(axesProperty.arraySize - 1);
            axisProperty.FindPropertyRelative("m_Name").stringValue = axis.name;
            axisProperty.FindPropertyRelative("descriptiveName").stringValue = axis.descriptiveName;
            axisProperty.FindPropertyRelative("descriptiveNegativeName").stringValue = axis.descriptiveNegativeName;
            axisProperty.FindPropertyRelative("negativeButton").stringValue = axis.negativeButton;
            axisProperty.FindPropertyRelative("positiveButton").stringValue = axis.positiveButton;
            axisProperty.FindPropertyRelative("altNegativeButton").stringValue = axis.altNegativeButton;
            axisProperty.FindPropertyRelative("altPositiveButton").stringValue = axis.altPositiveButton;
            axisProperty.FindPropertyRelative("gravity").floatValue = axis.gravity;
            axisProperty.FindPropertyRelative("dead").floatValue = axis.dead;
            axisProperty.FindPropertyRelative("sensitivity").floatValue = axis.sensitivity;
            axisProperty.FindPropertyRelative("snap").boolValue = axis.snap;
            axisProperty.FindPropertyRelative("invert").boolValue = axis.invert;
            axisProperty.FindPropertyRelative("type").intValue = axis.type;
            axisProperty.FindPropertyRelative("axis").intValue = axis.axis;
            axisProperty.FindPropertyRelative("joyNum").intValue = axis.joyNum;
            serializedObject.ApplyModifiedProperties();
        }
    }
}