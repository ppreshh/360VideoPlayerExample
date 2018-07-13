using Pixvana.Video;
using Pixvana.VR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pixvana.Examples
{
    public class Example2 : MonoBehaviour
    {

        private Pixvana.Video.Projector m_Projector = null;
        private CameraRig m_CameraRig = null;
        private Hmd m_Hmd = null;
        private PlayerBase m_Player = null;

        void Awake ()
        {

            GameObject gameObject = new GameObject("Projector");

            m_Projector = gameObject.AddComponent<Pixvana.Video.Projector>();
            m_CameraRig = gameObject.AddComponent<CameraRig>();
            m_Hmd = gameObject.AddComponent<Hmd>();
            m_Player = gameObject.AddComponent<Player>();

            // Configure the projector
            m_Projector.cameraRig = m_CameraRig;
            m_Projector.hmd = m_Hmd;
            m_Projector.player = m_Player;
            m_Projector.sourceUrl = "http://media.pixvana.com/media/twM2FJs0tM9R48nj7eXJbYwcpX-CVrD6uBznicrsi00~/index.opf";

            // Configure the camera rig
            m_CameraRig.hmd = m_Hmd;
        }

        void Start ()
        {
            
            m_Projector.Prepare ();
        }
    }
}