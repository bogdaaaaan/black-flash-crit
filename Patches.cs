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

            if (BlackFlashCrit.DisplayCritOverlay.Value)
                BlackFlashCrit.SpawnCritOverlay(go.transform);
        }
    }

    // Return custom crit chance 
    [HarmonyPatch(typeof(Gameplay), "get_WandererCritChance")]
    internal static class Gameplay_WandererCritChance_Patch
    {
        static void Postfix(ref float __result)
        {
            if (!BlackFlashCrit.ModEnabled.Value) return;
            __result = Mathf.Clamp01(BlackFlashCrit.CustomCritChance.Value);
        }
    }

    // Return custom crit damage multiplier 
    [HarmonyPatch(typeof(Gameplay), "get_WandererCritMultiplier")]
    internal static class Gameplay_WandererCritMultiplier_Patch
    {
        static void Postfix(ref float __result)
        {
            if (!BlackFlashCrit.ModEnabled.Value) return;
            __result = Mathf.Max(0f, BlackFlashCrit.CritDamageMultiplier.Value);
        }
    }

    // Apply crit values ASAP on hero awake (fixes first-hit ignoring on load)
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
        }
    }

    // Ensure the game ALWAYS uses values and that crits aren’t locked (covers binds/heals)
    [HarmonyPatch]
    internal static class HeroController_Update_ApplyCritValues_Patch
    {
        private static FieldInfo _wandererCritChanceFI;
        private static FieldInfo _wandererCritMultiplierFI;

        private static PropertyInfo _wandererStatePI;
        private static FieldInfo _lockedField;
        private static PropertyInfo _lockedProp;

        private static bool _searchedValues;
        private static bool _searchedState;

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("HeroController"), "Update");
        }

        static void Postfix(object __instance)
        {
            if (!BlackFlashCrit.ModEnabled.Value) return;

            // Keep chance/multiplier current (handles anything that overwrites them)
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

            // Unlock crits if the game tries to lock them (e.g., after silk bind)
            if (!_searchedState)
            {
                _searchedState = true;
                var hcType = __instance?.GetType();
                _wandererStatePI = hcType?.GetProperty("WandererState", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                // Determine the state type and its 'CriticalHitsLocked' member
                var stateType = _wandererStatePI?.PropertyType;
                if (stateType != null)
                {
                    _lockedField = stateType.GetField("CriticalHitsLocked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (_lockedField == null)
                    {
                        _lockedProp = stateType.GetProperty("CriticalHitsLocked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    }
                }
            }

            if (_wandererStatePI != null && (__instance != null))
            {
                TryUnlockCrits(__instance, _wandererStatePI, _lockedField, _lockedProp);
            }
        }

        private static void TryUnlockCrits(object hero, PropertyInfo statePI, FieldInfo lockedField, PropertyInfo lockedProp)
        {
            try
            {
                var state = statePI.GetValue(hero, null);
                if (state == null) return;

                if (lockedField != null) lockedField.SetValue(state, false);
                else if (lockedProp?.CanWrite == true) lockedProp.SetValue(state, false, null);

                // Reassign back (struct semantics)
                statePI.SetValue(hero, state, null);
            }
            catch { /* ignore */ }
        }
    }

    // Ignore silk requirement but keep gating by crest unless "every crest" is on.
    // IMPORTANT: also clear CriticalHitsLocked here in the same call to ensure the crit check passes this frame.
    [HarmonyPatch(typeof(HeroController), "get_IsWandererLucky")]
    internal static class HeroController_IsWandererLucky_Patch
    {
        private static PropertyInfo _wandererStatePI;
        private static FieldInfo _lockedField;
        private static PropertyInfo _lockedProp;
        private static bool _searchedStateMembers;

        static void Postfix(object __instance, ref bool __result)
        {
            if (!BlackFlashCrit.ModEnabled.Value) return;

            bool allow =
                BlackFlashCrit.EveryCrestCanCrit.Value ||
                SafeIsWandererCrestEquipped();

            if (!allow) return;

            __result = true;

            // Also force-unlock crits immediately (silk gate often locks them)
            if (!_searchedStateMembers)
            {
                _searchedStateMembers = true;
                var hcType = __instance?.GetType();
                _wandererStatePI = hcType?.GetProperty("WandererState", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var stateType = _wandererStatePI?.PropertyType;
                if (stateType != null)
                {
                    _lockedField = stateType.GetField("CriticalHitsLocked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (_lockedField == null)
                        _lockedProp = stateType.GetProperty("CriticalHitsLocked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }
            }

            if (_wandererStatePI != null)
            {
                try
                {
                    var state = _wandererStatePI.GetValue(__instance, null);
                    if (state != null)
                    {
                        if (_lockedField != null) _lockedField.SetValue(state, false);
                        else if (_lockedProp?.CanWrite == true) _lockedProp.SetValue(state, false, null);
                        _wandererStatePI.SetValue(__instance, state, null);
                    }
                }
                catch { /* ignore */ }
            }
        }

        private static bool SafeIsWandererCrestEquipped()
        {
            try
            {
                // Your build exposes Gameplay.WandererCrest; if it's Status-based, adjust here as needed.
                // This try/catch prevents NREs if the property isn't present on some versions.
                var crest = Gameplay.WandererCrest;
                // Prefer Status.IsEquipped if available, else IsEquipped
                var t = crest?.GetType();
                var statusPI = t?.GetProperty("Status");
                if (statusPI != null)
                {
                    var status = statusPI.GetValue(crest, null);
                    var isEq = status?.GetType().GetProperty("IsEquipped");
                    if (isEq != null) return (bool)isEq.GetValue(status, null);
                }
                var isEquippedPI = t?.GetProperty("IsEquipped");
                if (isEquippedPI != null) return (bool)isEquippedPI.GetValue(crest, null);
            }
            catch { }
            return false;
        }
    }
}