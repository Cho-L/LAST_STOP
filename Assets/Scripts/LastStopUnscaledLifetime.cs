using UnityEngine;

namespace LastStop
{
    /// <summary>
    /// Cleans up short-lived presentation objects even while a level-up menu pauses
    /// scaled game time. It has no gameplay state or collision behavior.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class LastStopUnscaledLifetime : MonoBehaviour
    {
        private float remaining;
        private SpriteRenderer hitTarget;
        private float hitSide;

        public void ConfigureHitSpark(float lifetime, SpriteRenderer target, float side, bool follow)
        {
            Configure(lifetime);
            hitSide = side;
            PositionHitSpark(target);
            if (follow) hitTarget = target;
        }

        private void LateUpdate()
        {
            if (hitTarget != null) PositionHitSpark(hitTarget);
        }

        private void PositionHitSpark(SpriteRenderer target)
        {
            var bounds = target.bounds;
            transform.position = bounds.center + Vector3.right * (bounds.extents.x * 0.7f * hitSide);
        }

        public void Configure(float lifetime)
        {
            remaining = lifetime;
        }

        private void Update()
        {
            remaining -= Time.unscaledDeltaTime;
            if (remaining <= 0f) Destroy(gameObject);
        }
    }
}
