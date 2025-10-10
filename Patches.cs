using GlobalSettings;
using HarmonyLib;
using UnityEngine;

namespace BlackFlashCrit {
	[HarmonyPatch(typeof(HealthManager))]
	internal static class HealthManager_TakeDamage_Patch {
		// start suppression window and optionally apply canon damage
		[HarmonyPatch("TakeDamage")]
		[HarmonyPrefix]
		private static void Prefix (ref HitInstance hitInstance) {
			if (!BlackFlashCrit.ModEnabled.Value) return;
			if (!hitInstance.CriticalHit) return;

			// Canon damage (base^2.5) if enabled
			if (!CritSettings.CanonBlackFlashDamage.Value) return;

			int baseDamage = hitInstance.DamageDealt;
			if (baseDamage <= 0) return;

			float powered = Mathf.Pow(baseDamage, 2.5f);
			Log.Info($"Canon crit: base {baseDamage} -> powered {powered}");
			hitInstance.DamageDealt = Mathf.Max(0, Mathf.RoundToInt(powered));
			hitInstance.Multiplier = 1f;
		}

		// visuals, ramp, custom audio
		[HarmonyPatch("TakeDamage")]
		[HarmonyPostfix]
		private static void Postfix (HealthManager __instance, ref HitInstance hitInstance) {
			if (!BlackFlashCrit.ModEnabled.Value) return;
			if (!hitInstance.CriticalHit) return;
			if (__instance == null) return;

			var victim = __instance as Component;
			if (victim != null && !victim.gameObject.CompareTag("Player")) {
				CritRamp.OnEnemyHit();
				BlackFlashCrit.SpawnCritOverlay(victim.transform);
				SilkOnCrit.GrantOnCrit();
				CritAudio.PlayRandomCritSFX(victim.transform.position);
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