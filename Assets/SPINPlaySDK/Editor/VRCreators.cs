using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Pixvana.VR
{
    // NOTE: Separate priority by more then 10 to introduce a separator

    public static class HmdCreator
    {

        [MenuItem ("GameObject/SPIN Play SDK/Hmd", false, 10)]
        private static void CreateHmd ()
        {

            GameObject gameObject = new GameObject ("Hmd");

            gameObject.AddComponent<Hmd> ();
        }
    }
}