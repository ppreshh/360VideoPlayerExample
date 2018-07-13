using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Pixvana.Opf
{

    /// <summary>
    /// Represents a uvMap transform.
    /// </summary>
    public class UvMapTransform : VideoTransform
    {
        private const string MapsKey = "uvMaps";
        private const string IdKey = "id";
        private GameObject m_Downloader;

        private class UvMapNode
        {
            private const string ToformatUrlKey = "toFormatUrl";
            private const string FromFormatUrlKey = "fromFormatUrl";
            private const string DiscontinuityAreaKey = "discontinuityArea";

            private string m_Id;
            private string m_ToFormatUrl;
            private string m_FromFormatUrl;
            private UInt16[] m_DiscontinuityArea;

            public UvMapNode(JSONObject jsonObject)
            {
                m_Id = jsonObject[IdKey];
                m_ToFormatUrl = jsonObject[ToformatUrlKey];
                m_FromFormatUrl = jsonObject[FromFormatUrlKey];
                JSONNode discontNode = jsonObject[DiscontinuityAreaKey];
                if (discontNode != null)
                {
                    JSONArray distArr = discontNode.AsArray;
                    m_DiscontinuityArea = new UInt16[distArr.Count];
                    for (int i = 0; i < distArr.Count; i++)
                        m_DiscontinuityArea[i] = (UInt16)distArr[i].AsInt;
                    Assert.IsTrue(distArr.Count % 6 == 0, "discontinuity area array length must be a multiple of 6, the length was: " + distArr.Count);
                }
            }

            public string toFormatUrl { get { return m_ToFormatUrl; } }
            public string fromFormatUrl { get { return m_FromFormatUrl; } }
            public string id { get { return m_Id; } }
        }

        private List<UvMapNode> m_MapNodes;

        private class TxtRec : MonoBehaviour
        {
            private Texture2D       m_Txt;
            bool                    m_Complete;
            List<Action<Texture2D>> m_PendingNotifications;

            void Awake()
            {
                m_Complete = false;
                m_PendingNotifications = new List<Action<Texture2D>>();
            }

            public void start_load(string url)
            {
                StartCoroutine(load(url));
            }

            public void withTexture(Action<Texture2D> completion)
            {
                if (m_Complete)
                    completion(m_Txt);
                else
                    m_PendingNotifications.Add(completion);
            }

            private IEnumerator load(string url)
            {
                DateTime start_time = DateTime.Now;
                WWW www = new WWW(url);
                yield return www;

                Texture2D wwwtex = www.texture;
                Color32[] src_colors = wwwtex.GetPixels32();
                Color[] dst_colors = new Color[src_colors.Length];
                int height = wwwtex.height;
                int width = wwwtex.width;
                www.Dispose();
                for (int y = 0; y < height; y++)
                {
                    int last_u = 0;
                    int last_v = 0;
                    for (int x = 0; x < width; x++)
                    {
                        Color32 src = src_colors[y * width + x];
                        int _u = ((int)src.g * 256) +src.b;
                        int _v = ((int)src.a * 256) +src.r;
                        int u = (last_u + _u) & 0x0000ffff;
                        int v = (last_v + _v) & 0x0000ffff;
                        Color dc;
                        dc.r = u / 65535.0f;
                        dc.g = v / 65535.0f;
                        dc.b = dc.a = 0;
                        dst_colors[y * width + x] = dc;
                        last_u = u;
                        last_v = v;
                    }
                }

                m_Txt = new Texture2D(width, height, TextureFormat.RGFloat, false/*mipmap*/);
                m_Txt.SetPixels(dst_colors);
                m_Txt.Apply();
                m_Txt.filterMode = FilterMode.Bilinear;
                m_Txt.wrapMode = TextureWrapMode.Clamp;
                m_Txt.anisoLevel = 1;

                m_Complete = true;
                foreach (Action<Texture2D> c in m_PendingNotifications)
                {
                    c(m_Txt);
                }
            }
        };

        private Dictionary<string, TxtRec> m_Textures;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pixvana.Opf.UvMapTransform"/> class.
        /// </summary>
        /// <param name="jsonObject">A Json object.</param>
        /// <param name="version">The OPF schema version.</param>
        public UvMapTransform(JSONObject jsonObject, int version)
        {
            Assert.IsNotNull(jsonObject, "jsonObject cannot be null");

            m_MapNodes = new List<UvMapNode>();
            m_Textures = new Dictionary<string, TxtRec>();
            m_Downloader = new GameObject("uvMap Downloader");
            foreach (JSONObject mapObject in jsonObject[MapsKey].AsArray)
            {
                m_MapNodes.Add(new UvMapNode(mapObject));
            }

        }

        /// <summary>
        /// Calls the given completion callback (typically supplied as a lambda) with the texture corresponding to the given url
        /// </summary>
        /// <param name="url">A string representing the url to the texture</param>
        /// <param name="completion">Callback (lambda) to call.  Will be called with the texture object</param>
        private void withTexture(string url, Action<Texture2D> completion)
        {
            if (!m_Textures.ContainsKey(url))
            {
                m_Textures[url] = m_Downloader.AddComponent<TxtRec>();
                m_Textures[url].start_load(url);
            }

            m_Textures[url].withTexture(completion);
        }

        private UvMapNode GetNamedNode(string mapName)
        {
            foreach (UvMapNode n in m_MapNodes)
            {
                if (n.id == mapName)
                    return n;
            }
            Debug.LogError("\"" + mapName + "\" not present in uvMaps");
            return m_MapNodes[0];
        }

        /// <summary>
        /// Calls the given completion callback (typically supplied as a lambda) with both the "toFormat" and "fromFormat" textures
        /// that are associated with the map identified by "mapName"
        /// </summary>
        /// <param name="url">A string representing the manName - the entity in tranformInfo</param>
        /// <param name="completion">Callback (lambda) to call.  Will be called with the two textures</param>
        public void withMaps(string mapName, Action<Texture2D, Texture2D> completion)
        {
            withTexture(GetNamedNode(mapName).toFormatUrl, (Texture2D toTxt) =>
            {
                withTexture(GetNamedNode(mapName).fromFormatUrl, (Texture2D fromTxt) =>
                {
                    completion(toTxt, fromTxt);
                });
            });
        }

        // TEMP - testing code
        public void withMaps(Action<Texture2D, Texture2D> completion)
        {
            withMaps(m_MapNodes[0].id, completion);
        }

        public override void UpdateShader(Pixvana.Video.ShaderSelector ss, Action completion)
        {
            withMaps((Texture2D toFormat, Texture2D fromFormat) =>
            {
                ss.uvMaps = new Texture2D[2] { toFormat, fromFormat };
                ss.discontinuities = new Vector2[] { new Vector2(.49f, 0.18f), new Vector2(.51f, 1.5f), new Vector2(.51f, 0.18f) };
                completion();
            });
        }

    }

}
