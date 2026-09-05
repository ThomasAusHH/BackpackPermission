using System;
using System.Collections.Generic;
using System.Linq;

namespace BackpackPermission.Permissions
{
    /// <summary>Who decides about backpack access in the current lobby.</summary>
    public enum LobbyMode
    {
        /// <summary>Every wearer manages their own permission list.</summary>
        Individual,
        /// <summary>The host assigns teams; team mates may access each other's packs.</summary>
        HostControlled
    }

    /// <summary>
    /// The lobby-wide rule the host publishes as a room property. Immutable.
    /// Wire format: <c>1|&lt;mode&gt;|&lt;hostActor&gt;|&lt;passedOut&gt;|&lt;everyone&gt;|&lt;key=team,...&gt;</c>.
    /// The host actor number lets clients detect a stale rule after the host left.
    /// </summary>
    internal sealed class LobbyRule
    {
        public const int FormatVersion = 1;

        /// <summary>Teams are numbered 1..<see cref="TeamCount"/>; <see cref="NoTeam"/> means unassigned.</summary>
        public const int TeamCount = 4;
        public const int NoTeam = 0;

        private const char FieldSeparator = '|';
        private const char EntrySeparator = ',';
        private const char AssignmentSeparator = '=';

        private readonly Dictionary<PlayerKey, int> _teams;

        public LobbyRule(LobbyMode mode, int hostActorNumber, bool unlockWhilePassedOut, bool allowEveryone, IEnumerable<KeyValuePair<PlayerKey, int>> teams)
        {
            Mode = mode;
            HostActorNumber = hostActorNumber;
            UnlockWhilePassedOut = unlockWhilePassedOut;
            AllowEveryone = allowEveryone;
            _teams = new Dictionary<PlayerKey, int>();
            foreach (KeyValuePair<PlayerKey, int> entry in teams ?? Enumerable.Empty<KeyValuePair<PlayerKey, int>>())
            {
                if (entry.Value != NoTeam)
                {
                    _teams[entry.Key] = entry.Value;
                }
            }
        }

        public LobbyMode Mode { get; }

        public int HostActorNumber { get; }

        public bool UnlockWhilePassedOut { get; }

        public bool AllowEveryone { get; }

        public IReadOnlyDictionary<PlayerKey, int> Teams => _teams;

        public int TeamOf(Photon.Realtime.Player player)
        {
            if (player == null || !PlayerKey.TryFrom(player, out PlayerKey key))
            {
                return NoTeam;
            }
            if (_teams.TryGetValue(key, out int team))
            {
                return team;
            }
            return _teams.TryGetValue(PlayerKey.ForActor(player.ActorNumber), out team) ? team : NoTeam;
        }

        /// <summary>Whether the team rule alone grants <paramref name="requester"/> access to <paramref name="wearer"/>'s pack.</summary>
        public bool Grants(Photon.Realtime.Player wearer, Photon.Realtime.Player requester)
        {
            if (AllowEveryone)
            {
                return true;
            }
            int wearerTeam = TeamOf(wearer);
            return wearerTeam != NoTeam && wearerTeam == TeamOf(requester);
        }

        public static string TeamName(int team)
        {
            return team == NoTeam ? null : ((char)('A' + team - 1)).ToString();
        }

        public string Serialize()
        {
            string teams = string.Join(EntrySeparator.ToString(), _teams.Select(t => t.Key.Value + AssignmentSeparator + t.Value));
            return string.Join(FieldSeparator.ToString(), FormatVersion.ToString(), ((int)Mode).ToString(), HostActorNumber.ToString(),
                Flag(UnlockWhilePassedOut), Flag(AllowEveryone), teams);
        }

        public static bool TryParse(string raw, out LobbyRule rule)
        {
            rule = null;
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            string[] fields = raw.Split(FieldSeparator);
            if (fields.Length < 6 || fields[0] != FormatVersion.ToString()
                || !int.TryParse(fields[1], out int mode) || !Enum.IsDefined(typeof(LobbyMode), mode)
                || !int.TryParse(fields[2], out int hostActor))
            {
                return false;
            }

            List<KeyValuePair<PlayerKey, int>> teams = new List<KeyValuePair<PlayerKey, int>>();
            foreach (string entry in fields[5].Split(new[] { EntrySeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                int split = entry.LastIndexOf(AssignmentSeparator);
                if (split > 0 && PlayerKey.TryParse(entry.Substring(0, split), out PlayerKey key)
                    && int.TryParse(entry.Substring(split + 1), out int team) && team >= NoTeam && team <= TeamCount)
                {
                    teams.Add(new KeyValuePair<PlayerKey, int>(key, team));
                }
            }

            rule = new LobbyRule((LobbyMode)mode, hostActor, fields[3] == "1", fields[4] == "1", teams);
            return true;
        }

        private static string Flag(bool value) => value ? "1" : "0";
    }
}
