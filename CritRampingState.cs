using System;
using UnityEngine;

namespace BlackFlashCrit
{
    // Tracks the multiplicative ramp of crit chance after each successful hero hit
    internal static class CritRampingState
    {
        private static float _multiplier = 1f;
        private static float _lastHitRealtime = -1f;

        // Call each frame (or at least before applying chance) to reset on timeout
        internal static void UpdateDecay()
        {
            if (!BlackFlashCrit.CritRampingEnabled.Value) return;
            if (_lastHitRealtime < 0f) return;

            float now = Time.realtimeSinceStartup;
            if (now - _lastHitRealtime >= Mathf.Max(0.01f, BlackFlashCrit.CritRampingDecaySeconds.Value))
            {
                _multiplier = 1f;
                _lastHitRealtime = -1f;
            }
        }

        // Call after a successful hero hit on an enemy
        internal static void OnSuccessfulHit()
        {
            if (!BlackFlashCrit.CritRampingEnabled.Value) return;

            float inc = Mathf.Max(0f, BlackFlashCrit.CritRampingIncreasePercent.Value);
            float factor = 1f + inc;
            // Multiply current multiplier; chance is clamped later, so we let multiplier grow
            _multiplier *= factor;
            _lastHitRealtime = Time.realtimeSinceStartup;
        }

        // Compute effective chance given base (config) chance
        internal static float GetEffectiveChance(float baseChance)
        {
            float c = Mathf.Clamp01(baseChance);
            if (!BlackFlashCrit.CritRampingEnabled.Value) return c;

            UpdateDecay();
            float eff = c * _multiplier;
            // BlackFlashCrit.Log.LogInfo($"Current effective chance: {eff}");
            return Mathf.Clamp01(eff);
        }

        // Reset all state (optional use)
        internal static void Reset()
        {
            _multiplier = 1f;
            _lastHitRealtime = -1f;
        }
    }
}