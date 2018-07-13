using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using SimpleJSON;

namespace Pixvana.Extras
{
    public class Playlist
    {
        #region Keys

        private const string WrapAroundKey          = "wrapAround";
        private const string PlayNextKey            = "playNext";
        private const string ItemsKey               = "items";

        #endregion

        private List<PlaylistItem> m_Items = new List<PlaylistItem>();
        public List<PlaylistItem> items
        {
            get { return m_Items; }
        }

        private bool m_WrapAround = true;
        public bool wrapAround
        {
            get { return m_WrapAround; }
            set { m_WrapAround = value; }
        }

        private bool m_PlayNext = false;
        public bool playNext
        {
            get { return m_PlayNext; }
            set { m_PlayNext = value; }
        }

        private Uri m_SourceUri = null;

        private PlaylistItem m_SelectedItem = null;
        public PlaylistItem selectedItem
        {

            get { return m_SelectedItem; }

            private set
            {
                if (value != m_SelectedItem)
                {
                    m_SelectedItem = value;
                    RaiseSelectedItemChanged(m_SelectedItem);
                }
            }
        }

        public event EventHandler<SelectedItemChangedEventArgs> onSelectedItemChanged;

        public Playlist() { }

        public Playlist(string jsonString, Uri sourceUri = null)
        {
            Assert.IsNotNull(jsonString, "jsonString cannot be null");

            m_SourceUri = sourceUri;

            // See: http://wiki.unity3d.com/index.php/SimpleJSON

            JSONNode jsonObject = JSON.Parse(jsonString);
            Assert.IsNotNull(jsonObject, "error parsing json");

            // wrapAround
            if (!string.IsNullOrEmpty(jsonObject[WrapAroundKey]))
            {
                m_WrapAround = jsonObject[WrapAroundKey].AsBool;
            }

            // playNext
            if (!string.IsNullOrEmpty(jsonObject[PlayNextKey]))
            {
                m_PlayNext = jsonObject[PlayNextKey].AsBool;
            }

            // items
            JSONNode itemsNode = jsonObject[ItemsKey];
            if (itemsNode != null)
            {
                foreach (JSONObject itemObject in itemsNode.AsArray)
                {
                    m_Items.Add(new PlaylistItem(itemObject, m_SourceUri));
                }
            }
        }

        public void Start()
        {
            if (this.items.Count > 0)
            {
                this.SelectItem(this.items[0]);
            }
        }

        public bool SelectItem(PlaylistItem item)
        {
            bool selected = false;

            if (this.items.Contains(item))
            {
                this.selectedItem = item;
                selected = true;
            }

            return selected;
        }

        public void PreviousItem()
        {
            MoveToItem(-1);
        }

        public void NextItem()
        {
            MoveToItem(1);
        }

        public void ItemEnded()
        {
            if (this.playNext)
            {
                NextItem();
            }
        }

        private void MoveToItem(int direction)
        {
            int index = this.items.IndexOf(this.selectedItem);
            if (index > -1)
            {
                index += direction;
                if (index < 0)
                {
                    index = (m_WrapAround ? this.items.Count - 1 : 0);
                }
                else if (index >= this.items.Count)
                {
                    index = (m_WrapAround ? 0 : this.items.Count - 1);
                }

                this.selectedItem = this.items[index];
            }
        }

        protected void RaiseSelectedItemChanged(PlaylistItem item)
        {
            if (onSelectedItemChanged != null)
            {
                onSelectedItemChanged(this, new SelectedItemChangedEventArgs(item));
            }
        }

        public override string ToString()
        {
            string itemsString = "";
            foreach (PlaylistItem item in m_Items)
            {
                itemsString += "\n    " + item;
            }

            return string.Format("playlist\n  wrapAround: {0}, playNext: {1}{2}\n",
                this.wrapAround, this.playNext, itemsString);
        }
    }

    #region EventArgs

    public class SelectedItemChangedEventArgs : EventArgs
    {
        private PlaylistItem m_Item = null;

        public SelectedItemChangedEventArgs(PlaylistItem item)
        {
            m_Item = item;
        }

        public PlaylistItem item { get { return m_Item; } }
    }

    #endregion
}
