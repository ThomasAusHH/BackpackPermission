using System;
using System.Collections.Generic;
using System.Linq;

namespace BackpackPermission.Permissions
{
    /// <summary>
    /// The access rule a wearer publishes to the lobby. Immutable.
    /// Wire format: <c>1|&lt;everyone&gt;|&lt;passedOut&gt;|&lt;key,key,...&gt;</c> with
    /// <c>1</c>/<c>0</c> for the flags and <see cref="PlayerKey"/> values for the list.
    /// </summary>
    internal sealed class AccessRule
    {
        public const int FormatVersion = 1;

        private const char FieldSeparator = '|';
        private const char KeySeparator = ',';

        private readonly HashSet<PlayerKey> _allowedPlayers;

        public AccessRule(bool allowEveryone, bool unlockWhilePassedOut, IEnumerable<PlayerKey> allowedPlayers)
        {
            AllowEveryone = allowEveryone;
            UnlockWhilePassedOut = unlockWhilePassedOut;
            _allowedPlayers = new HashSet<PlayerKey>(allowedPlayers ?? Enumerable.Empty<PlayerKey>());
        }

        public bool AllowEveryone { get; }

        public bool UnlockWhilePassedOut { get; }

        public IReadOnlyCollection<PlayerKey> AllowedPlayers => _allowedPlayers;

        /// <summary>
        /// Whether the rule grants access to <paramref name="requester"/> on its own, ignoring the
        /// wearer's state (self access, death and unconsciousness are handled by <see cref="AccessPolicy"/>).
        /// </summary>
        public bool Grants(Photon.Realtime.Player requester)
        {
            if (AllowEveryone)
            {
                return true;
            }
            if (!PlayerKey.TryFrom(requester, out PlayerKey key))
            {
                return false;
            }
            // The wearer may have seen the requester without a user id and stored the actor key instead.
            return _allowedPlayers.Contains(key) || _allowedPlayers.Contains(PlayerKey.ForActor(requester.ActorNumber));
        }

        public string Serialize()
        {
            string keys = string.Join(KeySeparator.ToString(), _allowedPlayers.Select(k => k.Value));
            return string.Join(FieldSeparator.ToString(), FormatVersion.ToString(), Flag(AllowEveryone), Flag(UnlockWhilePassedOut), keys);
        }

        public static bool TryParse(string raw, out AccessRule rule)
        {
            rule = null;
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            string[] fields = raw.Split(FieldSeparator);
            if (fields.Length < 4 || fields[0] != FormatVersion.ToString())
            {
                return false;
            }

            List<PlayerKey> keys = new List<PlayerKey>();
            foreach (string part in fields[3].Split(new[] { KeySeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (PlayerKey.TryParse(part, out PlayerKey key))
                {
                    keys.Add(key);
                }
            }

            rule = new AccessRule(fields[1] == "1", fields[2] == "1", keys);
            return true;
        }

        private static string Flag(bool value) => value ? "1" : "0";
    }
}
