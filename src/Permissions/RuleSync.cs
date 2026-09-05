using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

namespace BackpackPermission.Permissions
{
    /// <summary>
    /// Shares the local rule with the lobby through a Photon custom player property and reads
    /// the rules other players publish. Player properties reach every client, including late
    /// joiners, without any custom RPC.
    /// </summary>
    internal sealed class RuleSync : IDisposable
    {
        /// <summary>Custom property key on <see cref="Photon.Realtime.Player"/>.</summary>
        public const string PropertyKey = "bpk";

        /// <summary>How often the published property is verified against the local state.</summary>
        private const float VerifyIntervalSeconds = 3f;

        private readonly LocalPermissions _permissions;
        private string _publishedRule;
        private string _publishedRoom;
        private float _lastPublishTime;
        private bool _dirty = true;

        public RuleSync(LocalPermissions permissions)
        {
            _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
            _permissions.Changed += MarkDirty;
        }

        public void Dispose()
        {
            _permissions.Changed -= MarkDirty;
        }

        /// <summary>Reads the rule a player has published, if any. Players without the mod have none.</summary>
        public static bool TryReadRule(Photon.Realtime.Player player, out AccessRule rule)
        {
            rule = null;
            Hashtable properties = player?.CustomProperties;
            if (properties == null || !properties.TryGetValue(PropertyKey, out object raw) || !(raw is string serialized))
            {
                return false;
            }
            return AccessRule.TryParse(serialized, out rule);
        }

        /// <summary>Call once per frame. Publishes on change, on room change, and re-publishes if the property went missing.</summary>
        public void Tick()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            {
                _publishedRule = null;
                _publishedRoom = null;
                _dirty = true;
                return;
            }

            string room = PhotonNetwork.CurrentRoom?.Name ?? string.Empty;
            if (_dirty || room != _publishedRoom)
            {
                Publish(room);
                return;
            }

            if (Time.unscaledTime - _lastPublishTime > VerifyIntervalSeconds && !IsPropertyUpToDate())
            {
                Publish(room);
            }
        }

        private void MarkDirty() => _dirty = true;

        private bool IsPropertyUpToDate()
        {
            Hashtable properties = PhotonNetwork.LocalPlayer.CustomProperties;
            return properties != null
                   && properties.TryGetValue(PropertyKey, out object raw)
                   && raw is string current
                   && current == _publishedRule;
        }

        private void Publish(string room)
        {
            string serialized = _permissions.ToRule().Serialize();
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { PropertyKey, serialized } });

            _publishedRule = serialized;
            _publishedRoom = room;
            _lastPublishTime = Time.unscaledTime;
            _dirty = false;
            Plugin.LogVerbose($"Access rule published: {serialized}");
        }
    }
}
