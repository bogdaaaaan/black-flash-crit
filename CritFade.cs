using System;
using UnityEngine;

namespace BlackFlashCrit {
	public class CritFade : MonoBehaviour {
		private float life;
		private float maxLife;
		private float baseAlpha = 1f;
		private SpriteRenderer sr;

		// Callback to return object to pool instead of Destroy
		private Action<GameObject> _onFinished;

		// onFinished callback
		public void Init (float duration, float initialAlpha = 1f, Action<GameObject> onFinished = null) {
			maxLife = duration;
			life = duration;
			baseAlpha = Mathf.Clamp01(initialAlpha);
			_onFinished = onFinished;
			if (sr == null) sr = GetComponent<SpriteRenderer>();
		}

		private void Update () {
			life -= Time.deltaTime;
			if (life <= 0f) {
				// Release to pool if available; else Destroy
				if (_onFinished != null) {
					_onFinished(gameObject);
				}
				else {
					Destroy(gameObject);
				}
				return;
			}

			if (sr != null) {
				float t = life / maxLife;
				var c = sr.color;
				c.a = baseAlpha * t;
				sr.color = c;
			}
		}
	}
}