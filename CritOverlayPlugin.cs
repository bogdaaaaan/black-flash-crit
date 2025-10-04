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
    public class CritOverlayPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "bodyando.silksong.critoverlay";
        public const string PluginName = "Critical Strike Overlay";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;
        internal static Harmony HarmonyInstance;

        private const string SpriteFileName = "crit_overlay.png";
        internal static Sprite CritSprite;
        internal static string AssetPath;

        // Config
        internal static ConfigEntry<bool> ModEnabled;
        internal static ConfigEntry<bool> EveryCrestCanCrit;
        internal static ConfigEntry<float> CustomCritChance;
        internal static ConfigEntry<float> CritDamageMultiplier;

        private void Awake()
        {
            Log = Logger;
            HarmonyInstance = new Harmony(PluginGuid);
            AssetPath = Path.Combine(Application.dataPath, "Mods", "Playground", SpriteFileName);

            InitConfig();
            TryLoadOrCreateSprite();
            TryPatch();
            Log.LogInfo($"{PluginName} loaded.");
        }

        private void InitConfig()
        {
            ModEnabled = Config.Bind("General", "EnableMod", true, "Enable or disable mod");
            ModEnabled.SettingChanged += (sender, args) => Log.LogInfo($"Texture Parser Mod is now {(ModEnabled.Value ? "ON" : "OFF")}");

            EveryCrestCanCrit = Config.Bind("Crit", "EveryCrestCanCrit", false, "If true, all crests can trigger critical hits. If false, only the Wanderer crit crest can.");
            EveryCrestCanCrit.SettingChanged += (sender, args) => Log.LogInfo($"EveryCrestCanCrit is now {(EveryCrestCanCrit.Value ? "ON" : "OFF")}");

            CustomCritChance = Config.Bind("Crit", "CustomCritChance", 0.02f, new ConfigDescription("Custom critical chance (0.0 - 1.0).", new AcceptableValueRange<float>(0f, 1f)));
            CustomCritChance.SettingChanged += (sender, args) => Log.LogInfo($"CustomCritChance is now {CustomCritChance.Value}");
            
            CritDamageMultiplier = Config.Bind("Crit", "CritDamageMultiplier", 2.5f,
                    new ConfigDescription("Critical hit damage multiplier applied by the game.", new AcceptableValueRange<float>(0f, 10f)));
            CritDamageMultiplier.SettingChanged += (sender, args) => Log.LogInfo($"CritDamageMultiplier is now {CritDamageMultiplier.Value}");
        }

        private void TryLoadOrCreateSprite()
        {
            try
            {
                if (!File.Exists(AssetPath))
                {
                    Log.LogWarning($"{SpriteFileName} not found. Creating placeholder (red square).");
                    var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                    var fill = new Color32(255, 0, 0, 180);
                    var pixels = new Color32[32 * 32];
                    for (int i = 0; i < pixels.Length; i++) pixels[i] = fill;
                    tex.SetPixels32(pixels);
                    tex.Apply();
                    CritSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
                    return;
                }

                byte[] data = File.ReadAllBytes(AssetPath);
                var tex2 = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex2.LoadImage(data))
                {
                    Log.LogError($"Failed to load {SpriteFileName} image data.");
                    return;
                }
                CritSprite = Sprite.Create(tex2, new Rect(0, 0, tex2.width, tex2.height), new Vector2(0.5f, 0.5f), 64f);
                Log.LogInfo($"Loaded crit overlay sprite {tex2.width}x{tex2.height}.");
            }
            catch (Exception e)
            {
                Log.LogError($"Error loading overlay sprite: {e}");
            }
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
            if (!ModEnabled.Value) return;
            if (CritSprite == null || target == null) return;

            var go = new GameObject("CritOverlaySprite");
            go.transform.position = target.position + new Vector3(0f, 0.5f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CritSprite;
            sr.sortingLayerName = "Effects";  
            sr.sortingOrder = 999;
            sr.color = Color.white;

            go.AddComponent<CritFade>().Init(0.4f);
        }
    }
}