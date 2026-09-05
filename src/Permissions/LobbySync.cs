using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

namespace BackpackPermission.Permissions
{
    /// <summary>
    /// Publishes the host's <see cref="LobbyRule"/> as a Photon room property while the local
    /// player is the master client, and reads it on every client. A rule is only honoured while
    /// the master client that wrote it is still the master, so a host change to a player without
    /// the mod falls back to individual permissions automatically.
    /// </summary>
    internal sealed class LobbySync : IDisposable
    {
        /// <summary>Custom property key on the Photon room.</summary>
        public const string PropertyKey = "bpk_lobby";

        private const float VerifyIntervalSeconds = 3f;

        private readonly HostSettings _settings;
        private string _publishedRule;
        private float _lastPublishTime;
        private bool _dirty = true;

        public LobbySync(HostSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settings.Changed += MarkDirty;
        }

        public void Dispose()
        {
            _settings.Changed -= MarkDirty;
        }

        /// <summary>Reads the current, still valid lobby rule. False when no rule exists or the host that wrote it left.</summary>
        public static bool TryReadRule(out LobbyRule rule)
        {
            rule = null;
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || PhotonNetwork.MasterClient == null)
            {
                return false;
            }

            Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;
            if (properties == null || !properties.TryGetValue(PropertyKey, out object raw) || !(raw is string serialized))
            {
                return false;
            }
            if (!LobbyRule.TryParse(serialized, out LobbyRule parsed) || parsed.HostActorNumber != PhotonNetwork.MasterClient.ActorNumber)
            {
                return false;
            }

            rule = parsed;
            return true;
        }

        /// <summary>True when a valid host rule puts the lobby under host control.</summary>
        public static bool IsHostControlled(out LobbyRule rule)
        {
            return TryReadRule(out rule) && rule.Mode == LobbyMode.HostControlled;
        }

        /// <summary>Call once per frame.</summary>
        public void Tick()
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || PhotonNetwork.LocalPlayer == null)
            {
                _publishedRule = null;
                _dirty = true;
                return;
            }

            if (_dirty)
            {
                Publish();
                return;
            }

            if (Time.unscaledTime - _lastPublishTime > VerifyIntervalSeconds && !IsPropertyUpToDate())
            {
                Publish();
            }
        }

        private void MarkDirty() => _dirty = true;

        private bool IsPropertyUpToDate()
        {
            Hashtable properties = PhotonNetwork.CurrentRoom?.CustomProperties;
            return properties != null
                   && properties.TryGetValue(PropertyKey, out object raw)
                   && raw is string current
                   && current == _publishedRule;
        }

        private void Publish()
        {
            string serialized = _settings.ToRule(PhotonNetwork.LocalPlayer.ActorNumber).Serialize();
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { PropertyKey, serialized } });

            _publishedRule = serialized;
            _lastPublishTime = Time.unscaledTime;
            _dirty = false;
            Plugin.LogVerbose($"Lobby rule published: {serialized}");
        }
    }
}
