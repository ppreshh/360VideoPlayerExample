// To avoid modifying this file, these can also be defined under Edit/Project Settings/Player/Other Settings/Scripting Define Symbols
//#define OCULUS_UTILITIES_INSTALLED      // Uncomment if Oculus Utilities are installed
//#define STEAMVR_INSTALLED               // Uncomment if SteamVR is installed
//#define GOOGLE_VR_INSTALLED             // Uncomment if Google VR SDK is installed

// NOTE: This controller manager effectively ties input method(s) to HMD type. This is an issue
//       that will need to be resolved (for example, an Xbox controller can be used with both
//       an Oculus Rift and a Windows Mixed Reality device like the Acer or HP units).

using Pixvana.VR;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pixvana.Extras
{
    public class ControllerManager : MonoBehaviour {

        /// <summary>
        /// Command event arguments.
        /// </summary>
        public class CommandEventArgs : EventArgs
        {
            private Command m_Command = Command.None;

            /// <summary>
            /// Initializes a new instance of the <see cref="Pixvana.Extras.ControllerManager+CommandEventArgs"/> class.
            /// </summary>
            /// <param name="command">The command.</param>
            public CommandEventArgs(Command command) {

                m_Command = command;
            }

            /// <summary>
            /// Gets the <see cref="Command"/>.
            /// </summary>
            /// <value>The command.</value>
            public Command command { get { return m_Command; } }
        }

        // NOTE: These must match the name values under Edit/Project Settings/Input in Unity.
        //       They must also match ViveInputSettings.cs consts (would be nice to share).
        private const string ViveTriggerName = "ViveTrigger";
        private const string ViveMenuButtonName = "ViveMenuButton";
        private const string ViveLeftPadButtonName = "ViveLeftPadButton";
        private const string ViveRightPadButtonName = "ViveRightPadButton";
        private const string ViveLeftPadHorizontalAxisName = "ViveLeftPadHorizontalAxis";
        private const string ViveRightPadHorizontalAxisName = "ViveRightPadHorizontalAxis";
        private const string ViveLeftPadVerticalAxisName = "ViveLeftPadVerticalAxis";
        private const string ViveRightPadVerticalAxisName = "ViveRightPadVerticalAxis";

        #if GOOGLE_VR_INSTALLED
        // The Daydream controller returns its touch position as a Vector2 where X and Y range
        // from 0 to 1. (0, 0) is the top left of the touchpad, and (1, 1) is the bottom right
        // of the touchpad. From: https://developers.google.com/vr/unity/controller-basics.
        // Because our directional calculations depend on the center being at 0, 0, we apply an offset.
        private static Vector2 m_DaydreamTouchPadOffset = new Vector2 (-0.5f, -0.5f);
        #endif

        public static ControllerManager instance = null;

        private HmdType m_HmdType = HmdType.Unknown;
        public HmdType hmdType {

            get { return m_HmdType; }

            set {

                Debug.Assert (hmdType < HmdType.Max_HmdType, "Unrecognized HMD type");

                m_HmdType = value;

                #if OCULUS_UTILITIES_INSTALLED
                if (m_HmdType == HmdType.OculusGearVR ||
                    m_HmdType == HmdType.OculusGo) {

                OVRTouchpad.Create ();
                OVRTouchpad.TouchHandler += GearVRTouchHandler;
                }
                #endif
            }
        }

        public event EventHandler<CommandEventArgs> onCommand;

        private enum Direction
        {
            Unknown,
            Center,
            Left,
            Right,
            Up,
            Down
        }

        // NOTE: For now, these are very specific to this project. This will eventually be replaced by a more
        //       robust controller manager.
        public enum Command
        {
            None,
            Select,     // Used to select items and to toggle play/pause
            Menu,       // Used to toggle the menu (thumbnail view)
            Home,
            End,
            Left,
            Right,
            Up,
            Down,
            Exit
        }

        #if OCULUS_UTILITIES_INSTALLED
        public void GearVRTouchHandler (object sender, System.EventArgs e)
        {
            OVRTouchpad.TouchArgs touchArgs = (OVRTouchpad.TouchArgs)e;

            Debug.Log ("TOUCH: " + touchArgs.TouchType);

            Command command = Command.None;

            switch (touchArgs.TouchType) {

            case OVRTouchpad.TouchEvent.SingleTap:
                {
                    command = Command.Select;
                    break;
                }
            case OVRTouchpad.TouchEvent.Left:
                {
                    // Opposite of what we'd expect
                    command = Command.Right;
                    break;
                }
            case OVRTouchpad.TouchEvent.Right:
                {
                    // Opposite of what we'd expect
                    command = Command.Left;
                    break;
                }
            case OVRTouchpad.TouchEvent.Up:
                {
                    command = Command.Up;
                    break;
                }
            case OVRTouchpad.TouchEvent.Down:
                {
                    command = Command.Down;
                    break;
                }
            }

            RaiseCommand (command);
        }
        #endif

        public Command GetCommand () 
        {
            
            Command command = Command.None;

            // Check keyboard first
            if (Input.GetKeyDown (KeyCode.Space) ||
                Input.GetKeyDown (KeyCode.Return) ||
                Input.GetKeyDown (KeyCode.KeypadEnter)) {

                command = Command.Select;

            } else if (Input.GetKeyDown (KeyCode.Backspace)) {

                command = Command.Menu;

            } else if (Input.GetKeyDown (KeyCode.Home)) {
              
                command = Command.Home;

            } else if (Input.GetKeyDown (KeyCode.End)) {

                command = Command.End;

            } else if (Input.GetKeyDown (KeyCode.LeftArrow)) {

                command = Command.Left;

            } else if (Input.GetKeyDown (KeyCode.RightArrow)) {

                command = Command.Right;

            } else if (Input.GetKeyDown (KeyCode.UpArrow) ||
                     Input.GetKeyDown (KeyCode.PageUp)) {

                command = Command.Up;

            } else if (Input.GetKeyDown (KeyCode.DownArrow) ||
                     Input.GetKeyDown (KeyCode.PageDown)) {

                command = Command.Down;

            } else if (Input.GetKeyDown (KeyCode.Escape)) {

                command = Command.Exit;
            }

            // Still need to look for a command?
            if (command == Command.None) {
                
                switch (m_HmdType) {

                case HmdType.HTCVive:
                    {

                        if (Input.GetButtonDown (ViveTriggerName)) {
                        
                            command = Command.Select;

                        } else if (Input.GetButtonDown (ViveMenuButtonName)) {

                            command = Command.Menu;

                        } else if (Input.GetButtonDown (ViveLeftPadButtonName)) {

                            command = GetCommandForDirection (GetDirection (new Vector2 (Input.GetAxis (ViveLeftPadHorizontalAxisName), Input.GetAxis (ViveLeftPadVerticalAxisName)), 0.35f));

                        } else if (Input.GetButtonDown (ViveRightPadButtonName)) {

                            command = GetCommandForDirection (GetDirection (new Vector2 (Input.GetAxis (ViveRightPadHorizontalAxisName), Input.GetAxis (ViveRightPadVerticalAxisName)), 0.35f));
                        }

                        break;
                    }

                case HmdType.OculusRift:
                    {
                        #if OCULUS_UTILITIES_INSTALLED

                        if (OVRInput.GetDown (OVRInput.Button.One)) {

                            command = Command.Select;

                        } else if (OVRInput.GetDown (OVRInput.Button.Two)) {

                            command = Command.Menu;

                        } else if (OVRInput.GetDown (OVRInput.Button.Left) ||
                            OVRInput.GetDown (OVRInput.Button.DpadLeft) ||
                            OVRInput.GetDown (OVRInput.Button.PrimaryThumbstickLeft) ||
                            OVRInput.GetDown (OVRInput.Button.SecondaryThumbstickLeft)) {

                            command = Command.Left;

                        } else if (OVRInput.GetDown (OVRInput.Button.Right) ||
                            OVRInput.GetDown (OVRInput.Button.DpadRight) ||
                            OVRInput.GetDown (OVRInput.Button.PrimaryThumbstickRight) ||
                            OVRInput.GetDown (OVRInput.Button.SecondaryThumbstickRight)) {

                            command = Command.Right;

                        } else if (OVRInput.GetDown (OVRInput.Button.Up) ||
                            OVRInput.GetDown (OVRInput.Button.DpadUp) ||
                            OVRInput.GetDown (OVRInput.Button.PrimaryThumbstickUp) ||
                            OVRInput.GetDown (OVRInput.Button.SecondaryThumbstickUp)) {

                            command = Command.Up;

                        } else if (OVRInput.GetDown (OVRInput.Button.Down) ||
                            OVRInput.GetDown (OVRInput.Button.DpadDown) ||
                            OVRInput.GetDown (OVRInput.Button.PrimaryThumbstickDown) ||
                            OVRInput.GetDown (OVRInput.Button.SecondaryThumbstickDown)) {

                            command = Command.Down;
                        } 

                        #endif
                        break;
                    }

                case HmdType.Daydream:
                    {
                        #if GOOGLE_VR_INSTALLED

                        if (GvrControllerInput.IsTouching &&
                            GvrControllerInput.ClickButtonDown) {

                            command = GetCommandForDirection (GetDirection (GvrControllerInput.TouchPos + m_DaydreamTouchPadOffset, 0.25f));

                        } else if (GvrControllerInput.AppButtonDown) {

                            command = Command.Menu;
                        }

                        #endif
                        break;
                    }
                }
            }

            return command;
    	}

        public bool GetSelect (bool buttonDown) 
        {
            bool select = false;

            switch (m_HmdType) {

            case HmdType.HTCVive:
                {

                    select = (buttonDown ?
                        Input.GetButtonDown (ViveTriggerName) :
                        Input.GetButtonUp (ViveTriggerName));

                    break;
                }

            case HmdType.OculusRift:
                {
                    #if OCULUS_UTILITIES_INSTALLED

                    select = (buttonDown ?
                        OVRInput.GetDown (OVRInput.Button.One) :
                        OVRInput.GetUp (OVRInput.Button.One));

                    #endif
                    break;
                }

            case HmdType.Daydream:
                {
                    #if GOOGLE_VR_INSTALLED

                    select = (buttonDown ?
                        GvrControllerInput.ClickButtonDown :
                        GvrControllerInput.ClickButtonUp);

                    #endif
                    break;
                }

            case HmdType.OculusGearVR:
                {
                    Debug.LogWarning ("GetSelect() is not currently implemented for Gear VR");
                    break;
                }
            }

            return select;
        }

        // Converts a direction into a command
        private Command GetCommandForDirection (Direction direction)
        {
            Command command = Command.None;

            switch (direction) {

            case Direction.Center:
                {
                    command = Command.Select;
                    break; 
                }
            case Direction.Left:
                {
                    command = Command.Left;
                    break; 
                }
            case Direction.Right:
                {
                    command = Command.Right;
                    break; 
                }
            case Direction.Up:
                {
                    command = Command.Up;
                    break; 
                }
            case Direction.Down:
                {
                    command = Command.Down;
                    break; 
                }
            }

            return command;
        }

        private Direction GetDirection (Vector2 vector, float centerMagnitudeLimit)
        {
            const float halfPI = Mathf.PI / 2.0f;
            Direction direction = Direction.Unknown;

            // Direction?
            if (vector.magnitude >= centerMagnitudeLimit) {
                
                // Angle rotated 45 degrees for simplicity
                float angle = Mathf.Atan2 (vector.y, vector.x) + (Mathf.PI / 4.0f);

                // Determine direction
                if (angle > -halfPI && angle <= 0.0f)
                    direction = Direction.Up;
                else if (angle > 0.0f && angle <= halfPI)
                    direction = Direction.Right;
                else if (angle > halfPI && angle <= Mathf.PI)
                    direction = Direction.Down;
                else
                    direction = Direction.Left;
                
            } else {

                direction = Direction.Center;
            }

            return direction;
        }

        void Awake()
        {
            // Check if instance already exists
            if (instance == null) {

                //if not, set instance to this
                instance = this;

            // If instance already exists and it's not this:
            } else if (instance != this) {

                // Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a ControllerManager.
                Destroy (gameObject);    
            }

            // Sets this to not be destroyed when reloading scene
            // NOTE: Only works on root GameObjects, so be aware if crossing scenes
//            DontDestroyOnLoad (gameObject);
        }

        void Start()
        {
            if (m_HmdType == HmdType.Unknown) {
                
                // See if we can determine the HmdType (which can be overridden otherwise).
                Hmd hmd = FindObjectOfType<Hmd> ();
                if (hmd != null) {

                    this.hmdType = hmd.Type;
                }
            }
        }

        void Update()
        {
            #if OCULUS_UTILITIES_INSTALLED
            if (m_HmdType == HmdType.OculusRift || m_HmdType == HmdType.OculusGearVR || m_HmdType == HmdType.OculusGo) {
                
                // Be sure to call Update(), in case we're not using the Oculus CameraRig
                OVRInput.Update ();
            }
            #endif

            RaiseCommand (GetCommand ());
        }

        private void RaiseCommand(Command command)
        {
            // Make sure we have a command to raise
            if (command != Command.None && onCommand != null) {
                onCommand (this, new CommandEventArgs(command));
            }
        }

    }
}