using BackpackPermission.Permissions;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace BackpackPermission.Patches
{
    /// <summary>
    /// Records which player a pack belonged to when it hits the ground. Both drop paths run as
    /// RPCs on every client and let the master client spawn the item, so the master can pair the
    /// dropping character with the spawned object and feed the <see cref="DroppedPackRegistry"/>.
    /// </summary>
    [HarmonyPatch(typeof(CharacterItems))]
    internal static class DropTrackingPatches
    {
        /// <summary>Inventory slot that holds the worn pack.</summary>
        private const byte BackpackSlot = 3;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CharacterItems.DropItemRpc))]
        private static void DropItemRpc_Prefix()
        {
            SpawnTracker.Reset();
        }

        /// <summary>Manual drop: the wearer put the pack down.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(CharacterItems.DropItemRpc))]
        private static void DropItemRpc_Postfix(CharacterItems __instance, byte slotID)
        {
            RegisterIfBackpack(__instance, slotID, DropCause.Manual);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(CharacterItems.DropItemFromSlotRPC))]
        private static void DropItemFromSlotRPC_Prefix()
        {
            SpawnTracker.Reset();
        }

        /// <summary>Drop of a whole slot: used when a character dies or is revived.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(CharacterItems.DropItemFromSlotRPC))]
        private static void DropItemFromSlotRPC_Postfix(CharacterItems __instance, byte slotID)
        {
            RegisterIfBackpack(__instance, slotID, DropCause.Death);
        }

        private static void RegisterIfBackpack(CharacterItems items, byte slotID, DropCause cause)
        {
            if (!PhotonNetwork.IsMasterClient || slotID != BackpackSlot || Plugin.DroppedPacks == null)
            {
                return;
            }

            GameObject spawned = SpawnTracker.Take();
            Backpack pack = spawned != null ? spawned.GetComponent<Backpack>() : null;
            PhotonView packView = pack != null ? pack.photonView : null;
            // CharacterItems declares its own private "photonView" field that hides the public base property,
            // so the view is fetched from the component instead of through the member.
            PhotonView characterView = items != null ? items.GetComponent<PhotonView>() : null;
            Photon.Realtime.Player owner = characterView != null ? characterView.Owner : null;
            if (packView == null || owner == null)
            {
                return;
            }
            Plugin.DroppedPacks.Register(packView.ViewID, owner.ActorNumber, cause);
        }
    }

    /// <summary>Captures the most recent room item the master spawned, so drop RPCs can identify their pack.</summary>
    [HarmonyPatch(typeof(PhotonNetwork), nameof(PhotonNetwork.InstantiateItemRoom))]
    internal static class SpawnTracker
    {
        private static GameObject _last;

        private static void Postfix(GameObject __result)
        {
            _last = __result;
        }

        public static void Reset()
        {
            _last = null;
        }

        public static GameObject Take()
        {
            GameObject result = _last;
            _last = null;
            return result;
        }
    }
}
