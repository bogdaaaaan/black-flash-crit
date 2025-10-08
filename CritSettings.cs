using BepInEx.Configuration;

namespace BlackFlashCrit {
	// Owns crit rules and base crit values (used by patches and CritRamp).
	internal static class CritSettings {
		internal static ConfigEntry<bool> EveryCrestCanCrit;
		internal static ConfigEntry<bool> SkipCritChecks;
		internal static ConfigEntry<float> BaseCritChance;
		internal static ConfigEntry<float> DamageMultiplier;

		internal static ConfigEntry<bool> CanonBlackFlashDamage;

		// Debounced logs (to keep plugin lean)
		private static Log.Debounced<float> _baseChanceLogger;
		private static Log.Debounced<float> _multiplierLogger;

		internal static void Init (ConfigFile config) {
			EveryCrestCanCrit = config.Bind("General", "Every Crest Can Crit", false,
				"If true, all crests can trigger critical hits. If false, only the Wanderer crit crest can.");
			EveryCrestCanCrit.SettingChanged += (s, a) =>
				Log.Info($"EveryCrestCanCrit is now {(EveryCrestCanCrit.Value ? "ON" : "OFF")}");

			SkipCritChecks = config.Bind("General", "Skip Crit Checks", false,
				"If true, skip checks for critical hits. If false, use vanilla game checks (Player should not be covered in maggots and have 9 or more silk).");
			SkipCritChecks.SettingChanged += (s, a) =>
				Log.Info($"SkipCritChecks is now {(SkipCritChecks.Value ? "ON" : "OFF")}");

			BaseCritChance = config.Bind("Crit", "Custom Crit Chance", 0.15f,
				new ConfigDescription("Base critical chance (0.0 - 1.0)", new AcceptableValueRange<float>(0f, 1f)));
			_baseChanceLogger = new Log.Debounced<float>(v => Log.Info($"CustomCritChance (base) is now {v}"), 0.15f);
			BaseCritChance.SettingChanged += (s, a) => {
				// Rebase ramp on change for clarity
				CritRamp.RebaseToBase();
				_baseChanceLogger.Set(BaseCritChance.Value);
			};

			DamageMultiplier = config.Bind("Crit", "Crit Damage Multiplier", 3f,
				new ConfigDescription("Critical hit damage multiplier applied by the game.", new AcceptableValueRange<float>(0f, 10f)));
			_multiplierLogger = new Log.Debounced<float>(v => Log.Info($"CritDamageMultiplier is now {v}"), 0.15f);
			DamageMultiplier.SettingChanged += (s, a) => _multiplierLogger.Set(DamageMultiplier.Value);

			CanonBlackFlashDamage = config.Bind("Crit", "Canon Black Flash Damage", false,
				"If true, crit damage is computed as baseDamage^2.5 (Crit Damage Multiplier is ignored).");
			CanonBlackFlashDamage.SettingChanged += (s, a) =>
				Log.Info($"CanonBlackFlashDamage is now {(CanonBlackFlashDamage.Value ? "ON" : "OFF")}.");
		}

		internal static void Update () {
			_baseChanceLogger?.Update();
			_multiplierLogger?.Update();
		}
	}
}