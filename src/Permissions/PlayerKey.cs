using System;

namespace BackpackPermission.Permissions
{
    /// <summary>
    /// Stable identity of a Photon player as used in permission lists.
    /// Prefers the platform user id (the Steam ID), which survives reconnects and sessions,
    /// and falls back to the room-local actor number when no user id is available.
    /// </summary>
    internal readonly struct PlayerKey : IEquatable<PlayerKey>
    {
        private const string UserPrefix = "u:";
        private const string ActorPrefix = "a:";

        private PlayerKey(string value)
        {
            Value = value;
        }

        /// <summary>Serialized form, e.g. <c>u:7656119...</c> or <c>a:3</c>.</summary>
        public string Value { get; }

        /// <summary>True when the key is based on the user id and therefore valid across sessions.</summary>
        public bool IsPersistent => Value != null && Value.StartsWith(UserPrefix, StringComparison.Ordinal);

        public static PlayerKey ForActor(int actorNumber) => new PlayerKey(ActorPrefix + actorNumber);

        public static bool TryFrom(Photon.Realtime.Player player, out PlayerKey key)
        {
            if (player == null)
            {
                key = default;
                return false;
            }
            key = string.IsNullOrEmpty(player.UserId)
                ? ForActor(player.ActorNumber)
                : new PlayerKey(UserPrefix + player.UserId);
            return true;
        }

        public static bool TryParse(string raw, out PlayerKey key)
        {
            string trimmed = raw?.Trim();
            if (!string.IsNullOrEmpty(trimmed) &&
                (trimmed.StartsWith(UserPrefix, StringComparison.Ordinal) || trimmed.StartsWith(ActorPrefix, StringComparison.Ordinal)) &&
                trimmed.Length > UserPrefix.Length)
            {
                key = new PlayerKey(trimmed);
                return true;
            }
            key = default;
            return false;
        }

        public bool Equals(PlayerKey other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is PlayerKey other && Equals(other);

        public override int GetHashCode() => Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0;

        public override string ToString() => Value ?? string.Empty;
    }
}
