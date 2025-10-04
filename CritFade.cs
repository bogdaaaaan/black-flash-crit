using UnityEngine;

namespace BlackFlashCrit
{
    public class CritFade : MonoBehaviour
    {
        private float life;
        private float maxLife;
        private SpriteRenderer sr;

        public void Init(float duration)
        {
            maxLife = duration;
            life = duration;
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
                c.a = t;
                sr.color = c;
            }
        }
    }
}
