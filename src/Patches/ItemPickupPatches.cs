using BackpackPermission.Permissions;
using HarmonyLib;
using Photon.Pun;

namespace BackpackPermission.Patches
{
    /// <summary>
    /// Host-side enforcement. Every pickup is requested from the master client, which either
    /// accepts it or answers with <c>DenyPickupRPC</c>. Denying here protects the wearer even
    /// against players who do not run the mod, as long as the host does.
    /// </summary>
    [HarmonyPatch(typeof(Item), nameof(Item.RequestPickup))]
    internal static class ItemPickupPatches
    {
        private static bool Prefix(Item __instance, PhotonView characterView)
        {
            if (__instance.itemState != ItemState.InBackpack || __instance.backpackReference.IsNone)
            {
                return true;
            }

            BackpackReference reference = __instance.backpackReference.Value.Item2;
            if (reference.type != BackpackReference.BackpackType.Equipped || reference.view == null)
            {
                return true;
            }

            Character wearer = reference.view.GetComponent<Character>();
            Photon.Realtime.Player requester = characterView != null ? characterView.Owner : null;
            if (AccessPolicy.IsAllowed(wearer, requester))
            {
                return true;
            }

            Plugin.Log.LogInfo($"Denied {requester?.NickName} taking an item from {wearer.characterName}'s backpack (host check).");
            if (requester != null)
            {
                // Item.view is protected in the game assembly; the public PhotonView property is the safe way in.
                __instance.photonView.RPC("DenyPickupRPC", requester);
            }
            return false;
        }
    }
}
