using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BlackFlashCrit {
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	public class BlackFlashCrit : BaseUnityPlugin {
		public const string PluginGuid = "bodyando.silksong.blackflash";
		public const string PluginName = "Black Flash Mod";
		public const string PluginVersion = "1.4.0";

		internal static Sprite[] SpritesArray;

		private const string ImagesFolderName = "images";

		// Core on/off
		internal static ConfigEntry<bool> ModEnabled;

		private Harmony _harmony;

		private void Awake () {
			Log.LogSource = Logger;
			_harmony = new Harmony(PluginGuid);

			InitCoreConfig();

			// Initialize feature modules and their configs
			CritSettings.Init(Config);
			CritRamp.Init(Config);
			OverlaySettings.Init(Config);
			SilkOnCrit.Init(Config);

			// Initialize audio
			string pluginDir = Path.GetDirectoryName(Info.Location);
			CritAudio.Init(Config, pluginDir);

			TryLoadSprites();
			TryPatch();
			Log.Info($"{PluginName} loaded.");
		}

		private void InitCoreConfig () {
			ModEnabled = Config.Bind("General", "Enable Mod", true, "Enable or disable mod");
			ModEnabled.SettingChanged += (sender, args) => Log.Info($"{PluginName} is now {(ModEnabled.Value ? "ON" : "OFF")}");
		}

		private void Update () {
			// Modules that need per-frame maintenance
			CritRamp.Update();
			CritSettings.Update();
			OverlaySettings.Update();
			CritAudio.Update();
			SilkOnCrit.Update();
		}

		private void TryLoadSprites () {
			try {
				string pluginDir = Path.GetDirectoryName(Info.Location);
				string imagesDir = Path.Combine(pluginDir, ImagesFolderName);

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
				// Discard CPU copy to lower managed memory footprint
				if (!tex.LoadImage(data, markNonReadable: true)) {
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
			// Conditions to spawn overlay
			if (!ModEnabled.Value || target == null) return;
			if (!OverlaySettings.DisplayOverlay.Value) return;
			if (SpritesArray == null || SpritesArray.Length == 0) return;

			CritOverlayBurst.PlayBurst(target, SpritesArray);
		}
	}
}