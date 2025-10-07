using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BlackFlashCrit {
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	public class BlackFlashCrit : BaseUnityPlugin {
		public const string PluginGuid = "bodyando.silksong.blackflash";
		public const string PluginName = "Black Flash Mod";
		public const string PluginVersion = "1.1.0";

		internal static Sprite[] SpritesArray;

		// Config Settings
		// General
		internal static ConfigEntry<bool> ModEnabled;
		internal static ConfigEntry<bool> EveryCrestCanCrit;
		internal static ConfigEntry<bool> SkipCritChecks;
		// Visual
		internal static ConfigEntry<float> CritOverlayScale;
		internal static ConfigEntry<bool> DisplayCritOverlay;
		internal static ConfigEntry<int> CritBurstMinFrames;
		internal static ConfigEntry<int> CritBurstMaxFrames;
		// Crit
		internal static ConfigEntry<float> CustomCritChance;
		internal static ConfigEntry<float> CritDamageMultiplier;

		// Debounced loggers
		private Log.Debounced<float> _critChanceLogger;
		private Log.Debounced<float> _critMultiplierLogger;
		private Log.Debounced<float> _overlayScaleLogger;

		private Harmony _harmony;

		private void Awake () {
			Log.LogSource = Logger;
			_harmony = new Harmony(PluginGuid);

			InitConfig();
			TryLoadSprites();
			TryPatch();
			Log.Info($"{PluginName} loaded.");
		}

		private void InitConfig () {
			ModEnabled = Config.Bind("General", "EnableMod", true, "Enable or disable mod");
			ModEnabled.SettingChanged += (sender, args) => Log.Info($"{PluginName} is now {(ModEnabled.Value ? "ON" : "OFF")}");

			EveryCrestCanCrit = Config.Bind("General", "EveryCrestCanCrit", false, "If true, all crests can trigger critical hits. If false, only the Wanderer crit crest can.");
			EveryCrestCanCrit.SettingChanged += (sender, args) => Log.Info($"EveryCrestCanCrit is now {(EveryCrestCanCrit.Value ? "ON" : "OFF")}");

			SkipCritChecks = Config.Bind("General", "SkipCritChecks", false, "If true, skip checks for critical hits. If false, use vanilla game checks (Player should not be covered in maggots and have 9 or more silk).");
			SkipCritChecks.SettingChanged += (sender, args) => Log.Info($"SkipCritChecks is now {(SkipCritChecks.Value ? "ON" : "OFF")}");

			DisplayCritOverlay = Config.Bind("Visual", "DisplayCritOverlay", true, "If true, display custom sprites.");
			DisplayCritOverlay.SettingChanged += (sender, args) => Log.Info($"DisplayCritOverlay is now {(DisplayCritOverlay.Value ? "ON" : "OFF")}");

			CritOverlayScale = Config.Bind("Visual", "CritOverlayScale", 1f,
				new ConfigDescription("Scale multiplier for the crit overlay images.", new AcceptableValueRange<float>(0.1f, 2f)));
			_overlayScaleLogger = new Log.Debounced<float>(v => Log.Info($"CritOverlayScale is now {v}"), 0.15f);
			CritOverlayScale.SettingChanged += (sender, args) => _overlayScaleLogger.Set(CritOverlayScale.Value);

			CustomCritChance = Config.Bind("Crit", "CustomCritChance", 0.15f,
				new ConfigDescription("Custom critical chance (0.0 - 1.0).", new AcceptableValueRange<float>(0f, 1f)));
			_critChanceLogger = new Log.Debounced<float>(v => Log.Info($"CustomCritChance is now {v}"), 0.15f);
			CustomCritChance.SettingChanged += (sender, args) => _critChanceLogger.Set(CustomCritChance.Value);

			CritDamageMultiplier = Config.Bind("Crit", "CritDamageMultiplier", 3f,
				new ConfigDescription("Critical hit damage multiplier applied by the game.", new AcceptableValueRange<float>(0f, 10f)));
			_critMultiplierLogger = new Log.Debounced<float>(v => Log.Info($"CritDamageMultiplier is now {v}"), 0.15f);
			CritDamageMultiplier.SettingChanged += (sender, args) => _critMultiplierLogger.Set(CritDamageMultiplier.Value);

			CritBurstMinFrames = Config.Bind("Hidden", "CritBurstMinFrames", 5,
				new ConfigDescription("Minimum frames in between Black Flash burst frames (ADVANCED, hidden from UI).", null,
					new BrowsableAttribute(false))
			);

			CritBurstMaxFrames = Config.Bind("Hidden", "CritBurstMaxFrames", 10,
				new ConfigDescription("Maximum frames in between Black Flash burst frames (ADVANCED, hidden from UI).", null,
					new BrowsableAttribute(false))
			);
		}

		private void Update () {
			_critChanceLogger.Update();
			_critMultiplierLogger.Update();
			_overlayScaleLogger.Update();
		}

		private void TryLoadSprites () {
			try {
				string pluginDir = Path.GetDirectoryName(Info.Location);
				string imagesDir = Path.Combine(pluginDir, "images");

				if (!Directory.Exists(imagesDir)) {
					Log.Warn($"Images directory not found at {imagesDir}. No overlay images loaded.");
					SpritesArray = new Sprite[0];
					return;
				}

				var imageFiles = Directory.GetFiles(imagesDir, "*.png").OrderBy(f => f).ToArray();

				if (imageFiles.Length == 0) {
					Log.Warn($"No PNG files found in images directory {imagesDir}. No overlay images loaded.");
					SpritesArray = new Sprite[0];
					return;
				}

				var spriteList = new List<Sprite>();
				foreach (var path in imageFiles) {
					var tempSprite = LoadSpriteOrWarn(path, Path.GetFileName(path));
					if (tempSprite != null) {
						spriteList.Add(tempSprite);
					}
				}

				if (spriteList.Count == 0) {
					Log.Warn("No valid overlay images loaded. Creating placeholder.");
					spriteList.Add(MakePlaceholder(32, 32, new Color32(255, 0, 0, 180)));
				}
				SpritesArray = spriteList.ToArray();
			}
			catch (Exception e) {
				Log.Error($"Error loading overlay sprites: {e}");
				SpritesArray = new Sprite[] { MakePlaceholder(32, 32, new Color32(255, 0, 0, 180)) };
			}
		}

		private Sprite LoadSpriteOrWarn (string path, string label) {
			if (!File.Exists(path)) {
				Log.Warn($"{label}: file not found at {path}");
				return null;
			}

			try {
				byte[] data = File.ReadAllBytes(path);
				var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
				if (!tex.LoadImage(data)) {
					Log.Error($"{label}: failed to decode image at {path}");
					return null;
				}
				Log.Info($"{label}: loaded {Path.GetFileName(path)} ({tex.width}x{tex.height})");
				return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 64f);
			}
			catch (Exception e) {
				Log.Error($"{label}: error loading image at {path}: {e.Message}");
				return null;
			}
		}

		private Sprite MakePlaceholder (int w, int h, Color32 c) {
			var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
			var pixels = new Color32[w * h];
			for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
			tex.SetPixels32(pixels);
			tex.Apply();
			return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
		}

		private void TryPatch () {
			try {
				_harmony.PatchAll(Assembly.GetExecutingAssembly());
			}
			catch (Exception e) {
				Log.Error($"Harmony patch failed: {e}");
			}
		}

		internal static void SpawnCritOverlay (Transform target) {
			if (!ModEnabled.Value || target == null) return;
			if (SpritesArray == null || SpritesArray.Length == 0) return;
			CritOverlayBurst.PlayBurst(target, SpritesArray);
		}
	}
}