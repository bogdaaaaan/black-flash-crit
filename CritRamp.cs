using BepInEx.Configuration;
using UnityEngine;

namespace BlackFlashCrit {
	// Manages per-hit ramping of crit chance and inactivity reset.
	internal static class CritRamp {
		// Config
		internal static ConfigEntry<bool> CritRampEnabled;
		internal static ConfigEntry<bool> CritRampIncrease;
		internal static ConfigEntry<float> CritRampPercentPerHit;
		internal static ConfigEntry<float> CritRampResetSeconds;

		//Debounce logs
		private static Log.Debounced<float> _critRampPercentPerHitLogger;
		private static Log.Debounced<float> _critRampResetSecondsLogger;

		// State
		private static float s_CurrentCritChance;
		private static float s_LastHitRealtime;

		internal static void Init (ConfigFile config) {
			CritRampEnabled = config.Bind("Crit Ramp", "Enabled", false,
				"Enable per-hit crit chance ramping (multiplies after each hit, resets after inactivity).");

			CritRampIncrease = config.Bind("Crit Ramp", "Increase", true,
				"If true, chance increases per hit. If false, chance decreases per hit.");

			CritRampPercentPerHit = config.Bind("Crit Ramp", "Percent Per Hit", 0.10f,
				new ConfigDescription("Percent change per hit (0.0 - 1.0). Example: 0.10 = ±10% per hit.", new AcceptableValueRange<float>(0f, 1f)));
			_critRampPercentPerHitLogger = new Log.Debounced<float>(v => Log.Info($"PercentPerHit is now {v}"), 0.15f);

			CritRampResetSeconds = config.Bind("Crit Ramp", "Reset Seconds", 7f,
				new ConfigDescription("Seconds of inactivity after which crit chance resets to base.", new AcceptableValueRange<float>(0f, 60f)));
			_critRampResetSecondsLogger = new Log.Debounced<float>(v => Log.Info($"ResetSeconds is now {v}"), 0.15f);

			// Rebase to base chance at startup and when toggling
			RebaseToBase();

			CritRampEnabled.SettingChanged += (s, a) => {
				RebaseToBase();
				Log.Info($"CritRamp Enabled is now {(CritRampEnabled.Value ? "ON" : "OFF")}.");
			};
			CritRampIncrease.SettingChanged += (s, a) => {
				Log.Info($"CritRamp mode: {(CritRampIncrease.Value ? "INCREASE" : "DECREASE")} per hit.");
			};
			CritRampPercentPerHit.SettingChanged += (s, a) => {
				_critRampPercentPerHitLogger.Set(CritRampPercentPerHit.Value);
			};
			CritRampResetSeconds.SettingChanged += (s, a) => {
				_critRampResetSecondsLogger.Set(CritRampResetSeconds.Value);
			};
		}

		// Called from patch when an enemy (non-player) takes damage.
		internal static void OnEnemyHit () {
			if (!BlackFlashCrit.ModEnabled.Value) return;

			s_LastHitRealtime = Time.realtimeSinceStartup;

			// If disabled, keep current equal to base.
			if (!CritRampEnabled.Value) {
				RebaseToBase();
				return;
			}

			// Multiplicative step: current *= (1 ± step)
			float step = Mathf.Clamp01(CritRampPercentPerHit.Value);
			float factor = CritRampIncrease.Value ? (1f + step) : (1f - step);

			// If current somehow hit zero while base > 0, rebase to base before stepping.
			if (s_CurrentCritChance <= 0f && CritSettings.BaseCritChance.Value > 0f) {
				RebaseToBase();
			}

			s_CurrentCritChance = Mathf.Clamp01(s_CurrentCritChance * factor);
		}

		// Call every frame from plugin.Update to handle inactivity reset.
		internal static void Update () {
			ResetIfStale();
			_critRampPercentPerHitLogger.Update();
			_critRampResetSecondsLogger.Update();
		}

		// Used by the Gameplay.WandererCritChance patch.
		internal static float GetEffectiveCritChance () {
			float baseChance = Mathf.Clamp01(CritSettings.BaseCritChance.Value);
			if (!BlackFlashCrit.ModEnabled.Value) return baseChance;
			if (!CritRampEnabled.Value) return baseChance;

			Log.Info($"Effective crit chance: {s_CurrentCritChance} (base {baseChance})");
			return Mathf.Clamp01(s_CurrentCritChance);
		}

		// Rebase current to base chance.
		internal static void RebaseToBase () {
			s_CurrentCritChance = Mathf.Clamp01(CritSettings.BaseCritChance.Value);
		}

		private static void ResetIfStale () {
			if (!CritRampEnabled.Value) {
				// When disabled, keep current pinned to base.
				RebaseToBase();
				return;
			}

			float window = Mathf.Max(0f, CritRampResetSeconds.Value);
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