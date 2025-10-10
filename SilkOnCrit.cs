using UnityEngine;

namespace BlackFlashCrit {
	// Grants silk when a critical hit lands
	internal static class SilkOnCrit {

		internal static void GrantOnCrit () {
			if (!BlackFlashCrit.ModEnabled.Value) return;
			if (!CritSettings.SilkOnCritEnabled.Value) return;

			int amount = Mathf.Max(0, CritSettings.SilkPerCrit.Value);
			if (amount <= 0) return;

			var hc = HeroController.instance;
			if (!hc || hc.playerData == null) return;

			// Add silk; false = don't play hero effect to avoid extra VFX/SFX spam
			hc.AddSilk(amount, false);
		}
	}
}