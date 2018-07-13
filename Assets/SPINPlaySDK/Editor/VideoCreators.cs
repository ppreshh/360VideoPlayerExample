using Pixvana.VR;
using UnityEngine;
using UnityEditor;

namespace Pixvana.Video
{
    // NOTE: Separate priority by more then 10 to introduce a separator

    public static class ProjectorCreator
    {
        [MenuItem("GameObject/SPIN Play SDK/Projector", false, 0)]
        private static void CreateProjector() {

            GameObject gameObject = new GameObject ("Projector");

            Projector projector = gameObject.AddComponent<Projector> ();
            CameraRig cameraRig = gameObject.AddComponent<CameraRig> ();
            Hmd hmd = gameObject.AddComponent<Hmd> ();
            PlayerBase player = gameObject.AddComponent<Player> ();

            // Configure the projector
            projector.cameraRig = cameraRig;
            projector.hmd = hmd;
            projector.player = player;

            // Configure the camera rig
            cameraRig.hmd = hmd;
        }

        [MenuItem("GameObject/SPIN Play SDK/Player", false, 1)]
        private static void CreatePlayer() {

            GameObject gameObject = new GameObject ("Player");

            gameObject.AddComponent<Player> ();
        }
    }
}