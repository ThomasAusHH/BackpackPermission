using BackpackPermission.Localization;
using BackpackPermission.Permissions;
using HarmonyLib;

namespace BackpackPermission.Patches
{
    /// <summary>
    /// A pack lying on the ground. When its last wearer protects dropped packs, locked players see
    /// a "Locked" prompt, cannot open the wheel and cannot stash into it.
    /// </summary>
    [HarmonyPatch(typeof(Backpack))]
    internal static class BackpackItemPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Backpack.GetInteractionText))]
        private static void GetInteractionText_Postfix(Backpack __instance, ref string __result)
        {
            if (AccessPolicy.IsDroppedPackLockedForLocalPlayer(__instance))
            {
                __result = Strings.LockedPrompt;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Backpack.Interact))]
        private static bool Interact_Prefix(Backpack __instance, Character interactor)
        {
            if (interactor == null || !interactor.IsLocal || !AccessPolicy.IsDroppedPackLockedForLocalPlayer(__instance))
            {
                return true;
            }
            Plugin.LogVerbose("Blocked opening a dropped pack (local check).");
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Backpack.Stash))]
        private static bool Stash_Prefix(Backpack __instance, Character interactor)
        {
            if (interactor == null || !interactor.IsLocal || !AccessPolicy.IsDroppedPackLockedForLocalPlayer(__instance))
            {
                return true;
            }
            Plugin.LogVerbose("Blocked stashing into a dropped pack (local check).");
            return false;
        }
    }
}
