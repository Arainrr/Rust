﻿using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Marker Manager", "Mr. Blue", "0.0.3")]
    [Description("Add markers to objects in the server")]
    class MarkerManager : HurtworldPlugin
    {
        #region Config
        private PluginConfig config;

        public class PluginConfig
        {
            public Dictionary<string, Maker> Markers = new Dictionary<string, Maker>();

            public class Maker
            {
                public bool Enabled = false;
                public string Name = "Unknown";
                public string MarkerGuid = "b103a49eb66935a4ab08c236dcae21a2";
                public string Color = "#ffffff";
                public bool ShowInCompass = false;
                public float Scale = 100f;

                private GameObject getMarkerPrefab() => RuntimeHurtDB.Instance.GetObjectByGuid(MarkerGuid).Object as GameObject;
                private Vector3 getMarkerScale() => new Vector3(Scale, Scale, Scale);

                private Color getMarkerColor()
                {
                    Color col;
                    ColorUtility.TryParseHtmlString(Color, out col);
                    return col;
                }

                public MapMarkerData getMarker()
                {
                    MapMarkerData marker = new MapMarkerData();
                    marker.Color = getMarkerColor();
                    marker.Label = Name;
                    marker.Prefab = getMarkerPrefab();
                    marker.ShowInCompass = ShowInCompass;
                    marker.Scale = getMarkerScale();
                    marker.Global = true;
                    return marker;
                }
            }
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            PluginConfig newConfig = new PluginConfig();

            foreach (HNetworkView hNetworkView in Resources.FindObjectsOfTypeAll<HNetworkView>())
            {
                if (hNetworkView == null) continue;
                if (hNetworkView.name.Contains("(Clone)"))
                {
                    if (!newConfig.Markers.ContainsKey(hNetworkView.name))
                    {
                        PluginConfig.Maker marker = new PluginConfig.Maker();
                        marker.Name = hNetworkView.name.Replace("(Clone)", "");
                        newConfig.Markers.Add(hNetworkView.name, marker);
                    }
                }
            }

            return newConfig;
        }
        #endregion

        #region Markers
        private void OnServerInitialized()
        {
            config = Config.ReadObject<PluginConfig>();

            foreach (HNetworkView hNetworkView in Resources.FindObjectsOfTypeAll<HNetworkView>())
            {
                if (hNetworkView == null) continue;
                PluginConfig.Maker marker = NeedsMarker(hNetworkView);
                if (marker != null)
                    CreateMarker(hNetworkView, marker);
            }
        }

        void Unload()
        {
            foreach (MarkerDestroyer markerDestroyer in Resources.FindObjectsOfTypeAll<MarkerDestroyer>())
            {
                if(markerDestroyer != null)
                    UnityEngine.Object.Destroy(markerDestroyer);
            }
        }

        void OnEntitySpawned(HNetworkView data)
        {
            PluginConfig.Maker marker = NeedsMarker(data);
            if (marker != null)
                CreateMarker(data, marker);
        }

        public PluginConfig.Maker NeedsMarker(HNetworkView hNetworkView)
        {
            if (hNetworkView.gameObject.activeSelf)
            {
                if (config.Markers.ContainsKey(hNetworkView?.name))
                {
                    if (hNetworkView.gameObject.GetComponent<MarkerDestroyer>() != null)
                        UnityEngine.Object.Destroy(hNetworkView.gameObject.GetComponent<MarkerDestroyer>());
                    
                    PluginConfig.Maker marker = config.Markers[hNetworkView?.name];
                    if (marker.Enabled)
                    {
                        return marker;
                    }
                }
            }
            return null;
        }
        public void CreateMarker(HNetworkView networkView, PluginConfig.Maker marker)
        {
            MarkerDestroyer markerDestoryer = networkView.gameObject.GetOrAddComponent<MarkerDestroyer>();
            MapMarkerData mapMarker = marker.getMarker();
            MapManagerServer.Instance.RegisterMarker(mapMarker);
            NetworkedMapMarkerServer networkedMapMarkerServer = mapMarker.NetworkObject;
            networkedMapMarkerServer.TrackedTransform = networkView.gameObject.transform;
            markerDestoryer.data = mapMarker;
        }
        #endregion
        public class MarkerDestroyer : SlowUpdater
        {
            public MapMarkerData data;
            public override void OnNetDestroy()
            {
                MapManagerServer.Instance.DeregisterMarker(data);
                Destroy(this);
                base.OnNetDestroy();
            }
            public override void OnDestroy()
            {
                MapManagerServer.Instance.DeregisterMarker(data);
                Destroy(this);
                base.OnDestroy();
            }
        }
    }
}