using BackpackPermission.Permissions;
using HarmonyLib;

namespace BackpackPermission.Patches
{
    /// <summary>
    /// Client-side enforcement for stashing. The stash RPC is broadcast to everyone and the
    /// sender empties its own slot locally, so rejecting it on the receiving side would desync
    /// inventories. Blocking on the sender keeps the item in the sender's hand instead.
    /// </summary>
    [HarmonyPatch(typeof(CharacterBackpackHandler), nameof(CharacterBackpackHandler.StashInBackpack))]
    internal static class BackpackStashPatches
    {
        private static bool Prefix(CharacterBackpackHandler __instance, Character interactor)
        {
            if (interactor == null || !interactor.IsLocal)
            {
                return true;
            }

            // CharacterBackpackHandler.character is private in the game assembly.
            Character wearer = __instance.GetComponent<Character>();
            if (AccessPolicy.LocalPlayerMayAccess(wearer))
            {
                return true;
            }

            Plugin.LogVerbose($"Blocked stashing into {wearer.characterName}'s backpack (local check).");
            return false;
        }
    }
}
