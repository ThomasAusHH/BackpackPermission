using BackpackPermission.Permissions;
using BackpackPermission.UI;
using HarmonyLib;
using Photon.Pun;

namespace BackpackPermission.Patches
{
    /// <summary>
    /// The backpack wheel. Shows the access panel for the local player's own pack and closes the
    /// wheel if permission is revoked while it is open.
    /// </summary>
    [HarmonyPatch(typeof(BackpackWheel))]
    internal static class BackpackWheelPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(BackpackWheel.InitWheel))]
        private static void InitWheel_Postfix(BackpackWheel __instance, BackpackReference bp)
        {
            if (IsSomeoneElsesPack(bp))
            {
                PermissionPanel.HideIfOpen();
                if (!AccessPolicy.LocalPlayerMayAccess(bp))
                {
                    GUIManager.instance.CloseBackpackWheel();
                }
                return;
            }

            // The local player's own pack, worn or dropped, or a pack nobody owns: permissions are edited here.
            PermissionPanel.ShowFor(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(BackpackWheel.Update))]
        private static bool Update_Prefix(BackpackWheel __instance)
        {
            BackpackReference reference = __instance.backpack;
            if (reference.exists && IsSomeoneElsesPack(reference) && !AccessPolicy.LocalPlayerMayAccess(reference))
            {
                GUIManager.instance.CloseBackpackWheel();
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(BackpackWheel.Choose))]
        private static bool Choose_Prefix(BackpackWheel __instance)
        {
            return !__instance.chosenSlice.IsSome || AccessPolicy.LocalPlayerMayAccess(__instance.chosenSlice.Value.backpackReference);
        }

        private static bool IsSomeoneElsesPack(BackpackReference reference)
        {
            if (reference.view == null)
            {
                return false;
            }
            if (reference.type == BackpackReference.BackpackType.Equipped)
            {
                return !reference.IsOnMyBack();
            }
            return AccessPolicy.TryGetDroppedPackOwner(reference.view.ViewID, out Photon.Realtime.Player owner, out _)
                   && PhotonNetwork.LocalPlayer != null && owner.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber;
        }
    }

    /// <summary>Hides the access panel together with the wheel.</summary>
    [HarmonyPatch(typeof(GUIManager), nameof(GUIManager.CloseBackpackWheel))]
    internal static class GuiManagerPatches
    {
        private static void Postfix()
        {
            PermissionPanel.HideIfOpen();
        }
    }
}
