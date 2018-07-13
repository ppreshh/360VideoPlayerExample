using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pixvana.Geometry
{

    /// <summary>
    /// Contains useful geometry-related tools.
    /// </summary>
    public static class Tools
    {
        /// <summary>
        /// A horizontal or vertical direction.
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// A horizontal direction.
            /// </summary>
            Horizontal,
            /// <summary>
            /// A vertical direction.
            /// </summary>
            Vertical
        }

        /// <summary>
        /// Reverses the normals for a given mesh.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        static public void ReverseNormals(Mesh mesh) {

            Vector3[] normals = mesh.normals;
            for (int i = 0; i < normals.Length; i++)
                normals[i] = -normals[i];
            mesh.normals = normals;

            for (int m = 0; m < mesh.subMeshCount; m++)
            {
                int[] triangles = mesh.GetTriangles(m);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int temp = triangles[i + 0];
                    triangles[i + 0] = triangles[i + 1];
                    triangles[i + 1] = temp;
                }
                mesh.SetTriangles(triangles, m);
            }
        }

        /// <summary>
        /// Flips a UV map in a horizontal or vertical direction.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <param name="direction">The direction.</param>
        static public void FlipUVs(Mesh mesh, Direction direction) {

            for (int m = 0; m < mesh.subMeshCount; m++)
            {
                List<Vector2> uvs = new List<Vector2> ();
                mesh.GetUVs (m, uvs);
                for (int i = 0; i < uvs.Count; i++) {

                    switch (direction) {

                    case Direction.Horizontal:
                        {
                            uvs [i] = new Vector2(1.0f - uvs [i].x, uvs[i].y);
                            break;
                        }
                    case Direction.Vertical:
                        {
                            uvs [i] = new Vector2(uvs [i].x, 1.0f - uvs[i].y);
                            break;
                        }
                    }
                }
                mesh.SetUVs (m, uvs);
            }
        }

        static private float dot(Vector2 v1, Vector2 v2)
        {
            return v1.x * v2.x + v1.y * v2.y;
        }

        static private bool PointInTriangle(Vector2 pt, Vector2[] pts, Vector2[] n)
        {
            for (var i = 0; i < 3; i++) {
                if (dot(pt - pts[i], n[i]) > 0)
                    return false;
            }
            return true;
        }

        static private bool IntersectTriangles(Vector2[] t1, Vector2[] t2)
        {
            var d0 = t1[1] - t1[0];
            var n0 = new Vector2(-d0.y, d0.x);
            var d1 = t1[2] - t1[1];
            var n1 = new Vector2(-d1.y, d1.x);
            var d2 = t1[0] - t1[2];
            var n2 = new Vector2(-d2.y, d2.x);
            var n = new Vector2[] { n0, n1, n2 };

            for (var i = 0; i < 3; i++)
            {
                if (PointInTriangle(t2[i], t1, n))
                {
                    //Debug.Log("found intersection {" + t1[0].ToString("F4") + ", " + t1[1].ToString("F4") + ", " + t1[2].ToString("F4") + "}, {" + t2[0].ToString("F4") + ", " + t2[1].ToString("F4") + ", " + t2[2].ToString("F4") + "}");
                    return true;
                }
            }
            return false;
        }

        static private bool IntersectTriangles(Vector2[] t1, Vector4 t1b, Vector2[] t2, Vector4 t2b)
        {
            if (t1b.z > t2b.x && t1b.x < t2b.z && t1b.y < t2b.w && t1b.w > t2b.y)
                return IntersectTriangles(t1, t2) || IntersectTriangles(t2, t1);
            return false;
        }

        static private void IncreaseBounds(ref Vector4 bounds, Vector2 pt)
        {
            if (pt.x < bounds.x)
                bounds.x = pt.x;
            else if (pt.x > bounds.z)
                bounds.z = pt.x;
            if (pt.y < bounds.y)
                bounds.y = pt.y;
            else if (pt.y > bounds.w)
                bounds.w = pt.y;
        }

        static private Vector4 ComputeBounds(Vector2[] t)
        {
            var bounds = new Vector4(t[0].x, t[0].y, t[0].x, t[0].y);
            IncreaseBounds(ref bounds, t[1]);
            IncreaseBounds(ref bounds, t[2]);
            return bounds;
        }

        static public void AddIntersectedUVs(Mesh mesh, int subMesh, Vector2[] new_uvs)
        {
            var t = mesh.GetTriangles(subMesh);
            var numMeshTriangles = t.Length / 3;
            var uvs = mesh.uv;

            var num_newUvTriangles = new_uvs.Length / 3;
            var added_triangles = new List<int>();
            for (var mesh_triangle = 0; mesh_triangle < numMeshTriangles; mesh_triangle++)
            {
                var mti = mesh_triangle * 3;
                var mt = new Vector2[] { uvs[t[mti]], uvs[t[mti+1]], uvs[t[mti+2]] };
                var mtbounds = ComputeBounds(mt);
                //Debug.Log("indices <" + t[mti] + ", " + t[mti+1] + ", " + t[mti+2] + ">, triangle {" + mt[0].ToString("F4") + ", " + mt[1].ToString("F4") + ", " + mt[2].ToString("F4") + "}, bounds " + mtbounds.ToString("F4"));
                for (var new_triangle = 0; new_triangle < num_newUvTriangles; new_triangle++)
                {
                    var nti = new_triangle * 3;
                    var nt = new Vector2[] { new_uvs[nti], new_uvs[nti + 1], new_uvs[nti + 2] };
                    if (IntersectTriangles(mt, mtbounds, nt, ComputeBounds(nt)))
                    {
                        //Debug.Log("adding triangle {" + t[mti] + ", " + t[mti + 1] + ", " + t[mti + 2] + "}");
                        added_triangles.Add(t[mti]);
                        added_triangles.Add(t[mti+1]);
                        added_triangles.Add(t[mti+2]);
                        break;
                    }
                }
            }
            mesh.subMeshCount = 2;
            mesh.SetTriangles(added_triangles.ToArray(), 1, true);
        }

        #region Debug help

        /// <summary>
        /// Debugs an array of vertices.
        /// </summary>
        /// <param name="inVertices">The vertices.</param>
        static public void DebugVertices (Vector3[] inVertices)
        {

            for (int i = 0; i < inVertices.Length; i++) {

                Vector3 vertex = inVertices [i];
                Debug.Log ("Vertex: index = " + i.ToString () + ", x = " + vertex.x.ToString () + ", y = " + vertex.y.ToString () + ", z = " + vertex.z.ToString ());
            }
        }

        /// <summary>
        /// Debugs an array of UVs.
        /// </summary>
        /// <param name="inUVs">The UVs.</param>
        static public void DebugUVs (Vector2[] inUVs)
        {

            for (int i = 0; i < inUVs.Length; i++) {

                Vector2 uv = inUVs [i];
                Debug.Log ("UV: index = " + i.ToString () + ", x = " + uv.x.ToString () + ", y = " + uv.y.ToString ());
            }
        }

        /// <summary>
        /// Debugs an array of triangles.
        /// </summary>
        /// <param name="inTriangles">The triangles.</param>
        static public void DebugTriangles (int[] inTriangles)
        {

            for (int i = 0; i < inTriangles.Length; i++) {

                int triangle = inTriangles [i];
                Debug.Log ("Triangle: index = " + i.ToString () + ", value = " + triangle.ToString ());
            }
        }

#endregion
    }
}
