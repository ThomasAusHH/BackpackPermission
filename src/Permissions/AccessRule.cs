using System;
using System.Collections.Generic;
using System.Linq;

namespace BackpackPermission.Permissions
{
    /// <summary>
    /// The access rule a wearer publishes to the lobby. Immutable.
    /// Wire format: <c>1|&lt;everyone&gt;|&lt;passedOut&gt;|&lt;key,key,...&gt;|&lt;protectDropped&gt;|&lt;protectDeath&gt;</c>
    /// with <c>1</c>/<c>0</c> for the flags and <see cref="PlayerKey"/> values for the list. The two
    /// trailing flags were added in 1.1 and are optional when parsing.
    /// </summary>
    internal sealed class AccessRule
    {
        public const int FormatVersion = 1;

        private const char FieldSeparator = '|';
        private const char KeySeparator = ',';

        private readonly HashSet<PlayerKey> _allowedPlayers;

        public AccessRule(bool allowEveryone, bool unlockWhilePassedOut, IEnumerable<PlayerKey> allowedPlayers,
            bool protectDroppedPack = true, bool protectDeathDrop = false)
        {
            AllowEveryone = allowEveryone;
            UnlockWhilePassedOut = unlockWhilePassedOut;
            ProtectDroppedPack = protectDroppedPack;
            ProtectDeathDrop = protectDeathDrop;
            _allowedPlayers = new HashSet<PlayerKey>(allowedPlayers ?? Enumerable.Empty<PlayerKey>());
        }

        public bool AllowEveryone { get; }

        public bool UnlockWhilePassedOut { get; }

        /// <summary>Whether the list also applies to the pack after the wearer put it down.</summary>
        public bool ProtectDroppedPack { get; }

        /// <summary>Whether the list also applies to the pack after the wearer died.</summary>
        public bool ProtectDeathDrop { get; }

        public IReadOnlyCollection<PlayerKey> AllowedPlayers => _allowedPlayers;

        /// <summary>Whether the pack stays protected after dropping for the given reason.</summary>
        public bool Protects(DropCause cause) => cause == DropCause.Death ? ProtectDeathDrop : ProtectDroppedPack;

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
            return string.Join(FieldSeparator.ToString(), FormatVersion.ToString(), Flag(AllowEveryone), Flag(UnlockWhilePassedOut), keys,
                Flag(ProtectDroppedPack), Flag(ProtectDeathDrop));
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

            // 1.0 clients publish four fields; their dropped packs behave like the 1.0 game did: open.
            bool protectDropped = fields.Length > 4 && fields[4] == "1";
            bool protectDeath = fields.Length > 5 && fields[5] == "1";
            rule = new AccessRule(fields[1] == "1", fields[2] == "1", keys, protectDropped, protectDeath);
            return true;
        }

        private static string Flag(bool value) => value ? "1" : "0";
    }
}
