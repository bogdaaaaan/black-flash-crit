using GlobalSettings;
using HarmonyLib;
using UnityEngine;

namespace BlackFlashCrit {
	// Strong-typed patch: mutate HitInstance by ref before game consumes it
	[HarmonyPatch(typeof(HealthManager))]
	internal static class HealthManager_TakeDamage_Patch {

		// Prefix to apply canon Black Flash (base^2.5) on crits
		[HarmonyPatch("TakeDamage")]
		[HarmonyPrefix]
		private static void Prefix (ref HitInstance hitInstance) {
			if (!BlackFlashCrit.ModEnabled.Value) return;
			if (!CritSettings.CanonBlackFlashDamage.Value) return;
			if (!hitInstance.CriticalHit) return;

			int baseDamage = hitInstance.DamageDealt;
			if (baseDamage <= 0) return;

			// Canon: final = base^2.5
			float powered = Mathf.Pow(baseDamage, 2.5f);
			Log.Info($"Canon crit: base {baseDamage} -> powered {powered}");
			hitInstance.DamageDealt = Mathf.Max(0, Mathf.RoundToInt(powered));

			// Neutralize any further multiplier so the game won't rescale our powered damage
			hitInstance.Multiplier = 1f;
		}

		// Postfix for visuals and ramp
		[HarmonyPatch("TakeDamage")]
		[HarmonyPostfix]
		private static void Postfix (HealthManager __instance, ref HitInstance hitInstance) {
			if (!BlackFlashCrit.ModEnabled.Value) return;
			if (!hitInstance.CriticalHit) return;
			if (__instance == null) return;

			// Only show overlay and ramp when the victim is NOT the player
			var victim = __instance as Component;
			if (victim != null && !victim.gameObject.CompareTag("Player")) {
				CritRamp.OnEnemyHit();
				BlackFlashCrit.SpawnCritOverlay(victim.transform);
			}
		}
	}

	// Return effective crit chance (supports ramping)
	[HarmonyPatch(typeof(Gameplay), "get_WandererCritChance")]
	internal static class Gameplay_WandererCritChance_Patch {
		private static void Postfix (ref float __result) {
			if (!BlackFlashCrit.ModEnabled.Value) return;
			__result = Mathf.Clamp01(CritRamp.GetEffectiveCritChance());
		}
	}

	// Crit multiplier
	[HarmonyPatch(typeof(Gameplay), "get_WandererCritMultiplier")]
	internal static class Gameplay_WandererCritMultiplier_Patch {
		private static void Postfix (ref float __result) {
			if (!BlackFlashCrit.ModEnabled.Value) return;

			if (CritSettings.CanonBlackFlashDamage.Value) {
				__result = 1f;
				return;
			}

			__result = Mathf.Max(0f, CritSettings.DamageMultiplier.Value);
		}
	}

	// Modify whether the player can crit based on config
	[HarmonyPatch(typeof(HeroController), "get_IsWandererLucky")]
	internal static class HeroController_IsWandererLucky_Patch {
		private static void Postfix (object __instance, ref bool __result) {
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
			// Else: vanilla behavior
		}
	}
}