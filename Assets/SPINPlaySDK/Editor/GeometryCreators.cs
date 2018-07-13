using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Pixvana.Geometry
{
    // NOTE: Separate priority by more then 10 to introduce a separator

    public static class SphereCreator
    {

        [MenuItem ("GameObject/SPIN Play SDK/Sphere", false, 51)]
        private static void CreateSphere ()
        {

            Sphere.Create ();
        }
    }

    public static class FrustumCreator
    {

        [MenuItem ("GameObject/SPIN Play SDK/Frustum", false, 52)]
        private static void CreateFrustum ()
        {

            Frustum.Create ();
        }
    }
}