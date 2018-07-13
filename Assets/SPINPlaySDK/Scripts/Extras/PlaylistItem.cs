using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using SimpleJSON;

namespace Pixvana.Extras
{
    public class PlaylistItem
    {
        #region Keys

        private const string TitleKey           = "title";
        private const string UrlKey             = "url";
        private const string AutoPlayKey        = "autoPlay";
        private const string LoopKey            = "loop";

        #endregion

        private Uri m_SourceUri = null;

        private string m_Title = null;
        public string title
        {
            get { return m_Title; }
            set { m_Title = value; }
        }

        private Uri m_Url = null;
        public Uri url
        {
            get { return m_Url; }
            set { m_Url = value; }
        }

        private bool m_AutoPlay = true;
        public bool autoPlay
        {
            get { return m_AutoPlay; }
            set { m_AutoPlay = value; }
        }

        private bool m_Loop = false;
        public bool loop
        {
            get { return m_Loop; }
            set { m_Loop = value; }
        }

        public PlaylistItem(string title, Uri url)
        {
            this.title = title;
            this.url = url;
        }

        public PlaylistItem(JSONObject jsonObject, Uri sourceUri = null)
        {
            Assert.IsNotNull(jsonObject, "jsonObject cannot be null");

            // See: http://wiki.unity3d.com/index.php/SimpleJSON

            m_SourceUri = sourceUri;

            // Title
            title = (jsonObject[TitleKey] ?? "");

            // URL
            string urlString = jsonObject[UrlKey];
            Assert.IsTrue(!string.IsNullOrEmpty(urlString), UrlKey + " is required");
            url = EnsureAbsoluteUri(new Uri(urlString, UriKind.RelativeOrAbsolute));

            // loop
            if (!string.IsNullOrEmpty(jsonObject[LoopKey]))
            {
                m_Loop = jsonObject[LoopKey].AsBool;
            }

            // autoPlay
            if (!string.IsNullOrEmpty(jsonObject[AutoPlayKey]))
            {
                m_AutoPlay = jsonObject[AutoPlayKey].AsBool;
            }
        }

        public override string ToString()
        {
            return string.Format("title: {0}, url: {1}, autoPlay: {2}, loop: {3}",
                this.title, this.url, this.autoPlay, this.loop);
        }

        /// <summary>
        /// Ensures that a URI is absolute.
        /// </summary>
        /// <param name="uri">An absolute or relative URI.</param>
        /// <returns></returns>
        private Uri EnsureAbsoluteUri(Uri uri)
        {
            Uri absoluteUri = uri;

            // If this is a relative URI and we have an absolute source, convert to an absolute URI
            if (!uri.IsAbsoluteUri &&
                m_SourceUri != null)
            {
                string absoluteUriString = m_SourceUri.AbsoluteUri;

                int lastSlashPosition = absoluteUriString.LastIndexOf("/");
                if (lastSlashPosition > -1)
                {
                    absoluteUri = new Uri(absoluteUriString.Substring(0, lastSlashPosition) + "/" + uri, UriKind.Absolute);
                }
                Debug.Assert(absoluteUri.IsAbsoluteUri, "Could not create absolute Uri from: " + uri);
            }

            return absoluteUri;
        }
    }
}