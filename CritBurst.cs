using System.Collections;
using UnityEngine;

namespace BlackFlashCrit
{
    internal class CritOverlayBurst : MonoBehaviour
    {
        private static CritOverlayBurst runner;

        internal static void PlayBurst(Transform anchor, Sprite[] sprites)
        {
            if (!BlackFlashCrit.ModEnabled.Value) return;
            if (anchor == null) return;

            EnsureRunner();
            runner.StartCoroutine(runner.BurstRoutine(anchor, sprites));
        }

        private static void EnsureRunner()
        {
            if (runner != null) return;
            var go = new GameObject("BlackFlashCrit_BurstRunner");
            Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<CritOverlayBurst>();
        }

        private IEnumerator BurstRoutine(Transform anchor, Sprite[] sprites)
        {
            // Spawn chances per step (later steps lower chance)
            float[] chances = new float[] { 1f, 0.5f, 0.25f };

            int minFrames = 2;
            int maxFrames = 6;
            if (minFrames < 0) minFrames = 0;

            for (int i = 0; i < 3; i++)
            {
                if (anchor == null) yield break;

                if (Random.value <= chances[i])
                {
                    var sprite = GetSpriteForStep(sprites, i);
                    if (sprite != null)
                        SpawnOne(anchor, sprite, i);
                }

                if (i < 2)
                {
                    int framesToWait = (maxFrames >= minFrames) ? Random.Range(minFrames, maxFrames + 1) : minFrames;
                    for (int f = 0; f < framesToWait; f++)
                        yield return null;
                }
            }
        }

        private Sprite GetSpriteForStep(Sprite[] sprites, int stepIndex)
        {
            if (sprites == null || sprites.Length == 0) return null;
            if (stepIndex < sprites.Length && sprites[stepIndex] != null)
                return sprites[stepIndex];
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] != null) return sprites[i];
            return null;
        }

        private void SpawnOne(Transform anchor, Sprite sprite, int stepIndex)
        {
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

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            // Safer sorting: try Effects layer, else fallback with high order
            if (!TryAssignSortingLayer(sr, "Effects"))
            {
                sr.sortingOrder = 5000;
            }
            else
            {
                sr.sortingOrder = 999;
            }

            // Initial alpha per step: 1.0, 0.5, 0.25
            float initialAlpha = Mathf.Pow(0.5f, stepIndex);
            sr.color = new Color(1f, 1f, 1f, initialAlpha);

            float fade = 0.4f;
            go.AddComponent<CritFade>().Init(fade, initialAlpha);
        }

        private static bool TryAssignSortingLayer(SpriteRenderer sr, string layerName)
        {
            if (sr == null) return false;
            var layers = SortingLayer.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == layerName)
                {
                    sr.sortingLayerName = layerName;
                    return true;
                }
            }
            return false;
        }
    }
}