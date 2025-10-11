using BepInEx.Configuration;
using UnityEngine;

namespace BlackFlashCrit {
	// Manages per-hit Build Up of crit chance and inactivity reset.
	internal static class CritBuildUp {

		// State
		private static float s_CurrentCritChance;
		private static float s_LastHitRealtime;

		internal static void Init () {
			// Rebase to base chance at startup
			RebaseToBase();
		}

		// Called from patch when an enemy (non-player) takes damage.
		internal static void OnEnemyHit () {
			if (!BlackFlashCrit.ModEnabled.Value) return;

			s_LastHitRealtime = Time.realtimeSinceStartup;

			// If disabled, keep current equal to base.
			if (!CritBuildUpSettings.CritBuildUpEnabled.Value) {
				RebaseToBase();
				return;
			}

			// Multiplicative step: current *= (1 ± step)
			float step = Mathf.Clamp01(CritBuildUpSettings.CritBuildUpPercentPerHit.Value);
			float factor = CritBuildUpSettings.CritBuildUpIncrease.Value ? (1f + step) : (1f - step);

			// If current somehow hit zero while base > 0, rebase to base before stepping.
			if (s_CurrentCritChance <= 0f && CritSettings.BaseCritChance.Value > 0f) {
				RebaseToBase();
			}

			s_CurrentCritChance = Mathf.Clamp01(s_CurrentCritChance * factor);
			// Log.Info($"Effective crit chance: {s_CurrentCritChance}");
		}

		// Call every frame from plugin.Update to handle inactivity reset.
		internal static void Update () {
			ResetIfStale();
		}

		// Used by the Gameplay.WandererCritChance patch.
		internal static float GetEffectiveCritChance () {
			float baseChance = Mathf.Clamp01(CritSettings.BaseCritChance.Value);
			if (!BlackFlashCrit.ModEnabled.Value) return baseChance;
			if (!CritBuildUpSettings.CritBuildUpEnabled.Value) return baseChance;

			return Mathf.Clamp01(s_CurrentCritChance);
		}

		// Rebase current to base chance.
		internal static void RebaseToBase () {
			s_CurrentCritChance = Mathf.Clamp01(CritSettings.BaseCritChance.Value);
		}

		private static void ResetIfStale () {
			if (!CritBuildUpSettings.CritBuildUpEnabled.Value) {
				// When disabled, keep current pinned to base.
				RebaseToBase();
				return;
			}

			float window = Mathf.Max(0f, CritBuildUpSettings.CritBuildUpResetSeconds.Value);
			if (window <= 0f) return; // disabled auto-reset

			float now = Time.realtimeSinceStartup;
			if (now - s_LastHitRealtime >= window) {
				float baseChance = Mathf.Clamp01(CritSettings.BaseCritChance.Value);
				if (Mathf.Abs(s_CurrentCritChance - baseChance) > 0.0001f) {
					Log.Info($"Crit chance reset to base {baseChance} after {window} seconds of inactivity.");
					s_CurrentCritChance = baseChance;
				}
			}
		}
	}
}