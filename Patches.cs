using BepInEx.Configuration;
using GlobalSettings;
using HarmonyLib;
using System;
using System.Reflection;
using System.Linq.Expressions;
using UnityEngine;

namespace BlackFlashCrit {
	[HarmonyPatch]
	internal static class HealthManager_TakeDamage_Patch {
		// Cache compiled accessor for HitInstance.CriticalHit to avoid per-call reflection
		private static Func<object, bool> s_GetCriticalHit;
		private static bool s_TriedBuildAccessor;
		private static FieldInfo s_CritField;

		static MethodBase TargetMethod () {
			// Target the TakeDamage method in HealthManager that takes a HitInstance parameter
			var hmType = AccessTools.TypeByName("HealthManager");
			return AccessTools.Method(hmType, "TakeDamage", new Type[] { AccessTools.TypeByName("HitInstance") });
		}

		static void Postfix (object __instance, object hitInstance) {
			if (!BlackFlashCrit.ModEnabled.Value || __instance == null || hitInstance == null) return;

			// Build compiled accessor once instead of using reflection every hit
			EnsureCriticalHitAccessor(hitInstance);

			bool isCrit = false;
			if (s_GetCriticalHit != null) {
				// Fast delegate call
				isCrit = s_GetCriticalHit(hitInstance);
			}
			else {
				// Fallback path (should rarely/never happen after first success)
				try {
					var field = s_CritField;
					if (field == null) {
						var t = hitInstance.GetType();
						field = t.GetField("CriticalHit", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
						s_CritField = field;
					}
					if (field != null) {
						isCrit = (bool)field.GetValue(hitInstance);
					}
				}
				catch {
					return;
				}
			}

			if (!isCrit) return;

			// Only spawn overlay on non-player targets
			if (__instance is not Component c || c.gameObject.CompareTag("Player")) return;

			CritRamp.OnEnemyHit();
			BlackFlashCrit.SpawnCritOverlay(c.transform);
		}

		// Builds a compiled delegate: (object o) => ((HitInstance)o).CriticalHit
		private static void EnsureCriticalHitAccessor (object sampleHitInstance) {
			if (s_TriedBuildAccessor) return;
			s_TriedBuildAccessor = true;

			try {
				Type hitType = sampleHitInstance.GetType();
				var field = hitType.GetField("CriticalHit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (field == null || field.FieldType != typeof(bool)) return;

				s_CritField = field;

				// Build: (object obj) => ((HitType)obj).CriticalHit
				var objParam = Expression.Parameter(typeof(object), "obj");
				var casted = Expression.Convert(objParam, hitType);
				var fieldExpr = Expression.Field(casted, field);
				var body = Expression.Convert(fieldExpr, typeof(bool));
				var lambda = Expression.Lambda<Func<object, bool>>(body, objParam);

				s_GetCriticalHit = lambda.Compile();
			}
			catch {
				// ignore; fallback path will be used
			}
		}
	}

	// Return effective crit chance (supports ramping)
	[HarmonyPatch(typeof(Gameplay), "get_WandererCritChance")]
	internal static class Gameplay_WandererCritChance_Patch {
		static void Postfix (ref float __result) {
			if (!BlackFlashCrit.ModEnabled.Value) return;
			__result = Mathf.Clamp01(CritRamp.GetEffectiveCritChance());
		}
	}

	// Return custom crit damage multiplier 
	[HarmonyPatch(typeof(Gameplay), "get_WandererCritMultiplier")]
	internal static class Gameplay_WandererCritMultiplier_Patch {
		static void Postfix (ref float __result) {
			if (!BlackFlashCrit.ModEnabled.Value) return;
			__result = Mathf.Max(0f, CritSettings.DamageMultiplier.Value);
		}
	}

	// Modify whether the player can crit based on config
	[HarmonyPatch(typeof(HeroController), "get_IsWandererLucky")]
	internal static class HeroController_IsWandererLucky_Patch {
		static void Postfix (object __instance, ref bool __result) {
			if (!BlackFlashCrit.ModEnabled.Value) return;

			// All crests can crit, skip checks
			if (CritSettings.EveryCrestCanCrit.Value && CritSettings.SkipCritChecks.Value) {
				__result = true;
				return;
			}

			// All crests can crit, do checks
			if (CritSettings.EveryCrestCanCrit.Value && !CritSettings.SkipCritChecks.Value) {
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
			if (!CritSettings.EveryCrestCanCrit.Value && CritSettings.SkipCritChecks.Value) {
				if (!Gameplay.WandererCrest.IsEquipped) return;

				__result = true;
				return;
			}

			// Only Wanderer Crest can crit and do checks = vanilla behavior
		}
	}
}