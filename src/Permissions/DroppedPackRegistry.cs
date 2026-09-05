using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

namespace BackpackPermission.Permissions
{
    /// <summary>Why a pack ended up on the ground.</summary>
    internal enum DropCause
    {
        /// <summary>The wearer put it down.</summary>
        Manual,
        /// <summary>The wearer died (or was revived and dropped everything).</summary>
        Death
    }

    /// <summary>
    /// Remembers who last wore a pack that now lies on the ground, and why it dropped. The master
    /// client is the only one who sees the drop and the spawned object in the same frame, so it
    /// keeps the authoritative map and shares it through a room property for every other client.
    /// </summary>
    internal sealed class DroppedPackRegistry
    {
        /// <summary>Custom property key on the Photon room.</summary>
        public const string PropertyKey = "bpk_drops";

        private const float CleanupIntervalSeconds = 2f;
        private const char EntrySeparator = ',';
        private const char FieldSeparator = ':';

        private readonly Dictionary<int, Entry> _local = new Dictionary<int, Entry>();
        private bool _dirty;
        private float _lastCleanupTime;

        private static string _cachedRaw;
        private static Dictionary<int, Entry> _cachedEntries = new Dictionary<int, Entry>();

        private struct Entry
        {
            public int OwnerActor;
            public DropCause Cause;
        }

        /// <summary>Host only: records a pack that has just been spawned on the ground.</summary>
        public void Register(int viewId, int ownerActor, DropCause cause)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }
            _local[viewId] = new Entry { OwnerActor = ownerActor, Cause = cause };
            _dirty = true;
            Plugin.LogVerbose($"Dropped pack {viewId} registered: owner actor {ownerActor}, cause {cause}");
        }

        /// <summary>
        /// Looks up the last wearer of a pack on the ground. On the master this reads the local map
        /// (the room property lags one round trip), everywhere else the room property.
        /// </summary>
        public static bool TryGet(int viewId, out int ownerActor, out DropCause cause)
        {
            ownerActor = 0;
            cause = DropCause.Manual;

            Dictionary<int, Entry> entries;
            if (PhotonNetwork.IsMasterClient && Plugin.DroppedPacks != null)
            {
                entries = Plugin.DroppedPacks._local;
            }
            else
            {
                entries = ReadShared();
            }

            if (entries == null || !entries.TryGetValue(viewId, out Entry entry))
            {
                return false;
            }
            ownerActor = entry.OwnerActor;
            cause = entry.Cause;
            return true;
        }

        /// <summary>Call once per frame. Drops entries whose objects are gone and publishes changes.</summary>
        public void Tick()
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            {
                if (_local.Count > 0)
                {
                    _local.Clear();
                }
                return;
            }

            if (Time.unscaledTime - _lastCleanupTime > CleanupIntervalSeconds)
            {
                _lastCleanupTime = Time.unscaledTime;
                RemoveStaleEntries();
            }

            if (_dirty)
            {
                Publish();
            }
        }

        private void RemoveStaleEntries()
        {
            List<int> stale = null;
            foreach (int viewId in _local.Keys)
            {
                PhotonView view = PhotonView.Find(viewId);
                Item item = view != null ? view.GetComponent<Item>() : null;
                if (item == null || item.itemState != ItemState.Ground)
                {
                    (stale ?? (stale = new List<int>())).Add(viewId);
                }
            }
            if (stale == null)
            {
                return;
            }
            foreach (int viewId in stale)
            {
                _local.Remove(viewId);
            }
            _dirty = true;
        }

        private void Publish()
        {
            string serialized = string.Join(EntrySeparator.ToString(),
                _local.Select(e => $"{e.Key}{FieldSeparator}{e.Value.OwnerActor}{FieldSeparator}{(int)e.Value.Cause}"));
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { PropertyKey, serialized } });
            _dirty = false;
            Plugin.LogVerbose($"Dropped packs published: {(serialized.Length == 0 ? "(none)" : serialized)}");
        }

        private static Dictionary<int, Entry> ReadShared()
        {
            Hashtable properties = PhotonNetwork.CurrentRoom?.CustomProperties;
            if (properties == null || !properties.TryGetValue(PropertyKey, out object raw) || !(raw is string serialized))
            {
                return null;
            }
            if (ReferenceEquals(serialized, _cachedRaw) || serialized == _cachedRaw)
            {
                return _cachedEntries;
            }

            Dictionary<int, Entry> entries = new Dictionary<int, Entry>();
            foreach (string part in serialized.Split(new[] { EntrySeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] fields = part.Split(FieldSeparator);
                if (fields.Length == 3 && int.TryParse(fields[0], out int viewId) && int.TryParse(fields[1], out int actor)
                    && int.TryParse(fields[2], out int cause) && Enum.IsDefined(typeof(DropCause), cause))
                {
                    entries[viewId] = new Entry { OwnerActor = actor, Cause = (DropCause)cause };
                }
            }
            _cachedRaw = serialized;
            _cachedEntries = entries;
            return entries;
        }
    }
}
