using BepInEx.Configuration;
using GlobalSettings;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace BlackFlashCrit {
	[HarmonyPatch]
	internal static class HealthManager_TakeDamage_Patch {
		static MethodBase TargetMethod () {
			// Target the TakeDamage method in HealthManager that takes a HitInstance parameter
			var hmType = AccessTools.TypeByName("HealthManager");
			return AccessTools.Method(hmType, "TakeDamage", new Type[] { AccessTools.TypeByName("HitInstance") });
		}

		static void Postfix (object __instance, object hitInstance) {
			if (!BlackFlashCrit.ModEnabled.Value || __instance == null || hitInstance == null) return;

			// Try to get the "CriticalHit" field using reflection
			bool isCrit = false;
			try {
				var critField = hitInstance.GetType().GetField("CriticalHit", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (critField != null)
					isCrit = (bool)critField.GetValue(hitInstance);
			}
			catch {
				return;
			}
			if (!isCrit) return;

			// Only spawn overlay on non-player targets
			if (__instance is not Component c || c.gameObject.CompareTag("Player")) return;

			if (BlackFlashCrit.DisplayCritOverlay.Value)
				BlackFlashCrit.SpawnCritOverlay(c.transform);
		}
	}

	// Return custom crit chance 
	[HarmonyPatch(typeof(Gameplay), "get_WandererCritChance")]
	internal static class Gameplay_WandererCritChance_Patch {
		static void Postfix (ref float __result) {
			if (!BlackFlashCrit.ModEnabled.Value) return;
			__result = Mathf.Clamp01(BlackFlashCrit.CustomCritChance.Value);
		}
	}

	// Return custom crit damage multiplier 
	[HarmonyPatch(typeof(Gameplay), "get_WandererCritMultiplier")]
	internal static class Gameplay_WandererCritMultiplier_Patch {
		static void Postfix (ref float __result) {
			if (!BlackFlashCrit.ModEnabled.Value) return;
			__result = Mathf.Max(0f, BlackFlashCrit.CritDamageMultiplier.Value);
		}
	}

	// Modify whether the player can crit based on config
	[HarmonyPatch(typeof(HeroController), "get_IsWandererLucky")]
	internal static class HeroController_IsWandererLucky_Patch {
		static void Postfix (object __instance, ref bool __result) {
			if (!BlackFlashCrit.ModEnabled.Value) return;

			// All crests can crit, skip checks
			if (BlackFlashCrit.EveryCrestCanCrit.Value && BlackFlashCrit.SkipCritChecks.Value) {
				__result = true;
				return;
			}

			// All crests can crit, do checks
			if (BlackFlashCrit.EveryCrestCanCrit.Value && !BlackFlashCrit.SkipCritChecks.Value) {
				HeroController hc = __instance as HeroController ?? HeroController.instance;
				if (!hc) return;

				if (hc.cState != null && hc.cState.isMaggoted) {
					__result = false;
					return;
				}

				if (hc.playerData != null && hc.playerData.silk >= 9) {
					__result = true;
					return;
				}
			}

			// Only Wanderer Crest can crit and skip checks
			if (!BlackFlashCrit.EveryCrestCanCrit.Value && BlackFlashCrit.SkipCritChecks.Value) {
				if (!Gameplay.WandererCrest.IsEquipped) return;
				
				__result = true;
				return;
			}

			// Only Wanderer Crest can crit and do checks = vanilla behavior
		}
	}
}