using BackpackPermission.Localization;
using BackpackPermission.Permissions;
using HarmonyLib;

namespace BackpackPermission.Patches
{
    /// <summary>
    /// The pack on another player's back. Locked players see a "Locked" prompt, get no hold
    /// progress bar, cannot open the wheel and cannot light a rocketpack.
    /// </summary>
    [HarmonyPatch(typeof(BackpackOnBackVisuals))]
    internal static class BackpackOnBackVisualsPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(BackpackOnBackVisuals.IsConstantlyInteractable))]
        private static void IsConstantlyInteractable_Postfix(BackpackOnBackVisuals __instance, ref bool __result)
        {
            if (__result && IsLocked(__instance))
            {
                __result = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BackpackOnBackVisuals.GetInteractionText))]
        private static void GetInteractionText_Postfix(BackpackOnBackVisuals __instance, ref string __result)
        {
            if (IsLocked(__instance))
            {
                __result = Strings.LockedPrompt;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(BackpackOnBackVisuals.Interact_CastFinished))]
        private static bool Interact_CastFinished_Prefix(BackpackOnBackVisuals __instance)
        {
            if (!IsLocked(__instance))
            {
                return true;
            }
            Plugin.LogVerbose($"Blocked opening {__instance.character.characterName}'s backpack (local check).");
            return false;
        }

        private static bool IsLocked(BackpackOnBackVisuals visuals)
        {
            return visuals != null && AccessPolicy.IsLockedForLocalPlayer(visuals.character);
        }
    }
}
