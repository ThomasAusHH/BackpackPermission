using System;
using System.Collections.Generic;
using System.Linq;

namespace BackpackPermission.Permissions
{
    /// <summary>
    /// The local player's permission list: which players may access the pack on their back,
    /// plus the two global switches. Persisted through <see cref="ModConfig"/> and exposed to the
    /// lobby via <see cref="RuleSync"/>. Raises <see cref="Changed"/> after every modification.
    /// </summary>
    internal sealed class LocalPermissions
    {
        private const char KeySeparator = ',';

        private readonly ModConfig _config;
        private readonly HashSet<PlayerKey> _allowed = new HashSet<PlayerKey>();

        public LocalPermissions(ModConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Load();
        }

        /// <summary>Raised after any change to the list or the switches.</summary>
        public event Action Changed;

        public bool AllowEveryone
        {
            get => _config.AllowEveryone;
            private set
            {
                _config.AllowEveryone = value;
                Changed?.Invoke();
            }
        }

        public bool UnlockWhilePassedOut
        {
            get => _config.UnlockWhilePassedOut;
            private set
            {
                _config.UnlockWhilePassedOut = value;
                Changed?.Invoke();
            }
        }

        /// <summary>True when the player is explicitly on the list, regardless of <see cref="AllowEveryone"/>.</summary>
        public bool IsListed(Photon.Realtime.Player player)
        {
            return PlayerKey.TryFrom(player, out PlayerKey key) && _allowed.Contains(key);
        }

        /// <summary>True when the player would be granted access by the list or the everyone switch.</summary>
        public bool IsGranted(Photon.Realtime.Player player) => AllowEveryone || IsListed(player);

        public int CountGranted(IEnumerable<Photon.Realtime.Player> players) => players.Count(IsGranted);

        public void Toggle(Photon.Realtime.Player player)
        {
            if (!PlayerKey.TryFrom(player, out PlayerKey key))
            {
                return;
            }

            bool nowAllowed = _allowed.Add(key);
            if (!nowAllowed)
            {
                _allowed.Remove(key);
            }

            Plugin.Log.LogInfo($"Backpack access for {player.NickName}: {(nowAllowed ? "allowed" : "locked")}");
            Save();
            Changed?.Invoke();
        }

        public void ToggleAllowEveryone()
        {
            AllowEveryone = !AllowEveryone;
            Plugin.Log.LogInfo($"Allow everyone: {(AllowEveryone ? "on" : "off")}");
        }

        public void ToggleUnlockWhilePassedOut()
        {
            UnlockWhilePassedOut = !UnlockWhilePassedOut;
            Plugin.Log.LogInfo($"Unlock while passed out: {(UnlockWhilePassedOut ? "on" : "off")}");
        }

        /// <summary>Snapshot of the current state as the rule other clients evaluate.</summary>
        public AccessRule ToRule() => new AccessRule(AllowEveryone, UnlockWhilePassedOut, _allowed);

        private void Load()
        {
            _allowed.Clear();
            if (!_config.RememberAllowedPlayers)
            {
                return;
            }

            foreach (string part in _config.AllowedPlayers.Split(new[] { KeySeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (PlayerKey.TryParse(part, out PlayerKey key))
                {
                    _allowed.Add(key);
                }
            }
            Plugin.LogVerbose($"Allow list loaded: {_allowed.Count} entries, everyone={AllowEveryone}");
        }

        private void Save()
        {
            if (!_config.RememberAllowedPlayers)
            {
                return;
            }
            // Actor numbers are only meaningful inside the current room, so only persistent keys are stored.
            _config.AllowedPlayers = string.Join(KeySeparator.ToString(), _allowed.Where(k => k.IsPersistent).Select(k => k.Value));
        }
    }
}
