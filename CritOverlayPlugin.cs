using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace BlackFlashCrit
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class BlackFlashCrit : BaseUnityPlugin
    {
        public const string PluginGuid = "bodyando.silksong.blackflash";
        public const string PluginName = "Black Flash Mod";
        public const string PluginVersion = "1.1.0";

        internal static ManualLogSource Log;
        internal static Harmony HarmonyInstance;

        // Sprites
        internal static Sprite CritSprite1;
        internal static Sprite CritSprite2;
        internal static Sprite CritSprite3;
        private const string OverlayImageFile = "black_flash";

        // Config
        internal static ConfigEntry<bool> ModEnabled;
        internal static ConfigEntry<bool> EveryCrestCanCrit;
        internal static ConfigEntry<float> CustomCritChance;
        internal static ConfigEntry<float> CritDamageMultiplier;
        internal static ConfigEntry<float> CritOverlayScale;
        internal static ConfigEntry<bool> DisplayCritOverlay;

        // Debounce state for CustomCritChance logging
        private const float CritChanceLogDebounceSeconds = 0.15f;
        private bool _critChanceDirty;
        private float _critChanceLastChangeRealtime;
        private float _pendingCritChanceValue;

        // Debounce state for CritDamageMultiplier logging
        private const float CritDamageMultiplierLogDebounceSeconds = 0.15f;
        private bool _critDamageMultiplierDirty;
        private float _critDamageMultiplierLastChangeRealtime;
        private float _pendingCritDamageMultiplierValue;

        // Debounce state for CritOverlayScale logging
        private const float CritOverlayScaleLogDebounceSeconds = 0.15f;
        private bool _critOverlayScaleDirty;
        private float _critOverlayScaleLastChangeRealtime;
        private float _pendingCritOverlayScaleValue;

        private void Awake()
        {
            Log = Logger;
            HarmonyInstance = new Harmony(PluginGuid);

            InitConfig();
            TryLoadSprites();
            TryPatch();
            Log.LogInfo($"{PluginName} loaded.");
        }

        private void InitConfig()
        {
            ModEnabled = Config.Bind("General", "EnableMod", true, "Enable or disable mod");
            ModEnabled.SettingChanged += (sender, args) => Log.LogInfo($"{PluginName} is now {(ModEnabled.Value ? "ON" : "OFF")}");

            EveryCrestCanCrit = Config.Bind("General", "EveryCrestCanCrit", false, "If true, all crests can trigger critical hits. If false, only the Wanderer crit crest can.");
            EveryCrestCanCrit.SettingChanged += (sender, args) => Log.LogInfo($"EveryCrestCanCrit is now {(EveryCrestCanCrit.Value ? "ON" : "OFF")}");

            DisplayCritOverlay = Config.Bind("Visual", "DisplayCritOverlay", true, "If true, display custom sprites.");
            DisplayCritOverlay.SettingChanged += (sender, args) => Log.LogInfo($"DisplayCritOverlay is now {(DisplayCritOverlay.Value ? "ON" : "OFF")}");

            CritOverlayScale = Config.Bind("Visual", "CritOverlayScale", 1f,
                new ConfigDescription("Scale multiplier for the crit overlay images.", new AcceptableValueRange<float>(0.1f, 2f)));
            CritOverlayScale.SettingChanged += (sender, args) =>
            {
                _pendingCritOverlayScaleValue = CritOverlayScale.Value;
                _critOverlayScaleDirty = true;
                _critOverlayScaleLastChangeRealtime = Time.realtimeSinceStartup;
            };



            CustomCritChance = Config.Bind("Crit", "CustomCritChance", 0.15f,
                new ConfigDescription("Custom critical chance (0.0 - 1.0).", new AcceptableValueRange<float>(0f, 1f)));
            CustomCritChance.SettingChanged += (sender, args) =>
            {
                _pendingCritChanceValue = CustomCritChance.Value;
                _critChanceDirty = true;
                _critChanceLastChangeRealtime = Time.realtimeSinceStartup;
            };

            CritDamageMultiplier = Config.Bind("Crit", "CritDamageMultiplier", 3f,
                new ConfigDescription("Critical hit damage multiplier applied by the game.", new AcceptableValueRange<float>(0f, 10f)));
            CritDamageMultiplier.SettingChanged += (sender, args) =>
            {
                _pendingCritDamageMultiplierValue = CritDamageMultiplier.Value;
                _critDamageMultiplierDirty = true;
                _critDamageMultiplierLastChangeRealtime = Time.realtimeSinceStartup;
            };
        }

        private void Update()
        {
            if (_critChanceDirty && Time.realtimeSinceStartup - _critChanceLastChangeRealtime >= CritChanceLogDebounceSeconds)
            {
                Log.LogInfo($"CustomCritChance is now {_pendingCritChanceValue}");
                _critChanceDirty = false;
            }
            if (_critDamageMultiplierDirty && Time.realtimeSinceStartup - _critDamageMultiplierLastChangeRealtime >= CritDamageMultiplierLogDebounceSeconds)
            {
                Log.LogInfo($"CritDamageMultiplier is now {_pendingCritDamageMultiplierValue}");
                _critDamageMultiplierDirty = false;
            }
            if (_critOverlayScaleDirty && Time.realtimeSinceStartup - _critOverlayScaleLastChangeRealtime >= CritOverlayScaleLogDebounceSeconds)
            {
                Log.LogInfo($"CritOverlayScale is now {_pendingCritOverlayScaleValue}");
                _critOverlayScaleDirty = false;
            }
        }

        private void TryLoadSprites()
        {
            try
            {
                string pluginDir = Path.GetDirectoryName(Info.Location);

                CritSprite1 = LoadSpriteOrWarn(Path.Combine(pluginDir, OverlayImageFile + "_1.png"), "Image1");
                CritSprite2 = LoadSpriteOrWarn(Path.Combine(pluginDir, OverlayImageFile + "_2.png"), "Image2");
                CritSprite3 = LoadSpriteOrWarn(Path.Combine(pluginDir, OverlayImageFile + "_3.png"), "Image3");

                if (CritSprite1 == null && CritSprite2 == null && CritSprite3 == null)
                {
                    Log.LogWarning("No overlay images loaded. Creating placeholder.");
                    CritSprite1 = MakePlaceholder(32, 32, new Color32(255, 0, 0, 180));
                }
            }
            catch (Exception e)
            {
                Log.LogError($"Error loading overlay sprites: {e}");
                if (CritSprite1 == null && CritSprite2 == null && CritSprite3 == null)
                    CritSprite1 = MakePlaceholder(32, 32, new Color32(255, 0, 0, 180));
            }
        }

        private Sprite LoadSpriteOrWarn(string path, string label)
        {
            if (!File.Exists(path))
            {
                Log.LogWarning($"{label}: file not found at {path}");
                return null;
            }

            try
            {
                byte[] data = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(data))
                {
                    Log.LogError($"{label}: failed to decode image at {path}");
                    return null;
                }
                Log.LogInfo($"{label}: loaded {Path.GetFileName(path)} ({tex.width}x{tex.height})");
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 64f);
            }
            catch (Exception e)
            {
                Log.LogError($"{label}: error loading image at {path}: {e.Message}");
                return null;
            }
        }

        private Sprite MakePlaceholder(int w, int h, Color32 c)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        }

        private void TryPatch()
        {
            try
            {
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                Log.LogError($"Harmony patch failed: {e}");
            }
        }

        internal static void SpawnCritOverlay(Transform target)
        {
            if (!ModEnabled.Value || target == null) return;

            var sprites = new Sprite[] { CritSprite1, CritSprite2, CritSprite3 };
            CritOverlayBurst.PlayBurst(target, sprites);
        }
    }
}