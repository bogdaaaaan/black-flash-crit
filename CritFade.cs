using UnityEngine;

namespace BlackFlashCrit
{
    public class CritFade : MonoBehaviour
    {
        private float life;
        private float maxLife;
        private float baseAlpha = 1f;
        private SpriteRenderer sr;

        public void Init(float duration, float initialAlpha = 1f)
        {
            maxLife = duration;
            life = duration;
            baseAlpha = Mathf.Clamp01(initialAlpha);
            sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            if (sr != null)
            {
                float t = life / maxLife;
                var c = sr.color;
                c.a = baseAlpha * t;
                sr.color = c;
            }
        }
    }
}