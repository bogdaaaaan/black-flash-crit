using BepInEx.Configuration;
using System.ComponentModel;

namespace BlackFlashCrit {
	internal static class  SectionsOrder {
		internal const string S_General = "0_General";
		internal const string S_Crit = "1_Crit";
		internal const string S_Ramp = "2_Crit_Ramp";
		internal const string S_Visual = "3_Visual";
		internal const string S_Audio = "4_Audio";
		internal const string S_Hidden = "9_Hidden";
	}

	// Owns crit rules and base crit values (used by patches and CritRamp).
	internal static class CritSettings {
		// Crit config
		internal static ConfigEntry<bool> EveryCrestCanCrit;
		internal static ConfigEntry<bool> SkipCritChecks;
		internal static ConfigEntry<float> BaseCritChance;
		internal static ConfigEntry<float> DamageMultiplier;
		
		internal static ConfigEntry<bool> CanonBlackFlashDamage;

		// Silk on crit config
		internal static ConfigEntry<bool> SilkOnCritEnabled;
		internal static ConfigEntry<int> SilkPerCrit;

		// Debounced logs
		private static Log.Debounced<float> _baseChanceLogger;
		private static Log.Debounced<float> _multiplierLogger;
		private static Log.Debounced<int> _silkPerCritLogger;

		internal static void Init (ConfigFile config) {
			// Crit config
			EveryCrestCanCrit = config.Bind(SectionsOrder.S_General, "Every Crest Can Crit", false,
				"If true, all crests can trigger critical hits. If false, only the Wanderer crit crest can.");
			EveryCrestCanCrit.SettingChanged += (s, a) =>
				Log.Info($"EveryCrestCanCrit is now {(EveryCrestCanCrit.Value ? "ON" : "OFF")}");

			SkipCritChecks = config.Bind(SectionsOrder.S_General, "Skip Crit Checks", false,
				"If true, skip checks for critical hits. If false, use vanilla game checks (Player should not be covered in maggots and have 9 or more silk).");
			SkipCritChecks.SettingChanged += (s, a) =>
				Log.Info($"SkipCritChecks is now {(SkipCritChecks.Value ? "ON" : "OFF")}");

			BaseCritChance = config.Bind(SectionsOrder.S_Crit, "Custom Crit Chance", 0.15f,
				new ConfigDescription("Base critical chance (0.0 - 1.0)", new AcceptableValueRange<float>(0f, 1f)));
			_baseChanceLogger = new Log.Debounced<float>(v => Log.Info($"CustomCritChance (base) is now {v}"), 0.15f);
			BaseCritChance.SettingChanged += (s, a) => {
				CritRamp.RebaseToBase();
				_baseChanceLogger.Set(BaseCritChance.Value);
			};

			DamageMultiplier = config.Bind(SectionsOrder.S_Crit, "Crit Damage Multiplier", 3f,
				new ConfigDescription("Critical hit damage multiplier applied by the game.", new AcceptableValueRange<float>(0f, 10f)));
			_multiplierLogger = new Log.Debounced<float>(v => Log.Info($"CritDamageMultiplier is now {v}"), 0.15f);
			DamageMultiplier.SettingChanged += (s, a) => _multiplierLogger.Set(DamageMultiplier.Value);

			CanonBlackFlashDamage = config.Bind(SectionsOrder.S_Crit, "Canon Black Flash Damage", false,
				"If true, crit damage is computed as baseDamage^2.5 (Crit Damage Multiplier is ignored).");
			CanonBlackFlashDamage.SettingChanged += (s, a) =>
				Log.Info($"CanonBlackFlashDamage is now {(CanonBlackFlashDamage.Value ? "ON" : "OFF")}.");

			// Silk on crit config
			SilkOnCritEnabled = config.Bind(SectionsOrder.S_Crit, "Enable Silk On Crit", false, "If true, regain silk on critical hits (non-player victims).");
			SilkOnCritEnabled.SettingChanged += (s, a) => Log.Info($"Silk On Crit is now {(SilkOnCritEnabled.Value ? "ON" : "OFF")}");

			SilkPerCrit = config.Bind(SectionsOrder.S_Crit, "Silk Per Crit", 1, new ConfigDescription("How much silk to regain for each critical hit.", new AcceptableValueRange<int>(0, 99)));
			_silkPerCritLogger = new Log.Debounced<int>(v => Log.Info($"Silk Per Crit is now {v}"), 0.15f);
			SilkPerCrit.SettingChanged += (s, a) => _silkPerCritLogger.Set(SilkPerCrit.Value);
		}

		internal static void Update () {
			_baseChanceLogger?.Update();
			_multiplierLogger?.Update();
			_silkPerCritLogger?.Update();
		}
	}

	// Owns crit ramping settings and state.
	internal static class RampingSettings {
		// Crit ramp config 
		internal static ConfigEntry<bool> CritRampEnabled;
		internal static ConfigEntry<bool> CritRampIncrease;
		internal static ConfigEntry<float> CritRampPercentPerHit;
		internal static ConfigEntry<float> CritRampResetSeconds;

		//Debounce logs
		private static Log.Debounced<float> _critRampPercentPerHitLogger;
		private static Log.Debounced<float> _critRampResetSecondsLogger;

		internal static void Init (ConfigFile config) {
			// Crit ramp config
			CritRampEnabled = config.Bind(SectionsOrder.S_Ramp, "Enabled", false,
							"Enable per-hit crit chance ramping (multiplies after each hit, resets after inactivity).");

			CritRampIncrease = config.Bind(SectionsOrder.S_Ramp, "Increase", true,
				"If true, chance increases per hit. If false, chance decreases per hit.");

			CritRampPercentPerHit = config.Bind(SectionsOrder.S_Ramp, "Percent Per Hit", 0.10f,
				new ConfigDescription("Percent change per hit (0.0 - 1.0). Example: 0.10 = ±10% per hit.", new AcceptableValueRange<float>(0f, 1f)));
			_critRampPercentPerHitLogger = new Log.Debounced<float>(v => Log.Info($"PercentPerHit is now {v}"), 0.15f);

			CritRampResetSeconds = config.Bind(SectionsOrder.S_Ramp, "Reset Seconds", 7f,
				new ConfigDescription("Seconds of inactivity after which crit chance resets to base.", new AcceptableValueRange<float>(0f, 60f)));
			_critRampResetSecondsLogger = new Log.Debounced<float>(v => Log.Info($"ResetSeconds is now {v}"), 0.15f);

			CritRampEnabled.SettingChanged += (s, a) => {
				CritRamp.RebaseToBase();
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

		internal static void Update () {
			_critRampPercentPerHitLogger.Update();
			_critRampResetSecondsLogger.Update();
		}
	}

	// Owns overlay display settings and state.
	internal static class OverlaySettings {
		internal static ConfigEntry<bool> DisplayOverlay;
		internal static ConfigEntry<float> OverlayScale;
		internal static ConfigEntry<int> BurstMinFrames;
		internal static ConfigEntry<int> BurstMaxFrames;

		private static Log.Debounced<float> _scaleLogger;

		internal static void Init (ConfigFile config) {
			DisplayOverlay = config.Bind(SectionsOrder.S_Visual, "Display Crit", true,
				"If true, display custom sprites.");
			DisplayOverlay.SettingChanged += (s, a) =>
				Log.Info($"DisplayCritOverlay is now {(DisplayOverlay.Value ? "ON" : "OFF")}");

			OverlayScale = config.Bind(SectionsOrder.S_Visual, "Crit Scale", 1f,
				new ConfigDescription("Scale multiplier for the crit overlay images.", new AcceptableValueRange<float>(0.1f, 2f)));
			_scaleLogger = new Log.Debounced<float>(v => Log.Info($"CritOverlayScale is now {v}"), 0.15f);
			OverlayScale.SettingChanged += (s, a) => _scaleLogger.Set(OverlayScale.Value);

			BurstMinFrames = config.Bind(SectionsOrder.S_Hidden, "CritBurstMinFrames", 5,
				new ConfigDescription("Minimum frames between Black Flash burst frames (ADVANCED, hidden from UI).", null, new BrowsableAttribute(false)));

			BurstMaxFrames = config.Bind(SectionsOrder.S_Hidden, "CritBurstMaxFrames", 10,
				new ConfigDescription("Maximum frames between Black Flash burst frames (ADVANCED, hidden from UI).", null, new BrowsableAttribute(false)));
		}

		internal static void Update () {
			_scaleLogger?.Update();
		}
	}

	// Owns audio settings and state.
	internal static class AudioSetting {
		// Config
		internal static ConfigEntry<bool> EnableCritSounds;
		internal static ConfigEntry<float> CritSoundVolume;
		internal static ConfigEntry<float> CritSoundPitchMin;
		internal static ConfigEntry<float> CritSoundPitchMax;

		internal static ConfigEntry<bool> MuteDefaultCritSfx;

		// Hidden config
		internal static ConfigEntry<int> MaxVoices;

		// Debounced logs
		private static Log.Debounced<float> _critSoundVolumeLogger;
		private static Log.Debounced<float> _critSoundPitchMinLogger;
		private static Log.Debounced<float> _critSoundPitchMaxLogger;

		internal static void Init (ConfigFile config) {
			EnableCritSounds = config.Bind(SectionsOrder.S_Audio, "Enable Crit Sounds", true, "Play a random custom sound when landing a critical hit.");
			EnableCritSounds.SettingChanged += (s, a) => Log.Info($"Enable Crit Sounds is now {(EnableCritSounds.Value ? "ON" : "OFF")}");

			MuteDefaultCritSfx = config.Bind(SectionsOrder.S_Audio, "Mute Default Critical Sound", true, "Mutes the game's default critical hit sound.");
			MuteDefaultCritSfx.SettingChanged += (s, a) => Log.Info($"Mute Default Crit is now {(MuteDefaultCritSfx.Value ? "ON" : "OFF")}");

			CritSoundVolume = config.Bind(SectionsOrder.S_Audio, "Crit Sound Volume", 0.9f, new ConfigDescription("Volume (0.0 - 1.0) applied to crit sounds.", new AcceptableValueRange<float>(0f, 1f)));
			_critSoundVolumeLogger = new Log.Debounced<float>(v => Log.Info($"Crit Sound Volume is now {v}"), 0.15f);
			CritSoundVolume.SettingChanged += (s, a) => _critSoundVolumeLogger.Set(CritSoundVolume.Value);

			CritSoundPitchMin = config.Bind(SectionsOrder.S_Audio, "Crit Sound Pitch Min", 1.0f, new ConfigDescription("Minimum random pitch per crit.", new AcceptableValueRange<float>(0.5f, 3f)));
			_critSoundPitchMinLogger = new Log.Debounced<float>(v => Log.Info($"Crit Sound Pitch Min is now {v}"), 0.15f);
			CritSoundPitchMin.SettingChanged += (s, a) => _critSoundPitchMinLogger.Set(CritSoundPitchMin.Value);

			CritSoundPitchMax = config.Bind(SectionsOrder.S_Audio, "Crit Sound Pitch Max", 1.0f, new ConfigDescription("Maximum random pitch per crit.", new AcceptableValueRange<float>(0.5f, 3f)));
			_critSoundPitchMaxLogger = new Log.Debounced<float>(v => Log.Info($"Crit Sound Pitch Max is now {v}"), 0.15f);
			CritSoundPitchMax.SettingChanged += (s, a) => _critSoundPitchMaxLogger.Set(CritSoundPitchMax.Value);

			MaxVoices = config.Bind(SectionsOrder.S_Hidden, "Max Simultaneous Voices (ADVANCED, hidden from UI).", 4, new ConfigDescription("Number of overlapping crit sounds allowed.", new AcceptableValueRange<int>(1, 16), new BrowsableAttribute(false)));
		}
		internal static void Update () {
			_critSoundVolumeLogger?.Update();
			_critSoundPitchMinLogger?.Update();
			_critSoundPitchMaxLogger?.Update();
		}
	}
}