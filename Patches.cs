using BepInEx.Configuration;
using GlobalSettings;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace BlackFlashCrit
{
    [HarmonyPatch]
    internal static class HealthManager_TakeDamage_Patch
    {
        static MethodBase TargetMethod()
        {
            var hmType = AccessTools.TypeByName("HealthManager");
            return AccessTools.Method(hmType, "TakeDamage", new Type[] { AccessTools.TypeByName("HitInstance") });
        }

        static void Postfix(object __instance, object hitInstance)
        {
            if (!CritOverlayPlugin.ModEnabled.Value) return;
            if (__instance == null || hitInstance == null) return;

            var hiType = hitInstance.GetType();
            var critField = hiType.GetField("CriticalHit", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (critField == null) return;

            bool isCrit;
            try { isCrit = (bool)critField.GetValue(hitInstance); }
            catch { return; }
            if (!isCrit) return;

            if (!(__instance is Component c)) return;
            var go = c.gameObject;
            if (go.CompareTag("Player")) return;

            CritOverlayPlugin.SpawnCritOverlay(go.transform);
        }
    }

    // Return custom crit chance 
    [HarmonyPatch(typeof(Gameplay), "get_WandererCritChance")]
    internal static class Gameplay_WandererCritChance_Patch
    {
        static void Postfix(ref float __result)
        {
            if (!CritOverlayPlugin.ModEnabled.Value) return;

            __result = Mathf.Clamp01(CritOverlayPlugin.CustomCritChance.Value);
        }
    }

    // Return custom crit damage multiplier 
    [HarmonyPatch(typeof(Gameplay), "get_WandererCritMultiplier")]
    internal static class Gameplay_WandererCritMultiplier_Patch
    {
        static void Postfix(ref float __result)
        {
            if (!CritOverlayPlugin.ModEnabled.Value) return;
            // Ensure non-negative multiplier
            __result = Mathf.Max(0f, CritOverlayPlugin.CritDamageMultiplier.Value);
        }
    }

    // Ensure the game ALWAYS uses values even if it reads fields directly or caches them.
    [HarmonyPatch]
    internal static class HeroController_Update_ApplyCritValues_Patch
    {
        private static FieldInfo _wandererCritChanceFI;
        private static FieldInfo _wandererCritMultiplierFI;
        private static bool _searched;

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("HeroController"), "Update");
        }

        static void Postfix()
        {
            if (!CritOverlayPlugin.ModEnabled.Value) return;

            if (!_searched)
            {
                _searched = true;
                var gameplayType = AccessTools.TypeByName("Gameplay");
                if (gameplayType != null)
                {
                    _wandererCritChanceFI = gameplayType.GetField("WandererCritChance",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    _wandererCritMultiplierFI = gameplayType.GetField("WandererCritMultiplier",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }
            }

            // Apply chance
            if (_wandererCritChanceFI != null)
            {
                float chance = Mathf.Clamp01(CritOverlayPlugin.CustomCritChance.Value);
                try { _wandererCritChanceFI.SetValue(null, chance); } catch { /* ignore */ }
            }

            // Apply damage multiplier
            if (_wandererCritMultiplierFI != null)
            {
                float mult = Mathf.Max(0f, CritOverlayPlugin.CritDamageMultiplier.Value);
                try { _wandererCritMultiplierFI.SetValue(null, mult); } catch { /* ignore */ }
            }
        }
    }

    [HarmonyPatch(typeof(HeroController), "get_IsWandererLucky")]
    internal static class HeroController_IsWandererLucky_Patch
    {
        static void Postfix(ref bool __result)
        {
            if (!CritOverlayPlugin.ModEnabled.Value) return;
            if (!CritOverlayPlugin.EveryCrestCanCrit.Value) return;
            __result = true;
        }
    }
}