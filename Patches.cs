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
            if (!BlackFlashCrit.ModEnabled.Value) return;
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

            BlackFlashCrit.SpawnCritOverlay(go.transform);
        }
    }

    [HarmonyPatch(typeof(Gameplay), "get_WandererCritChance")]
    internal static class Gameplay_WandererCritChance_Patch
    {
        static void Postfix(ref float __result)
        {
            if (!BlackFlashCrit.ModEnabled.Value) return;
            __result = Mathf.Clamp01(BlackFlashCrit.CustomCritChance.Value);
        }
    }

    [HarmonyPatch(typeof(Gameplay), "get_WandererCritMultiplier")]
    internal static class Gameplay_WandererCritMultiplier_Patch
    {
        static void Postfix(ref float __result)
        {
            if (!BlackFlashCrit.ModEnabled.Value) return;
            __result = Mathf.Max(0f, BlackFlashCrit.CritDamageMultiplier.Value);
        }
    }

    [HarmonyPatch]
    internal static class HeroController_Awake_ApplyCritValues_Patch
    {
        private static FieldInfo _chanceFI;
        private static FieldInfo _multFI;
        private static bool _searched;

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("HeroController"), "Awake");
        }

        static void Postfix()
        {
            if (!BlackFlashCrit.ModEnabled.Value) return;

            if (!_searched)
            {
                _searched = true;
                var gameplayType = AccessTools.TypeByName("Gameplay");
                if (gameplayType != null)
                {
                    _chanceFI = gameplayType.GetField("WandererCritChance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    _multFI = gameplayType.GetField("WandererCritMultiplier", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }
            }

            try { _chanceFI?.SetValue(null, Mathf.Clamp01(BlackFlashCrit.CustomCritChance.Value)); } catch { }
            try { _multFI?.SetValue(null, Mathf.Max(0f, BlackFlashCrit.CritDamageMultiplier.Value)); } catch { }

            // Prime the vanilla gates cache immediately on hero load
            VanillaGateCache.UpdateGates();
        }
    }

    [HarmonyPatch]
    internal static class HeroController_Update_ApplyCritValues_Patch
    {
        private static FieldInfo _wandererCritChanceFI;
        private static FieldInfo _wandererCritMultiplierFI;
        private static bool _searchedValues;

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("HeroController"), "Update");
        }

        static void Postfix()
        {
            if (!BlackFlashCrit.ModEnabled.Value) return;

            // Update vanilla gate pass once per frame (cheap)
            VanillaGateCache.UpdateGates();

            if (!_searchedValues)
            {
                _searchedValues = true;
                var gameplayType = AccessTools.TypeByName("Gameplay");
                if (gameplayType != null)
                {
                    _wandererCritChanceFI = gameplayType.GetField("WandererCritChance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    _wandererCritMultiplierFI = gameplayType.GetField("WandererCritMultiplier", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }
            }
            try { _wandererCritChanceFI?.SetValue(null, Mathf.Clamp01(BlackFlashCrit.CustomCritChance.Value)); } catch { }
            try { _wandererCritMultiplierFI?.SetValue(null, Mathf.Max(0f, BlackFlashCrit.CritDamageMultiplier.Value)); } catch { }
        }
    }

    // Performance-safe lucky gate:
    // - If CritVanillaSilkGate && EveryCrestCanCrit: use cached VanillaGateCache.GatesPass (no reflection here).
    // - If CritVanillaSilkGate && !EveryCrestCanCrit: leave vanilla behavior intact (no overrides).
    // - Else: keep your previous behavior (EveryCrestCanCrit -> always lucky; else when Wanderer crest equipped).
    [HarmonyPatch(typeof(HeroController), "get_IsWandererLucky")]
    internal static class HeroController_IsWandererLucky_Patch
    {
        static void Postfix(ref bool __result)
        {
            if (!BlackFlashCrit.ModEnabled.Value) return;

            if (BlackFlashCrit.CritVanillaSilkGate.Value)
            {
                if (BlackFlashCrit.EveryCrestCanCrit.Value)
                {
                    __result = VanillaGateCache.GatesPass;
                    return;
                }

                // Vanilla gate requested but not "every crest": do not override (keep true vanilla)
                return;
            }

            // Non-vanilla gate paths
            if (BlackFlashCrit.EveryCrestCanCrit.Value)
            {
                __result = true;
                return;
            }

            try
            {
                // Crest-only mode: allow when Wanderer crest is equipped
                var crest = Gameplay.WandererCrest;
                if (crest != null)
                {
                    var t = crest.GetType();
                    var statusPI = t.GetProperty("Status", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (statusPI != null)
                    {
                        var status = statusPI.GetValue(crest, null);
                        var isEq = status?.GetType().GetProperty("IsEquipped", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (isEq != null && (bool)isEq.GetValue(status, null))
                        {
                            __result = true;
                            return;
                        }
                    }
                    var isEquippedPI = t.GetProperty("IsEquipped", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (isEquippedPI != null && (bool)isEquippedPI.GetValue(crest, null))
                    {
                        __result = true;
                        return;
                    }
                }
            }
            catch { }
        }
    }
}