using System.Collections;
using UnityEngine;

namespace BlackFlashCrit {
	internal class CritOverlayBurst : MonoBehaviour {
		private static CritOverlayBurst runner;

		// Call this to play a burst of crit overlay sprites at the given anchor transform
		internal static void PlayBurst (Transform anchor, Sprite[] sprites) {
			if (!BlackFlashCrit.ModEnabled.Value) return;
			if (anchor == null) return;

			EnsureRunner();
			runner.StartCoroutine(runner.BurstRoutine(anchor, sprites));
		}

		private static void EnsureRunner () {
			if (runner != null) return;
			var go = new GameObject("BlackFlashCrit_BurstRunner");
			Object.DontDestroyOnLoad(go);
			runner = go.AddComponent<CritOverlayBurst>();
		}

		// Coroutine that spawns overlay sprites in a burst, with delays and chances
		private IEnumerator BurstRoutine (Transform anchor, Sprite[] sprites) {
			// Spawn chances per step (later steps lower chance)
			int n = sprites.Length;
			float[] chances = new float[n];
			for (int i = 0; i < n; i++) {
				chances[i] = 1f / (i + 1);
			}

			// Delay between steps (in frames)
			int minFrames = BlackFlashCrit.CritBurstMinFrames.Value;
			int maxFrames = BlackFlashCrit.CritBurstMaxFrames.Value;

			// Ensure values are valid: minFrames >= 0, maxFrames >= minFrames
			if (minFrames < 0 || maxFrames < minFrames) {
				minFrames = 5;
				maxFrames = 10;
			}

			for (int i = 0; i < n; i++) {
				if (anchor == null) {
					yield break;
				}

				if (Random.value <= chances[i]) {
					var sprite = GetSpriteForStep(sprites, i);
					if (sprite != null) {
						SpawnOne(anchor, sprite, i);
					}
				}

				if (i < n - 1) {
					int framesToWait = (maxFrames >= minFrames) ? Random.Range(minFrames, maxFrames + 1) : minFrames;
					for (int f = 0; f < framesToWait; f++) {
						yield return null;
					}
				}
			}
		}

		private Sprite GetSpriteForStep (Sprite[] sprites, int stepIndex) {
			if (sprites == null || sprites.Length == 0) return null;

			if (stepIndex < sprites.Length && sprites[stepIndex] != null) {
				return sprites[stepIndex];
			}

			// Fallback: return first non-null sprite
			for (int i = 0; i < sprites.Length; i++) {
				if (sprites[i] != null) return sprites[i];
			}
				
			return null;
		}

		private void SpawnOne (Transform anchor, Sprite sprite, int stepIndex) {
			float posRadius = 0.15f;
			float rotMax = 18f;
			float scaleMin = 0.90f;
			float scaleMax = 1.10f;
			float scale = Random.Range(scaleMin, scaleMax) * BlackFlashCrit.CritOverlayScale.Value;

			Vector2 jitter2D = posRadius > 0f ? Random.insideUnitCircle * posRadius : Vector2.zero;
			float rotZ = (rotMax > 0f) ? Random.Range(-rotMax, rotMax) : 0f;

			var go = new GameObject("CritOverlaySprite");
			go.transform.position = anchor.position + new Vector3(jitter2D.x, jitter2D.y, 0f);
			go.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
			go.transform.localScale = new Vector3(scale, scale, 1f);

			// Parent to anchor to follow movement
			var sr = go.AddComponent<SpriteRenderer>();
			sr.sprite = sprite;

			// Safer sorting: try Effects layer, else fallback with high order
			if (!TryAssignSortingLayer(sr, "Effects")) {
				sr.sortingOrder = 5000;
			}
			else {
				sr.sortingOrder = 999;
			}

			// Initial alpha per step: 0.7f, 0.49f, 0.343f, ...
			float initialAlpha = Mathf.Pow(0.7f, stepIndex);
			sr.color = new Color(1f, 1f, 1f, initialAlpha);

			float fade = 0.4f;
			go.AddComponent<CritFade>().Init(fade, initialAlpha);
		}

		// Try to assign the given sorting layer name to the sprite renderer
		private static bool TryAssignSortingLayer (SpriteRenderer sr, string layerName) {
			if (sr == null) return false;

			var layers = SortingLayer.layers;
			for (int i = 0; i < layers.Length; i++) {
				if (layers[i].name == layerName) {
					sr.sortingLayerName = layerName;
					return true;
				}
			}
			return false;
		}
	}
}