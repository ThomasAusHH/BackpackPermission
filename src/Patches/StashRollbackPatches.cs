using BackpackPermission.Permissions;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using Zorro.Core.Serizalization;

namespace BackpackPermission.Patches
{
    /// <summary>
    /// Host-side enforcement for stashing. The stash RPC is broadcast to everyone by the stasher,
    /// so it cannot be refused up front. The master client is the inventory authority, though: it
    /// skips the RPC locally, re-syncs the target pack to every other client and drops the item on
    /// the ground next to the stasher. That closes the gap for players who do not run the mod.
    /// </summary>
    internal static class StashRollback
    {
        /// <summary>
        /// Decides on the master whether an incoming stash must be rolled back. When it must, the
        /// stashed item is respawned at the stasher's feet and the caller has to skip the original RPC
        /// and re-sync the target pack.
        /// </summary>
        /// <returns>True when the stash is denied and was bounced.</returns>
        public static bool TryBounce(PhotonView stasherView, byte inventorySlotID, AccessVerdict verdict, string target)
        {
            if (!PhotonNetwork.IsMasterClient || verdict.IsGranted() || stasherView == null)
            {
                return false;
            }

            Photon.Realtime.Player stasher = stasherView.Owner;
            Player stasherPlayer = stasherView.GetComponent<Player>();
            ItemSlot slot = stasherPlayer != null ? stasherPlayer.GetItemSlot(inventorySlotID) : null;
            Plugin.Log.LogInfo($"Denied {stasher?.NickName} stashing into {target} (host check), returning the item.");

            // The stasher's slot is still filled on the master: their remove request arrives after this RPC.
            if (slot != null && !slot.IsEmpty())
            {
                RespawnNearStasher(slot, stasher);
            }
            return true;
        }

        /// <summary>Pushes the master's copy of a worn pack to every other client, undoing their local stash.</summary>
        public static void ResyncWornPack(Character wearer)
        {
            Player player = wearer != null ? wearer.player : null;
            if (player == null || player.photonView == null)
            {
                return;
            }
            byte[] payload = IBinarySerializable.ToManagedArray(new InventorySyncData(player.itemSlots, player.backpackSlot, player.tempFullSlot));
            player.photonView.RPC("SyncInventoryRPC", RpcTarget.Others, payload, false);
        }

        /// <summary>Pushes the master's copy of a pack on the ground to every other client.</summary>
        public static void ResyncGroundPack(Backpack pack)
        {
            if (pack != null && pack.photonView != null && pack.data != null)
            {
                pack.photonView.RPC("SetItemInstanceDataRPC", RpcTarget.Others, pack.data);
            }
        }

        private static void RespawnNearStasher(ItemSlot slot, Photon.Realtime.Player stasher)
        {
            string prefabName = slot.GetPrefabName();
            if (string.IsNullOrEmpty(prefabName))
            {
                return;
            }

            Character character = stasher != null ? PlayerHandler.GetPlayerCharacter(stasher) : null;
            Vector3 position = character != null
                ? character.Center + Vector3.up * 0.5f + character.transform.forward * 0.5f
                : Vector3.zero;

            GameObject spawned = PhotonNetwork.InstantiateItemRoom(prefabName, position, Quaternion.identity);
            PhotonView view = spawned != null ? spawned.GetComponent<PhotonView>() : null;
            if (view == null)
            {
                return;
            }
            view.RPC("SetItemInstanceDataRPC", RpcTarget.All, slot.data);
            view.RPC("SetKinematicRPC", RpcTarget.All, false, position, Quaternion.identity);
        }
    }

    /// <summary>Stash into a worn pack.</summary>
    [HarmonyPatch(typeof(CharacterBackpackHandler), nameof(CharacterBackpackHandler.RPCAddItemToCharacterBackpack))]
    internal static class WornPackStashRollbackPatch
    {
        private static bool Prefix(CharacterBackpackHandler __instance, PhotonView playerView, byte inventorySlotID)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return true;
            }

            Character wearer = __instance.GetComponent<Character>();
            AccessVerdict verdict = AccessPolicy.Evaluate(wearer, playerView != null ? playerView.Owner : null);
            if (!StashRollback.TryBounce(playerView, inventorySlotID, verdict, $"{wearer?.characterName}'s backpack"))
            {
                return true;
            }

            StashRollback.ResyncWornPack(wearer);
            return false;
        }
    }

    /// <summary>Stash into a pack lying on the ground.</summary>
    [HarmonyPatch(typeof(Backpack), nameof(Backpack.RPCAddItemToBackpack))]
    internal static class GroundPackStashRollbackPatch
    {
        private static bool Prefix(Backpack __instance, PhotonView playerView, byte slotID)
        {
            if (!PhotonNetwork.IsMasterClient || __instance.photonView == null)
            {
                return true;
            }

            AccessVerdict verdict = AccessPolicy.EvaluateDropped(__instance.photonView.ViewID, playerView != null ? playerView.Owner : null);
            if (!StashRollback.TryBounce(playerView, slotID, verdict, "a dropped pack"))
            {
                return true;
            }

            StashRollback.ResyncGroundPack(__instance);
            return false;
        }
    }
}
