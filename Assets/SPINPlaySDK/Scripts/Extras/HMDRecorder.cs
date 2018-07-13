using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace Pixvana.Extras
{

    public class HMDRecorder : MonoBehaviour
    {

        private const string TimeKey = "time";
        private const string HeadingKey = "heading";

        public delegate void PlayDelegate (Vector3 heading);

        public enum RecordingState
        {
            Stopped = 0,
            Recording = 1,
            Playing = 2
        }

        [Tooltip("The sample duration in seconds")]
        [SerializeField] private float m_SampleDuration = 0.1f;
        public float SampleDuration { get { return m_SampleDuration; } set { m_SampleDuration = value; } }

        [SerializeField] private GameObject m_HeadModel = null;
        public GameObject HeadModel { get { return m_HeadModel; } set { m_HeadModel = value; } }

        [SerializeField] private TextAsset m_Recording = null;
        public TextAsset Recording { get { return m_Recording; } set { m_Recording = value; } }

        private RecordingState m_State = RecordingState.Stopped;

        public RecordingState State { get { return m_State; } }

        private List<float> m_Timings = new List<float> ();
        private List<Vector3> m_Headings = new List<Vector3> ();
        private float m_LastSampleTime = 0.0f;
        private Quaternion m_HeadModelBaseRotation = Quaternion.identity;

        private Func<Vector3> m_GetHeading = null;
        private PlayDelegate m_PlayHeading = null;

        void Awake ()
        {
            // Hide the head model
            ShowHeadModel (false);

            // Grab the head model's transform
            if (m_HeadModel != null) {
                
                m_HeadModelBaseRotation = m_HeadModel.transform.rotation;
            }

            // If we have a text asset, load it
            if (m_Recording != null) {

                LoadFromString (m_Recording.text);
            }
        }

        public void Record (Func<Vector3> getHeading)
        {

            // NOTE: Always replaces any current recording

            Debug.Assert (m_State == RecordingState.Stopped, "Must stop before recording");
            Debug.Assert (getHeading != null, "getHeading is required");

            m_GetHeading = getHeading;
            m_State = RecordingState.Recording;

            // Set recording time reference as now
            m_LastSampleTime = Time.time;

            // Clear last recording
            m_Timings.Clear ();
            m_Headings.Clear ();

            StartCoroutine (SampleHeading ());
        }

        public void Play (PlayDelegate playHeading)
        {

            // NOTE: Always starts at the beginning

            Debug.Assert (m_State == RecordingState.Stopped, "Must stop before playing");

            m_PlayHeading = playHeading;
            m_State = RecordingState.Playing;

            // Show the head model
            ShowHeadModel (true);

            StartCoroutine (PlayHeadings ());
        }

        public void Stop ()
        {

            StopCoroutine ("SampleHeading");
            m_State = RecordingState.Stopped;

            // Hide the head model
            ShowHeadModel (false);
        }

        #region Head Model

        private void ShowHeadModel (bool show) {

            if (m_HeadModel != null) {

                m_HeadModel.SetActive (show);
            }
        }

        private void SetHeadModelHeading (Vector3 heading, float time) {

            if (m_HeadModel != null) {

                m_HeadModel.transform.localRotation = Quaternion.Euler (heading) * m_HeadModelBaseRotation;

                Vector3 forward = m_HeadModel.transform.TransformDirection(Vector3.up) * 100;
                Debug.DrawRay(m_HeadModel.transform.position, forward, Color.red, time);
            }
        }

        #endregion

        IEnumerator SampleHeading ()
        {

            while (m_State == RecordingState.Recording) {

                float currentTime = Time.time;

                m_Timings.Add (currentTime - m_LastSampleTime);
                m_Headings.Add (m_GetHeading ());

                m_LastSampleTime = currentTime;

                yield return new WaitForSeconds (m_SampleDuration);
            }
        }

        IEnumerator PlayHeadings ()
        {

            Debug.Assert (m_Timings.Count == m_Headings.Count, "Data consistency error: should have an equal number of timings and headings.");

            for (int playIndex = 0; playIndex < m_Timings.Count; playIndex++) {

                if (m_State == RecordingState.Playing) {
                
                    yield return new WaitForSeconds (m_Timings [playIndex]);

                    Vector3 heading = m_Headings [playIndex];
                    m_PlayHeading.Invoke (heading);
                    SetHeadModelHeading (heading, m_Timings [playIndex]);
                }
            }
        }

        public void SaveToFile (string path)
        {

            Debug.Assert (m_State == RecordingState.Stopped, "Must stop before saving");
            Debug.Assert ((path != null) && (path.Length > 0), "path is required");
            Debug.Assert (m_Timings.Count == m_Headings.Count, "Data consistency error: should have an equal number of timings and headings.");

            JSONNode headingsNode = new JSONArray ();

            for (int playIndex = 0; playIndex < m_Timings.Count; playIndex++) {

                JSONNode headingNode = new JSONObject ();
                headingNode [TimeKey].AsFloat = m_Timings [playIndex];
                headingNode [HeadingKey] = m_Headings [playIndex].ToString ();

                headingsNode.Add (headingNode);
            }

            // Write JSON to file
            System.IO.File.WriteAllText (path, headingsNode.ToString ());

            Debug.Log ("Saved " + m_Timings.Count + " HMD samples");
        }

        public void LoadFromFile (string path)
        {

            Debug.Assert (m_State == RecordingState.Stopped, "Must stop before loading");
            Debug.Assert ((path != null) && (path.Length > 0), "path is required");

            string jsonString = System.IO.File.ReadAllText (path);

            LoadFromString (jsonString);
        }

        private void LoadFromString (string jsonString) {

            Debug.Assert ((jsonString != null) && (jsonString.Length > 0), "jsonString is required");

            // Clear last recording
            m_Timings.Clear ();
            m_Headings.Clear ();

            JSONArray headingsNode = JSON.Parse (jsonString) as JSONArray;

            foreach (JSONObject headingNode in headingsNode) {

                m_Timings.Add (headingNode [TimeKey].AsFloat);
                m_Headings.Add (Vector3FromString (headingNode [HeadingKey]));
            }

            Debug.Assert (m_Timings.Count == m_Headings.Count, "Data consistency error: should have an equal number of timings and headings.");

//            Debug.Log ("Loaded " + m_Timings.Count + " HMD samples");
        }

        private Vector3 Vector3FromString (string vector3String)
        {

            Debug.Assert ((vector3String != null) && (vector3String.Length > 2), "A valid vector3String is required");

            string[] components = vector3String.Substring (1, vector3String.Length - 2).Split (',');
            float x = float.Parse (components [0]);
            float y = float.Parse (components [1]);
            float z = float.Parse (components [2]);
            return new Vector3 (x, y, z);
        }
    }
}
