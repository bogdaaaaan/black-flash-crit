using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackFlashCrit {
	internal class CritOverlayBurst : MonoBehaviour {
		private static CritOverlayBurst runner;

		// Simple object pool for overlay GameObjects to reduce GC
		private static readonly Queue<OverlayPooledItem> s_Pool = new Queue<OverlayPooledItem>();
		private const int PoolPrewarm = 0;
		private const int PoolMax = 128;
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

			// Ensure we have at least sprites.Length instances prepped
			int need = Mathf.Max(0, sprites.Length - s_Pool.Count);
			for (int i = 0; i < need; i++) {
				var pooled = CreateOverlayGO();
				ReleaseToPool(pooled);
			}

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

			// Prewarm
			for (int i = 0; i < PoolPrewarm; i++) {
				var pooled = CreateOverlayGO();
				ReleaseToPool(pooled);
			}
		}

		// Coroutine that spawns overlay sprites in a burst, with delays and chances
		private IEnumerator BurstRoutine (Transform anchor, Sprite[] sprites) {
			int n = sprites.Length;

			// Delay between steps (in frames)
			int minFrames = OverlaySettings.BurstMinFrames.Value;
			int maxFrames = OverlaySettings.BurstMaxFrames.Value;

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
			float scale = Random.Range(scaleMin, scaleMax) * OverlaySettings.OverlayScale.Value;

			Vector2 jitter2D = posRadius > 0f ? Random.insideUnitCircle * posRadius : Vector2.zero;
			float rotZ = (rotMax > 0f) ? Random.Range(-rotMax, rotMax) : 0f;

			// Get from pool
			var item = GetFromPool();
			var tr = item.transform;
			tr.SetParent(null, false);
			tr.SetPositionAndRotation(
				anchor.position + new Vector3(jitter2D.x, jitter2D.y, 0f),
				Quaternion.Euler(0f, 0f, rotZ)
			);
			tr.localScale = new Vector3(scale, scale, 1f);

			var sr = item.SR;
			var fade = item.Fade;

			sr.sprite = sprite;
			EnsureEffectsLayerCached();
			if (s_HasEffectsLayer) {
				sr.sortingLayerName = EffectsLayerName;
				sr.sortingOrder = 999;
			}
			else {
				sr.sortingOrder = 5000;
			}

			float initialAlpha = Mathf.Pow(0.7f, stepIndex);
			sr.color = new Color(1f, 1f, 1f, initialAlpha);

			float fadeDuration = 0.4f;
			// ReleaseToPool now expects OverlayPooledItem
			fade.Init(fadeDuration, initialAlpha, go => ReleaseToPool(item));
			item.gameObject.SetActive(true);
		}

		// Pooled instance creation
		private static OverlayPooledItem CreateOverlayGO () {
			var go = new GameObject("CritOverlaySprite");
			go.SetActive(false);
			go.transform.SetParent(s_PoolRoot, false);

			// add once
			var sr = go.AddComponent<SpriteRenderer>();
			var fade = go.AddComponent<CritFade>();
			var holder = go.AddComponent<OverlayPooledItem>();
			holder.SR = sr;
			holder.Fade = fade;

			return holder;
		}

		// Get from pool or create
		private static OverlayPooledItem GetFromPool () {
			if (s_Pool.Count > 0) return s_Pool.Dequeue();
			return CreateOverlayGO();
		}

		// Release back to pool
		private static void ReleaseToPool (OverlayPooledItem item) {
			if (item == null) return;
			var go = item.gameObject;
			go.SetActive(false);
			go.transform.SetParent(s_PoolRoot, false);

			// Optional pool cap to avoid unbounded memory growth
			if (s_Pool.Count < PoolMax) {
				s_Pool.Enqueue(item);
			}
			else {
				Object.Destroy(go);
			}
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

	internal class OverlayPooledItem : MonoBehaviour {
		public SpriteRenderer SR;
		public CritFade Fade;
		private void Awake () {
			// These are added at creation time; cache once
			SR = GetComponent<SpriteRenderer>();
			Fade = GetComponent<CritFade>();
		}
	}
}