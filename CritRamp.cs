using BepInEx.Configuration;
using UnityEngine;

namespace BlackFlashCrit {
	// Manages per-hit ramping of crit chance and inactivity reset.
	internal static class CritRamp {
		// Config
		internal static ConfigEntry<bool> Enabled;                 // Enable per-hit ramping
		internal static ConfigEntry<bool> Increase;                // true = increase, false = decrease
		internal static ConfigEntry<float> PercentPerHit;          // 0..1 (e.g., 0.10 = 10% per hit)
		internal static ConfigEntry<float> ResetSeconds;           // seconds without a hit before reset

		// State
		private static float s_CurrentCritChance;                  // live crit chance (ramped)
		private static float s_LastHitRealtime;                    // last time we recorded a hit (RealtimeSinceStartup)

		internal static void Init (ConfigFile config) {
			Enabled = config.Bind("CritRamp", "Enabled", false,
				"Enable per-hit crit chance ramping (multiplies after each hit, resets after inactivity).");

			Increase = config.Bind("CritRamp", "Increase", true,
				"If true, chance increases per hit. If false, chance decreases per hit.");

			PercentPerHit = config.Bind("CritRamp", "PercentPerHit", 0.10f,
				new ConfigDescription("Percent change per hit (0.0 - 1.0). Example: 0.10 = ±10% per hit.",
					new AcceptableValueRange<float>(0f, 1f)));

			ResetSeconds = config.Bind("CritRamp", "ResetSeconds", 10f,
				new ConfigDescription("Seconds of inactivity after which crit chance resets to base.",
					new AcceptableValueRange<float>(0f, 120f)));

			// Rebase to base chance at startup and when toggling
			RebaseToBase();

			Enabled.SettingChanged += (s, a) => {
				RebaseToBase();
				Log.Info($"CritRamp Enabled is now {(Enabled.Value ? "ON" : "OFF")}.");
			};
			Increase.SettingChanged += (s, a) => {
				Log.Info($"CritRamp mode: {(Increase.Value ? "INCREASE" : "DECREASE")} per hit.");
			};
			PercentPerHit.SettingChanged += (s, a) => {
				Log.Info($"CritRamp PercentPerHit is now {PercentPerHit.Value:P0}");
			};
			ResetSeconds.SettingChanged += (s, a) => {
				Log.Info($"CritRamp ResetSeconds is now {ResetSeconds.Value} sec.");
			};
		}

		// Called from patch when an enemy (non-player) takes damage.
		internal static void OnEnemyHit () {
			if (!BlackFlashCrit.ModEnabled.Value) return;

			s_LastHitRealtime = Time.realtimeSinceStartup;

			// If disabled, keep current equal to base.
			if (!Enabled.Value) {
				RebaseToBase();
				return;
			}

			// Multiplicative step: current *= (1 ± step)
			float step = Mathf.Clamp01(PercentPerHit.Value);
			float factor = Increase.Value ? (1f + step) : (1f - step);

			// If current somehow hit zero while base > 0, rebase to base before stepping.
			if (s_CurrentCritChance <= 0f && BlackFlashCrit.CustomCritChance.Value > 0f) {
				RebaseToBase();
			}

			s_CurrentCritChance = Mathf.Clamp01(s_CurrentCritChance * factor);
			//Log.Info($"CritRamp OnEnemyHit: new effective crit chance is {s_CurrentCritChance}");
		}

		// Call every frame from plugin.Update to handle inactivity reset.
		internal static void Update () {
			ResetIfStale();
		}

		// Used by the Gameplay.WandererCritChance patch.
		internal static float GetEffectiveCritChance () {
			float baseChance = Mathf.Clamp01(BlackFlashCrit.CustomCritChance.Value);
			if (!BlackFlashCrit.ModEnabled.Value) return baseChance;
			if (!Enabled.Value) return baseChance;
			return Mathf.Clamp01(s_CurrentCritChance);
		}

		// Rebase current to base chance.
		internal static void RebaseToBase () {
			s_CurrentCritChance = Mathf.Clamp01(BlackFlashCrit.CustomCritChance.Value);
		}

		private static void ResetIfStale () {
			if (!Enabled.Value) {
				// When disabled, keep current pinned to base.
				RebaseToBase();
				return;
			}

			float window = Mathf.Max(0f, ResetSeconds.Value);
			if (window <= 0f) return; // disabled auto-reset

			float now = Time.realtimeSinceStartup;
			if (now - s_LastHitRealtime >= window) {
				float baseChance = Mathf.Clamp01(BlackFlashCrit.CustomCritChance.Value);
				if (Mathf.Abs(s_CurrentCritChance - baseChance) > 0.0001f) {
					s_CurrentCritChance = baseChance;
				}
			}
		}
	}
}