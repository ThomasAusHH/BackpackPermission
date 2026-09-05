using BackpackPermission.Permissions;
using HarmonyLib;
using Photon.Pun;

namespace BackpackPermission.Patches
{
    /// <summary>
    /// Host-side enforcement. Every pickup is requested from the master client, which either
    /// accepts it or answers with <c>DenyPickupRPC</c>. Denying here protects the wearer even
    /// against players who do not run the mod, as long as the host does. Covers items inside a
    /// worn pack, items inside a pack on the ground, and picking up a protected pack itself.
    /// </summary>
    [HarmonyPatch(typeof(Item), nameof(Item.RequestPickup))]
    internal static class ItemPickupPatches
    {
        private static bool Prefix(Item __instance, PhotonView characterView)
        {
            Photon.Realtime.Player requester = characterView != null ? characterView.Owner : null;
            if (requester == null)
            {
                return true;
            }

            AccessVerdict verdict;
            string subject;
            if (__instance.itemState == ItemState.InBackpack && __instance.backpackReference.IsSome)
            {
                BackpackReference reference = __instance.backpackReference.Value.Item2;
                if (reference.view == null)
                {
                    return true;
                }
                if (reference.type == BackpackReference.BackpackType.Equipped)
                {
                    Character wearer = reference.view.GetComponent<Character>();
                    verdict = AccessPolicy.Evaluate(wearer, requester);
                    subject = $"{wearer?.characterName}'s backpack";
                }
                else
                {
                    verdict = AccessPolicy.EvaluateDropped(reference.view.ViewID, requester);
                    subject = "a dropped pack";
                }
            }
            else if (__instance is Backpack && __instance.itemState == ItemState.Ground && __instance.photonView != null)
            {
                verdict = AccessPolicy.EvaluateDropped(__instance.photonView.ViewID, requester);
                subject = "a dropped pack (wearing it)";
            }
            else
            {
                return true;
            }

            if (verdict.IsGranted())
            {
                return true;
            }

            Plugin.Log.LogInfo($"Denied {requester.NickName} taking from {subject} (host check).");
            // Item.view is protected in the game assembly; the public PhotonView property is the safe way in.
            __instance.photonView.RPC("DenyPickupRPC", requester);
            return false;
        }
    }
}
