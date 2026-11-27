using UnityEngine;

namespace LastStop
{
    /// <summary>Presentation-only puffs behind the moving bus. Never changes the bus transform.</summary>
    public sealed class LastStopBusTrail : MonoBehaviour
    {
        private const int PuffCount = 3;
        private const float CycleSeconds = 0.9f;

        [SerializeField, HideInInspector] private Transform[] puffTransforms = new Transform[PuffCount];
        [SerializeField, HideInInspector] private SpriteRenderer[] puffRenderers = new SpriteRenderer[PuffCount];
        [SerializeField, HideInInspector] private Sprite puffSprite;
        [SerializeField, HideInInspector] private float startedAt;

        public void Configure(Sprite sprite)
        {
            if (sprite == null) return;
            puffSprite = sprite;
            startedAt = Time.time;
            for (var index = 0; index < PuffCount; index++)
            {
                var puff = new GameObject("Bus Trail Puff");
                puff.transform.SetParent(transform.parent, false);
                var renderer = puff.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = -2;
                puffTransforms[index] = puff.transform;
                puffRenderers[index] = renderer;
            }

            UpdatePuffs();
        }

        private void Update()
        {
            if (puffSprite != null) UpdatePuffs();
        }

        private void UpdatePuffs()
        {
            if (puffTransforms == null || puffRenderers == null ||
                puffTransforms.Length != PuffCount || puffRenderers.Length != PuffCount) return;
            for (var index = 0; index < PuffCount; index++)
            {
                var age = Mathf.Repeat((Time.time - startedAt) / CycleSeconds + index / (float)PuffCount, 1f);
                var puff = puffTransforms[index];
                if (puff == null || puffRenderers[index] == null) continue;
                puff.localPosition = transform.localPosition + new Vector3(-2.15f - age * 0.85f, -0.42f + age * 0.24f, 0f);
                var size = 0.28f + age * 0.27f;
                puff.localScale = new Vector3(size / puffSprite.bounds.size.x, size / puffSprite.bounds.size.y, 1f);
                var alpha = 0.55f * (1f - age) * (1f - age);
                puffRenderers[index].color = new Color(0.73f, 0.76f, 0.85f, alpha);
            }
        }

        private void OnDestroy()
        {
            if (puffTransforms == null) return;
            foreach (var puff in puffTransforms)
            {
                if (puff != null) Destroy(puff.gameObject);
            }
        }
    }
}
