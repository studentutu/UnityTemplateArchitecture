using System;
using System.Collections.Generic;
using Unity.RecordedPlayback;

namespace UnityEngine.EventSystems
{
    /**
     * This class is a drop in replacement for the Input class with support for recording and playback in the
     * Automated QA package. Currently supports touch and mouse events only with other input types coming soon.
     */
    public class RecordableInput
    {
        internal static bool playbackActive = false;

        // fake mouse data
        private static Dictionary<int, FakePress> fakeMousePresses = new Dictionary<int, FakePress>();
        private static HashSet<int> fakeMouseDownEvents = new HashSet<int>();
        private static HashSet<int> fakeMouseUpEvents = new HashSet<int>();
        private static Vector2 fakeMousePos;

        // fake touch data
        private static List<Touch> fakeTouches = new List<Touch>();
        private static Dictionary<int, FakePress> fakeTouchPresses = new Dictionary<int, FakePress>();

        // fake key/button data
        private static FakeKeyStates fakeKeyPresses = new FakeKeyStates();
        private static FakeKeyStates fakeKeyNamePresses = new FakeKeyStates();
        private static FakeKeyStates fakeButtonPresses = new FakeKeyStates();

        private struct FakePress
        {
            public float startTime;
            public float endTime;
            public Vector2 startPos;
            public Vector2 endPos;
        }

        private class FakeKeyStates
        {
            private Dictionary<string, FakePress> presses = new Dictionary<string, FakePress>();
            private HashSet<string> downEvents = new HashSet<string>();
            private HashSet<string> upEvents = new HashSet<string>();
            
            internal void Press(string key, FakePress fakeKey)
            {
                presses[key] = fakeKey;
                upEvents.Remove(key);
                downEvents.Add(key);
            }
            
            internal void UpdateStates()
            {
                downEvents.Clear();
                upEvents.Clear();

                foreach (var key in presses.Keys)
                {
                    var press = presses[key];
                    if (Time.time >= press.endTime)
                    {
                        upEvents.Add(key);
                    }
                }
                
                foreach (var key in upEvents)
                {
                    presses.Remove(key);
                }
                
            }

            internal bool GetPressed(string key)
            {
                return IsPlaybackActive() && presses.ContainsKey(key);
            }

            internal bool GetDown(string key)
            {
                if (IsPlaybackActive() && presses.ContainsKey(key))
                {
                    return downEvents.Contains(key);
                }

                return false;
            }

            internal bool GetUp(string key)
            {
                return IsPlaybackActive() && upEvents.Contains(key);
            }
            
            internal void Clear()
            {
                presses.Clear();
                upEvents.Clear();
                downEvents.Clear();
            }

        }

        /// <summary>
        ///   <para>Number of touches. Guaranteed not to change throughout the frame. (Read Only)</para>
        /// </summary>
        public static int touchCount
        {
            get
            {
                if (IsPlaybackActive())
                {
                    return fakeTouches.Count;
                }

                return Input.touchCount;
            }
        }

        /// <summary>
        ///   <para>Returns list of objects representing status of all touches during last frame. (Read Only) (Allocates temporary variables).</para>
        /// </summary>
        public static Touch[] touches
        {
            get
            {
                int numTouches = touchCount;
                Touch[] touchArray = new Touch[numTouches];
                for (int index = 0; index < numTouches; ++index)
                    touchArray[index] = GetTouch(index);
                return touchArray;
            }
        }

        /// <summary>
        ///   <para>Call Input.GetTouch to obtain a Touch struct.</para>
        /// </summary>
        /// <param name="index">The touch input on the device screen.</param>
        /// <returns>
        ///   <para>Touch details in the struct.</para>
        /// </returns>
        public static Touch GetTouch(int index)
        {
            if (IsPlaybackActive())
            {
                if (index < fakeTouches.Count)
                {
                    var touch = fakeTouches[index];
                    if (fakeTouchPresses.ContainsKey(touch.fingerId))
                    {
                        touch.position = GetInterpolatedPosition(fakeTouchPresses[touch.fingerId]);
                    }
                    return touch;
                }
            }

            return Input.GetTouch(index);
        }

        /// <summary>
        ///   <para>The current mouse position in pixel coordinates. (Read Only)</para>
        /// </summary>
        public static Vector3 mousePosition
        {
            get
            {
                if (IsPlaybackActive())
                {
                    return fakeMousePresses.ContainsKey(0) ? GetInterpolatedPosition(fakeMousePresses[0]) : fakeMousePos;
                }

                return Input.mousePosition;
            }
        }

        /// <summary>
        ///   <para>Returns whether the given mouse button is held down.</para>
        /// </summary>
        /// <param name="button"></param>
        public static bool GetMouseButton(int button)
        {
            bool fake = (IsPlaybackActive() && fakeMousePresses.ContainsKey(button));
            return fake || Input.GetMouseButton(button);
        }

        /// <summary>
        ///   <para>Returns true during the frame the user pressed the given mouse button.</para>
        /// </summary>
        /// <param name="button"></param>
        public static bool GetMouseButtonDown(int button)
        {
            var fakeDown = false;
            if (IsPlaybackActive() && fakeMousePresses.ContainsKey(button))
            {
                fakeDown = fakeMouseDownEvents.Contains(button);
            }

            return fakeDown || Input.GetMouseButtonDown(button);
        }

        /// <summary>
        ///   <para>Returns true during the frame the user releases the given mouse button.</para>
        /// </summary>
        /// <param name="button"></param>
        public static bool GetMouseButtonUp(int button)
        {
            var fakeUp = false;
            if (IsPlaybackActive() && fakeMousePresses.ContainsKey(button))
            {
                fakeUp = fakeMouseUpEvents.Contains(button);
            }

            return fakeUp || Input.GetMouseButtonUp(button);
        }
        
        /// <summary>
        ///   <para>Last measured linear acceleration of a device in three-dimensional space. (Read Only)
        ///   Not supported with Recorded Playback.</para>
        /// </summary>
        public static Vector3 acceleration => Input.acceleration;
        
        /// <summary>
        ///   <para>Number of acceleration measurements which occurred during last frame.
        ///   Not supported with Recorded Playback.</para>
        /// </summary>
        public static int accelerationEventCount => Input.accelerationEventCount;
        
        
        /// <summary>
        ///   <para>Returns list of acceleration measurements which occurred during the last frame. (Read Only) (Allocates temporary variables).
        ///   Not supported with Recorded Playback.</para>
        /// </summary>
        public static AccelerationEvent[] accelerationEvents => Input.accelerationEvents;
        
        /// <summary>
        ///   <para>Is any key or mouse button currently held down? (Read Only)
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        public static bool anyKey => Input.anyKey;
        
        /// <summary>
        ///   <para>Returns true the first frame the user hits any key or mouse button. (Read Only)
        /// 
        ///   Not supported with Recorded Playback.</para>
        /// </summary>
        public static bool anyKeyDown => Input.anyKeyDown;
        
        /// <summary>
        ///         <para>Should  Back button quit the application?
        /// 
        /// Only usable on Android, Windows Phone or Windows Tablets.
        ///
        /// Not supported with Recorded Playback.</para>
        ///       </summary>
        public static bool backButtonLeavesApp => Input.backButtonLeavesApp;
        
        /// <summary>
        ///   <para>Property for accessing compass (handheld devices only). (Read Only)
        ///
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        public static Compass compass => Input.compass;
        
        /// <summary>
        ///   <para>This property controls if input sensors should be compensated for screen orientation.
        ///
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        public static bool compensateSensors => Input.compensateSensors;
        
        /// <summary>
        ///   <para>The current text input position used by IMEs to open windows.
        ///
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        public static Vector2 compositionCursorPos => Input.compositionCursorPos;
        
        /// <summary>
        ///   <para>The current IME composition string being typed by the user.
        ///
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        public static string compositionString => Input.compositionString;
        
        /// <summary>
        ///   <para>Device physical orientation as reported by OS. (Read Only)
        ///
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        public static DeviceOrientation deviceOrientation => Input.deviceOrientation;
        
        /// <summary>
        ///   <para>Returns default gyroscope.
        ///
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        public static Gyroscope gyro => Input.gyro;
        
        /// <summary>
        ///   <para>Controls enabling and disabling of IME input composition.
        ///
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        public static IMECompositionMode imeCompositionMode => Input.imeCompositionMode;
        
        /// <summary>
        ///   <para>Does the user have an IME keyboard input source selected?
        ///
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        public static bool imeIsSelected => Input.imeIsSelected;
        
        /// <summary>
        ///   <para>Returns the keyboard input entered this frame. (Read Only)
        /// 
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        public static string inputString => Input.inputString;
        
        /// <summary>
        ///   <para>Indicates if a mouse device is detected.
        /// 
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        public static bool mousePresent => Input.mousePresent;
        
        /// <summary>
        ///   <para>The current mouse scroll delta. (Read Only)
        ///
        ///  Not supported with Recorded Playback.</para>
        /// </summary>
        public static Vector2 mouseScrollDelta => Input.mouseScrollDelta;
        
        /// <summary>
        ///   <para>Property indicating whether the system handles multiple touches.
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        public static bool multiTouchEnabled => Input.multiTouchEnabled;
        
        /// <summary>
        ///   <para>Enables/Disables mouse simulation with touches. By default this option is enabled.
        ///
        ///  Not supported with Recorded Playback.</para>
        /// </summary>
        public static bool simulateMouseWithTouches => Input.simulateMouseWithTouches;
        
        
        /// <summary>
        ///   <para>Returns true when Stylus Touch is supported by a device or platform.
        /// 
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        public static bool stylusTouchSupported => Input.stylusTouchSupported;
        
        /// <summary>
        ///   <para>Bool value which let's users check if touch pressure is supported.
        ///
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        public static bool touchPressureSupported => Input.touchPressureSupported;

        /// <summary>
        ///   <para>Returns specific acceleration measurement which occurred during last frame. (Does not allocate temporary variables).
        /// 
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        /// <param name="index"></param>
        public static AccelerationEvent GetAccelerationEvent(int index)
        {
            return Input.GetAccelerationEvent(index);
        }

        /// <summary>
        ///   <para>Returns the value of the virtual axis identified by axisName.
        ///
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        /// <param name="axisName"></param>
        public static float GetAxis(string axisName)
        {
            return Input.GetAxis(axisName);
        }

        /// <summary>
        ///   <para>Returns the value of the virtual axis identified by axisName with no smoothing filtering applied.
        ///
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        /// <param name="axisName"></param>
        public static float GetAxisRaw(string axisName)
        {
            return Input.GetAxisRaw(axisName);
        }

        
        /// <summary>
        ///   <para>Returns true while the virtual button identified by buttonName is held down.</para>
        /// </summary>
        /// <param name="buttonName">The name of the button such as Jump.</param>
        /// <returns>
        ///   <para>True when an axis has been pressed and not released.</para>
        /// </returns>
        public static bool GetButton(string buttonName)
        {
            return fakeButtonPresses.GetPressed(buttonName) || Input.GetButton(buttonName);
        }

        /// <summary>
        ///   <para>Returns true during the frame the user pressed down the virtual button identified by buttonName.</para>
        /// </summary>
        /// <param name="buttonName"></param>
        public static bool GetButtonDown(string buttonName)
        {
            return fakeButtonPresses.GetDown(buttonName) || Input.GetButtonDown(buttonName);
        }

        /// <summary>
        ///   <para>Returns true the first frame the user releases the virtual button identified by buttonName.</para>
        /// </summary>
        /// <param name="buttonName"></param>
        public static bool GetButtonUp(string buttonName)
        {
            return fakeButtonPresses.GetUp(buttonName) || Input.GetButtonUp(buttonName);
        }

        /// <summary>
        ///   <para>Returns an array of strings describing the connected joysticks.
        ///
        /// Not supported with Recorded Playback.</para>
        /// </summary>
        public static string[] GetJoystickNames()
        {
            return Input.GetJoystickNames();
        }

        /// <summary>
        ///   <para>Returns true while the user holds down the key identified by the key KeyCode enum parameter.</para>
        /// </summary>
        /// <param name="key"></param>
        public static bool GetKey(KeyCode keyCode)
        {
            return fakeKeyPresses.GetPressed(keyCode.ToString()) || Input.GetKey(keyCode);
        }

        /// <summary>
        ///   <para>Returns true while the user holds down the key identified by name.</para>
        /// </summary>
        /// <param name="name"></param>
        public static bool GetKey(string name)
        {
            return fakeKeyNamePresses.GetPressed(name) || Input.GetKey(name);
        }

        /// <summary>
        ///   <para>Returns true during the frame the user starts pressing down the key identified by the key KeyCode enum parameter.</para>
        /// </summary>
        /// <param name="key"></param>
        public static bool GetKeyDown(KeyCode keyCode)
        {
            return fakeKeyPresses.GetDown(keyCode.ToString()) || Input.GetKeyDown(keyCode);
        }

        /// <summary>
        ///   <para>Returns true during the frame the user starts pressing down the key identified by name.</para>
        /// </summary>
        /// <param name="name"></param>
        public static bool GetKeyDown(string name)
        {
            return fakeKeyNamePresses.GetDown(name) || Input.GetKeyDown(name);
        }

        
        /// <summary>
        ///   <para>Returns true during the frame the user releases the key identified by the key KeyCode enum parameter.</para>
        /// </summary>
        /// <param name="key"></param>
        public static bool GetKeyUp(KeyCode keyCode)
        {
            return fakeKeyPresses.GetUp(keyCode.ToString()) || Input.GetKeyUp(keyCode);
        }

        /// <summary>
        ///   <para>Returns true during the frame the user releases the key identified by name.</para>
        /// </summary>
        /// <param name="name"></param>
        public static bool GetKeyUp(string name)
        {
            return fakeKeyNamePresses.GetUp(name) || Input.GetKeyUp(name);
        }

        /// <summary>
        ///   <para>Resets all input. After ResetInputAxes all axes return to 0 and all buttons return to 0 for one frame.
        ///
        /// Not supported by Recorded Playback.</para>
        /// </summary>
        public static void ResetInputAxes()
        {
            Input.ResetInputAxes();
        }

        // Internal methods used for playback of recorded data
        internal static void FakeMouseDown(int button, Vector2 downPos, Vector2 upPos, float duration = 1f)
        {
            var fakeMousePress = new FakePress
            {
                startTime = Time.time,
                endTime = Time.time + duration,
                startPos = downPos,
                endPos = upPos
            };
            fakeMousePresses[button] = fakeMousePress;
            fakeMouseUpEvents.Remove(button);
            fakeMouseDownEvents.Add(button);
        }
        
        internal static void FakeKeyDown(KeyCode keycode, float duration = 1f)
        {
            var fakeKey = new FakePress
            {
                startTime = Time.time,
                endTime = Time.time + duration,
            };
            fakeKeyPresses.Press(keycode.ToString(), fakeKey);
        }
        
        internal static void FakeKeyDown(string keycode, float duration = 1f)
        {
            var fakeKey = new FakePress
            {
                startTime = Time.time,
                endTime = Time.time + duration,
            };
            fakeKeyNamePresses.Press(keycode, fakeKey);
        }


        internal static void FakeButtonDown(string axisName, float duration = 1f)
        {
            var fakeKey = new FakePress
            {
                startTime = Time.time,
                endTime = Time.time + duration,
            };
            fakeButtonPresses.Press(axisName, fakeKey);
        }

        internal static void FakeTouch(Touch touch, Vector2 releasePos, float duration)
        {
            var press = new FakePress
            {
                startTime = Time.time,
                endTime = Time.time + duration,
                startPos = touch.position,
                endPos = releasePos
            };
            fakeTouchPresses[touch.fingerId] = press;

            for (int i = 0; i < fakeTouches.Count; i++)
            {
                if (fakeTouches[i].fingerId == touch.fingerId)
                {
                    fakeTouches[i] = touch;
                    return;
                }
            }
            fakeTouches.Add(touch);
        }

        internal static void Update()
        {
            UpdateTouch();
            UpdateMouse();
            fakeKeyPresses.UpdateStates();
            fakeKeyNamePresses.UpdateStates();
            fakeButtonPresses.UpdateStates();
        }

        private static void UpdateTouch()
        {
            for (int i = fakeTouches.Count - 1; i >= 0; i--)
            {
                switch (fakeTouches[i].phase)
                {
                    case TouchPhase.Began:
                        var newTouch = fakeTouches[i];
                        newTouch.phase = TouchPhase.Moved;
                        fakeTouches[i] = newTouch;
                        break;
                    case TouchPhase.Ended:
                        fakeTouches.RemoveAt(i);
                        break;
                    case TouchPhase.Moved:
                        var endingTouch = fakeTouches[i];
                        if (fakeTouchPresses.ContainsKey(endingTouch.fingerId) && Time.time >= fakeTouchPresses[endingTouch.fingerId].endTime)
                        {
                            endingTouch.phase = TouchPhase.Ended;
                            fakeTouches[i] = endingTouch;
                        }
                        break;
                }
            }
        }

        private static void UpdateMouse()
        {
            fakeMouseDownEvents.Clear();
            foreach (var button in fakeMouseUpEvents)
            {
                if (button == 0 && fakeMousePresses.ContainsKey(button))
                {
                    fakeMousePos = fakeMousePresses[button].endPos;
                }
                fakeMousePresses.Remove(button);
            }
            fakeMouseUpEvents.Clear();

            foreach (var button in fakeMousePresses.Keys)
            {
                var press = fakeMousePresses[button];
                if (Time.time >= press.endTime)
                {
                    fakeMouseUpEvents.Add(button);
                }
            }
        }
        
        
        internal static void Reset()
        {
            fakeMousePos = new Vector2();
            
            fakeMousePresses.Clear();
            fakeMouseDownEvents.Clear();
            fakeMouseUpEvents.Clear();
            
            fakeKeyPresses.Clear();
            fakeKeyNamePresses.Clear();
            fakeButtonPresses.Clear();
            
            fakeTouches.Clear();
            fakeTouchPresses.Clear();
            
        }

        private static Vector2 GetInterpolatedPosition(FakePress fakePress)
        {
            var now = Time.time;
            var deltaTime = Math.Min(1, (now - fakePress.startTime) / (fakePress.endTime - fakePress.startTime));
            return fakePress.startPos + deltaTime * (fakePress.endPos - fakePress.startPos);
        }

        private static bool IsPlaybackActive()
        {
            return playbackActive || RecordedPlaybackController.IsPlaybackActive();
        }
    }
}
