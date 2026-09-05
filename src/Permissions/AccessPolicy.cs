using Photon.Pun;

namespace BackpackPermission.Permissions
{
    /// <summary>Outcome of an access check, with the reason access was granted.</summary>
    internal enum AccessVerdict
    {
        /// <summary>The wearer's own rule lists the requester or allows everyone.</summary>
        GrantedByRule,
        /// <summary>The host's team rule puts wearer and requester in the same team, or allows everyone.</summary>
        GrantedByTeam,
        /// <summary>The requester is the wearer.</summary>
        GrantedSelf,
        /// <summary>Neither the host nor the wearer published a rule, so vanilla behaviour applies.</summary>
        GrantedNoRule,
        /// <summary>The wearer is dead; the pack drops anyway.</summary>
        GrantedDead,
        /// <summary>The wearer is passed out and the applicable rule unlocks the pack in that state.</summary>
        GrantedPassedOut,
        Denied
    }

    internal static class AccessVerdictExtensions
    {
        public static bool IsGranted(this AccessVerdict verdict) => verdict != AccessVerdict.Denied;
    }

    /// <summary>
    /// Decides whether a player may access the pack worn by a character. When the host controls
    /// the lobby, only the host's team rule counts; otherwise the wearer's own published rule
    /// applies. Every client and the host evaluate the same data and reach the same result.
    /// </summary>
    internal static class AccessPolicy
    {
        public static AccessVerdict Evaluate(Character wearer, Photon.Realtime.Player requester)
        {
            if (wearer == null || requester == null || wearer.isBot)
            {
                return AccessVerdict.GrantedNoRule;
            }

            Photon.Realtime.Player owner = wearer.photonView != null ? wearer.photonView.Owner : null;
            if (owner == null)
            {
                return AccessVerdict.GrantedNoRule;
            }
            if (owner.ActorNumber == requester.ActorNumber)
            {
                return AccessVerdict.GrantedSelf;
            }

            CharacterData state = wearer.data;
            if (state != null && state.dead)
            {
                return AccessVerdict.GrantedDead;
            }
            bool passedOut = state != null && (state.passedOut || state.fullyPassedOut);

            if (LobbySync.IsHostControlled(out LobbyRule lobby))
            {
                if (lobby.UnlockWhilePassedOut && passedOut)
                {
                    return AccessVerdict.GrantedPassedOut;
                }
                return lobby.Grants(owner, requester) ? AccessVerdict.GrantedByTeam : AccessVerdict.Denied;
            }

            if (!RuleSync.TryReadRule(owner, out AccessRule rule))
            {
                return AccessVerdict.GrantedNoRule;
            }
            if (rule.UnlockWhilePassedOut && passedOut)
            {
                return AccessVerdict.GrantedPassedOut;
            }
            return rule.Grants(requester) ? AccessVerdict.GrantedByRule : AccessVerdict.Denied;
        }

        public static bool IsAllowed(Character wearer, Photon.Realtime.Player requester)
        {
            return Evaluate(wearer, requester).IsGranted();
        }

        /// <summary>Whether the local player may access the pack worn by <paramref name="wearer"/>.</summary>
        public static bool LocalPlayerMayAccess(Character wearer)
        {
            return IsAllowed(wearer, PhotonNetwork.LocalPlayer);
        }

        /// <summary>
        /// Whether the local player may access the pack behind a wheel reference. Packs lying on
        /// the ground are always accessible, as in the base game.
        /// </summary>
        public static bool LocalPlayerMayAccess(BackpackReference reference)
        {
            if (reference.type != BackpackReference.BackpackType.Equipped || reference.view == null)
            {
                return true;
            }
            Character wearer = reference.view.GetComponent<Character>();
            return wearer == null || wearer.IsLocal || LocalPlayerMayAccess(wearer);
        }

        /// <summary>True when the pack of another player is locked for the local player.</summary>
        public static bool IsLockedForLocalPlayer(Character wearer)
        {
            return wearer != null && !wearer.IsLocal && !LocalPlayerMayAccess(wearer);
        }
    }
}
