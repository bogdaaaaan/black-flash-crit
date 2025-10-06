using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace BlackFlashCrit
{
    internal static class VanillaGateCache
    {
        private static bool _searched;
        private static Type _maggotRegionType;
        private static PropertyInfo _maggotIsProp;
        private static FieldInfo _maggotIsField;

        private static Type _silkSpoolType;
        private static PropertyInfo _bindCostProp;
        private static FieldInfo _bindCostField;

        private static float _bindCostCached = 9f; // sensible default if reflection fails
        internal static bool GatesPass { get; private set; }

        internal static void UpdateGates()
        {
            try
            {
                if (!_searched)
                {
                    _searched = true;

                    _maggotRegionType = AccessTools.TypeByName("MaggotRegion");
                    if (_maggotRegionType != null)
                    {
                        _maggotIsProp = _maggotRegionType.GetProperty("IsMaggoted", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        _maggotIsField = _maggotRegionType.GetField("IsMaggoted", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    }

                    _silkSpoolType = AccessTools.TypeByName("SilkSpool");
                    if (_silkSpoolType != null)
                    {
                        _bindCostProp = _silkSpoolType.GetProperty("BindCost", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        _bindCostField = _silkSpoolType.GetField("BindCost", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        TryRefreshBindCost();
                    }
                }

                // Refresh bind cost occasionally (in case difficulty/charms affect it)
                TryRefreshBindCost();

                // Read fast: PlayerData is a direct object, no reflection here
                int silk = 0;
                try { silk = PlayerData.instance?.silk ?? 0; } catch { }

                bool maggoted = false;
                try
                {
                    if (_maggotIsProp != null)
                    {
                        var v = _maggotIsProp.GetValue(null, null);
                        if (v is bool b) maggoted = b;
                    }
                    else if (_maggotIsField != null)
                    {
                        var v2 = _maggotIsField.GetValue(null);
                        if (v2 is bool b2) maggoted = b2;
                    }
                }
                catch { }

                GatesPass = (silk >= Mathf.CeilToInt(_bindCostCached)) && !maggoted;
            }
            catch
            {
                // Fail-safe: default to false (vanilla gates do not pass)
                GatesPass = false;
            }
        }

        private static void TryRefreshBindCost()
        {
            try
            {
                if (_bindCostProp != null)
                {
                    var v = _bindCostProp.GetValue(null, null);
                    if (v is float f) _bindCostCached = f;
                    else if (v is int i) _bindCostCached = i;
                }
                else if (_bindCostField != null)
                {
                    var v2 = _bindCostField.GetValue(null);
                    if (v2 is float f2) _bindCostCached = f2;
                    else if (v2 is int i2) _bindCostCached = i2;
                }
            }
            catch { /* ignore, keep previous cached */ }
        }
    }
}