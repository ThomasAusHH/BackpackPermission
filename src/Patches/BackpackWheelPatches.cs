using BackpackPermission.Permissions;
using BackpackPermission.UI;
using HarmonyLib;

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
            bool isSomeoneElsesBack = bp.type == BackpackReference.BackpackType.Equipped && !bp.IsOnMyBack();
            if (isSomeoneElsesBack)
            {
                PermissionPanel.HideIfOpen();
                if (!AccessPolicy.LocalPlayerMayAccess(bp))
                {
                    GUIManager.instance.CloseBackpackWheel();
                }
                return;
            }

            // A dropped pack or the local player's own back: this is where permissions are edited.
            PermissionPanel.ShowFor(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(BackpackWheel.Update))]
        private static bool Update_Prefix(BackpackWheel __instance)
        {
            BackpackReference reference = __instance.backpack;
            if (reference.type == BackpackReference.BackpackType.Equipped && reference.exists && !AccessPolicy.LocalPlayerMayAccess(reference))
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
