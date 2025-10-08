using BepInEx.Configuration;
using System.ComponentModel;

namespace BlackFlashCrit {
	// Owns overlay visuals and burst timing.
	internal static class OverlaySettings {
		internal static ConfigEntry<bool> DisplayOverlay;
		internal static ConfigEntry<float> OverlayScale;
		internal static ConfigEntry<int> BurstMinFrames;
		internal static ConfigEntry<int> BurstMaxFrames;

		private static Log.Debounced<float> _scaleLogger;

		internal static void Init (ConfigFile config) {
			DisplayOverlay = config.Bind("Visual", "Display Crit", true,
				"If true, display custom sprites.");
			DisplayOverlay.SettingChanged += (s, a) =>
				Log.Info($"DisplayCritOverlay is now {(DisplayOverlay.Value ? "ON" : "OFF")}");

			OverlayScale = config.Bind("Visual", "Crit Scale", 1f,
				new ConfigDescription("Scale multiplier for the crit overlay images.", new AcceptableValueRange<float>(0.1f, 2f)));
			_scaleLogger = new Log.Debounced<float>(v => Log.Info($"CritOverlayScale is now {v}"), 0.15f);
			OverlayScale.SettingChanged += (s, a) => _scaleLogger.Set(OverlayScale.Value);

			BurstMinFrames = config.Bind("Hidden", "CritBurstMinFrames", 5,
				new ConfigDescription("Minimum frames between Black Flash burst frames (ADVANCED, hidden from UI).", null,
					new BrowsableAttribute(false)));

			BurstMaxFrames = config.Bind("Hidden", "CritBurstMaxFrames", 10,
				new ConfigDescription("Maximum frames between Black Flash burst frames (ADVANCED, hidden from UI).", null,
					new BrowsableAttribute(false)));
		}

		internal static void Update () {
			_scaleLogger?.Update();
		}
	}
}