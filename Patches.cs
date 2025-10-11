using GlobalSettings;
using HarmonyLib;
using UnityEngine;
using System;

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

		// visuals, build up, custom audio
		[HarmonyPatch("TakeDamage")]
		[HarmonyPostfix]
		private static void Postfix (HealthManager __instance, ref HitInstance hitInstance) {
			if (!BlackFlashCrit.ModEnabled.Value) return;
			if (!hitInstance.CriticalHit) return;
			if (__instance == null) return;

			var victim = __instance as Component;
			if (victim != null && !victim.gameObject.CompareTag("Player")) {
				CritBuildUp.OnEnemyHit();
				BlackFlashCrit.SpawnCritOverlay(victim.transform);
				SilkOnCrit.GrantOnCrit();
				CritAudio.PlayRandomCritSFX(victim.transform.position);
			}
		}
	}

	// Return effective crit chance (supports crit build up)
	[HarmonyPatch(typeof(Gameplay), "get_WandererCritChance")]
	internal static class Gameplay_WandererCritChance_Patch {
		private static void Postfix (ref float __result) {
			if (!BlackFlashCrit.ModEnabled.Value) return;
			__result = Mathf.Clamp01(CritBuildUp.GetEffectiveCritChance());
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

	[HarmonyPatch(typeof(ObjectPool))]
	internal static class CritEffectAudioMute {
		[HarmonyPatch(nameof(ObjectPool.Spawn), new Type[] { typeof(GameObject), typeof(Transform), typeof(Vector3), typeof(Quaternion) })]
		[HarmonyPostfix]
		private static void Postfix (GameObject prefab, Transform parent, Vector3 position, Quaternion rotation, ref GameObject __result) {
			if (__result == null) return;

			var critPrefab = Gameplay.WandererCritEffect;
			if (!critPrefab || prefab != critPrefab) return;

			foreach (var src in __result.GetComponentsInChildren<AudioSource>(true)) {
				if (AudioSetting.MuteDefaultCritSfx.Value) {
					// Ensure nothing plays (even if OnEnable already fired)
					if (src.isPlaying) src.Stop();
					src.playOnAwake = false;
					src.mute = true;
					src.volume = 0f;
				}
				else {
					// Restore to a sane audible state (handles pooled instances previously muted)
					bool wasMutedOrDisabled = src.mute || src.volume <= 0.0001f || !src.playOnAwake;

					src.mute = false;
					if (src.volume <= 0.0001f) src.volume = 1f;
					src.playOnAwake = true;

					// If it was previously muted/disabled, play now so this spawn has audio
					if (wasMutedOrDisabled && src.clip) {
						try { src.Play(); } catch { /* ignore */ }
					}
				}
			}
		}
	}
}