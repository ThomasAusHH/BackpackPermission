using System;
using System.Collections.Generic;
using System.Linq;

namespace BackpackPermission.Permissions
{
    /// <summary>
    /// The lobby settings this player applies when hosting: the lobby mode, the team of every
    /// player and the host-wide switches. Persisted through <see cref="ModConfig"/>, published by
    /// <see cref="LobbySync"/> while this player is the master client.
    /// </summary>
    internal sealed class HostSettings
    {
        private const char EntrySeparator = ',';
        private const char AssignmentSeparator = '=';

        private readonly ModConfig _config;
        private readonly Dictionary<PlayerKey, int> _teams = new Dictionary<PlayerKey, int>();

        public HostSettings(ModConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Load();
        }

        /// <summary>Raised after any change.</summary>
        public event Action Changed;

        public LobbyMode Mode
        {
            get => _config.LobbyMode;
            private set
            {
                _config.LobbyMode = value;
                Changed?.Invoke();
            }
        }

        public bool UnlockWhilePassedOut
        {
            get => _config.HostUnlockWhilePassedOut;
            private set
            {
                _config.HostUnlockWhilePassedOut = value;
                Changed?.Invoke();
            }
        }

        public bool AllowEveryone
        {
            get => _config.HostAllowEveryone;
            private set
            {
                _config.HostAllowEveryone = value;
                Changed?.Invoke();
            }
        }

        public int TeamOf(Photon.Realtime.Player player)
        {
            return PlayerKey.TryFrom(player, out PlayerKey key) && _teams.TryGetValue(key, out int team) ? team : LobbyRule.NoTeam;
        }

        /// <summary>Moves the player to the next team: none, A, B, C, D, none, ...</summary>
        public void CycleTeam(Photon.Realtime.Player player)
        {
            if (!PlayerKey.TryFrom(player, out PlayerKey key))
            {
                return;
            }

            int next = (TeamOf(player) + 1) % (LobbyRule.TeamCount + 1);
            if (next == LobbyRule.NoTeam)
            {
                _teams.Remove(key);
            }
            else
            {
                _teams[key] = next;
            }

            Plugin.Log.LogInfo($"Team for {player.NickName}: {LobbyRule.TeamName(next) ?? "none"}");
            Save();
            Changed?.Invoke();
        }

        public void ToggleMode()
        {
            Mode = Mode == LobbyMode.Individual ? LobbyMode.HostControlled : LobbyMode.Individual;
            Plugin.Log.LogInfo($"Lobby mode: {Mode}");
        }

        public void ToggleAllowEveryone()
        {
            AllowEveryone = !AllowEveryone;
            Plugin.Log.LogInfo($"Host: allow everyone {(AllowEveryone ? "on" : "off")}");
        }

        public void ToggleUnlockWhilePassedOut()
        {
            UnlockWhilePassedOut = !UnlockWhilePassedOut;
            Plugin.Log.LogInfo($"Host: unlock while passed out {(UnlockWhilePassedOut ? "on" : "off")}");
        }

        public LobbyRule ToRule(int hostActorNumber)
        {
            return new LobbyRule(Mode, hostActorNumber, UnlockWhilePassedOut, AllowEveryone, _teams);
        }

        private void Load()
        {
            _teams.Clear();
            foreach (string entry in _config.HostTeams.Split(new[] { EntrySeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                int split = entry.LastIndexOf(AssignmentSeparator);
                if (split > 0 && PlayerKey.TryParse(entry.Substring(0, split), out PlayerKey key)
                    && int.TryParse(entry.Substring(split + 1), out int team) && team > LobbyRule.NoTeam && team <= LobbyRule.TeamCount)
                {
                    _teams[key] = team;
                }
            }
            Plugin.LogVerbose($"Host settings loaded: mode={Mode}, {_teams.Count} team assignments");
        }

        private void Save()
        {
            // Actor numbers only exist inside the current room; persist Steam-ID based keys only.
            _config.HostTeams = string.Join(EntrySeparator.ToString(),
                _teams.Where(t => t.Key.IsPersistent).Select(t => t.Key.Value + AssignmentSeparator + t.Value));
        }
    }
}
