using BepInEx.Configuration;
using UnityEngine;

namespace BlackFlashCrit {
	// Grants silk when a critical hit lands
	internal static class SilkOnCrit {
		internal static ConfigEntry<bool> Enabled;
		internal static ConfigEntry<int> SilkPerCrit;

		// Debounced logs
		private static Log.Debounced<int> _silkPerCritLogger;

		internal static void Init (ConfigFile config) {
			Enabled = config.Bind("Crit", "Enable Silk On Crit", false, "If true, regain silk on critical hits (non-player victims).");
			Enabled.SettingChanged += (s, a) => Log.Info($"Silk On Crit is now {(Enabled.Value ? "ON" : "OFF")}");

			SilkPerCrit = config.Bind("Crit", "Silk Per Crit", 1, new ConfigDescription("How much silk to regain for each critical hit.", new AcceptableValueRange<int>(0, 99)));
			_silkPerCritLogger = new Log.Debounced<int>(v => Log.Info($"Silk Per Crit is now {v}"), 0.15f);
			SilkPerCrit.SettingChanged += (s, a) => _silkPerCritLogger.Set(SilkPerCrit.Value);
		}

		internal static void Update () {
			_silkPerCritLogger?.Update();
		}

		internal static void GrantOnCrit () {
			if (!BlackFlashCrit.ModEnabled.Value) return;
			if (!Enabled.Value) return;

			int amount = Mathf.Max(0, SilkPerCrit.Value);
			if (amount <= 0) return;

			var hc = HeroController.instance;
			if (!hc || hc.playerData == null) return;

			// Add silk; false = don't play hero effect to avoid extra VFX/SFX spam
			hc.AddSilk(amount, false);
		}
	}
}