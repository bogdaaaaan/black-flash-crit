using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackFlashCrit {
	internal class CritOverlayBurst : MonoBehaviour {
		private static CritOverlayBurst runner;

		// Simple object pool for overlay GameObjects to reduce GC
		private static readonly Queue<GameObject> s_Pool = new Queue<GameObject>();
		private const int PoolPrewarm = 0;
		private static Transform s_PoolRoot;

		// Cache sorting layer lookup
		private static bool s_LayerCached;
		private static bool s_HasEffectsLayer;
		private const string EffectsLayerName = "Effects";

		// Play a burst of crit overlay sprites at the given anchor transform
		internal static void PlayBurst (Transform anchor, Sprite[] sprites) {
			if (!BlackFlashCrit.ModEnabled.Value) return;
			if (anchor == null || sprites == null || sprites.Length == 0) return;

			EnsureRunner();
			runner.StartCoroutine(runner.BurstRoutine(anchor, sprites));
		}

		private static void EnsureRunner () {
			if (runner != null) return;

			var go = new GameObject("BlackFlashCrit_BurstRunner");
			Object.DontDestroyOnLoad(go);
			runner = go.AddComponent<CritOverlayBurst>();

			// Create pool root (hidden)
			var poolRootGO = new GameObject("BlackFlashCrit_Pool");
			poolRootGO.hideFlags = HideFlags.HideAndDontSave;
			Object.DontDestroyOnLoad(poolRootGO);
			s_PoolRoot = poolRootGO.transform;

			// Optional prewarm
			for (int i = 0; i < PoolPrewarm; i++) {
				var pooled = CreateOverlayGO();
				ReleaseToPool(pooled);
			}
		}

		// Coroutine that spawns overlay sprites in a burst, with delays and chances
		private IEnumerator BurstRoutine (Transform anchor, Sprite[] sprites) {
			int n = sprites.Length;

			// Compute chance on the fly to avoid allocating a float[] per burst
			// Delay between steps (in frames)
			int minFrames = BlackFlashCrit.CritBurstMinFrames.Value;
			int maxFrames = BlackFlashCrit.CritBurstMaxFrames.Value;

			// Ensure values are valid: minFrames >= 0, maxFrames >= minFrames
			if (minFrames < 0 || maxFrames < minFrames) {
				minFrames = 5;
				maxFrames = 10;
			}

			for (int i = 0; i < n; i++) {
				if (anchor == null) yield break;

				float chance = 1f / (i + 1f);
				if (Random.value <= chance) {
					var sprite = GetSpriteForStep(sprites, i);
					if (sprite != null) {
						SpawnOne(anchor, sprite, i);
					}
				}

				// wait between steps (skip wait after last one)
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

			// Get from pool
			var go = GetFromPool();
			var tr = go.transform;
			tr.SetParent(null, false);
			tr.position = anchor.position + new Vector3(jitter2D.x, jitter2D.y, 0f);
			tr.rotation = Quaternion.Euler(0f, 0f, rotZ);
			tr.localScale = new Vector3(scale, scale, 1f);

			// Get components (created once per pooled object)
			var sr = go.GetComponent<SpriteRenderer>();
			var fade = go.GetComponent<CritFade>();

			sr.sprite = sprite;

			// Cache sorting layer lookup
			EnsureEffectsLayerCached();
			if (s_HasEffectsLayer) {
				sr.sortingLayerName = EffectsLayerName;
				sr.sortingOrder = 999;
			}
			else {
				sr.sortingOrder = 5000;
			}

			// Initial alpha per step: 0.7f, 0.49f, 0.343f, ...
			float initialAlpha = Mathf.Pow(0.7f, stepIndex);
			sr.color = new Color(1f, 1f, 1f, initialAlpha);

			float fadeDuration = 0.4f;
			// Return to pool instead of Destroy
			fade.Init(fadeDuration, initialAlpha, ReleaseToPool);
			go.SetActive(true);
		}

		// Pooled instance creation
		private static GameObject CreateOverlayGO () {
			var go = new GameObject("CritOverlaySprite");
			go.SetActive(false);
			go.transform.SetParent(s_PoolRoot, false);

			// components are added only once and reused
			var sr = go.AddComponent<SpriteRenderer>();
			var fade = go.AddComponent<CritFade>();

			return go;
		}

		// Get from pool or create
		private static GameObject GetFromPool () {
			if (s_Pool.Count > 0) {
				var go = s_Pool.Dequeue();
				return go;
			}
			return CreateOverlayGO();
		}

		// Release back to pool
		private static void ReleaseToPool (GameObject go) {
			if (go == null) return;
			go.SetActive(false);
			go.transform.SetParent(s_PoolRoot, false);
			s_Pool.Enqueue(go);
		}

		// Cache whether "Effects" sorting layer exists
		private static void EnsureEffectsLayerCached () {
			if (s_LayerCached) return;
			s_LayerCached = true;
			var layers = SortingLayer.layers;
			for (int i = 0; i < layers.Length; i++) {
				if (layers[i].name == EffectsLayerName) {
					s_HasEffectsLayer = true;
					return;
				}
			}
			s_HasEffectsLayer = false;
		}
	}
}