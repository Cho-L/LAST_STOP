using UnityEngine;

namespace LastStop
{
    /// <summary>
    /// Displays idle motion and combat poses without moving the gameplay position.
    /// </summary>
    public sealed class LastStopSpriteMotion : MonoBehaviour
    {
        private Vector3 baseScale;
        private MotionStyle style;
        private float phase;
        private float entryStartedAt = float.NegativeInfinity;
        private float recoilDirection;
        private Vector3 visualRestPosition;
        private float hitStartedAt = float.NegativeInfinity;
        private float attackStartedAt = float.NegativeInfinity;
        private float attackFacing = 1f;
        private int attackComboStep;
        private Quaternion baseRotation;
        private SpriteRenderer spriteRenderer;
        private Color baseColor;
        private bool facingLeft;
        private Sprite idleSprite;
        private Sprite swingContact;
        private Sprite swingFollowThrough;

        public bool HasSwingSprites => swingContact != null && swingFollowThrough != null;

        public void SetSwingSprites(Sprite contact, Sprite followThrough)
        {
            idleSprite = spriteRenderer.sprite;
            swingContact = contact;
            swingFollowThrough = followThrough;
        }

        public enum MotionStyle
        {
            GentleBreathing,
            EnemyBounce,
            BusSway
        }

        public void Configure(MotionStyle motionStyle)
        {
            baseScale = transform.localScale;
            baseRotation = transform.localRotation;
            style = motionStyle;
            phase = Random.value * Mathf.PI * 2f;
            SetPresentationRenderer(GetComponent<SpriteRenderer>());
        }

        public void TriggerHit()
        {
            if (style == MotionStyle.EnemyBounce) hitStartedAt = Time.time;
        }

        public void TriggerEntry()
        {
            entryStartedAt = Time.time;
        }

        public void TriggerInteriorHit(float direction)
        {
            TriggerHit();
            recoilDirection = direction;
        }

        public void TriggerAttack(int comboStep, float facing)
        {
            if (style != MotionStyle.GentleBreathing) return;
            attackComboStep = comboStep;
            attackFacing = facing;
            attackStartedAt = Time.time;
        }

        public void SetPresentationRenderer(SpriteRenderer renderer)
        {
            if (spriteRenderer == null && renderer != null) baseColor = renderer.color;
            spriteRenderer = renderer;
            if (renderer == null) return;
            renderer.color = baseColor;
            facingLeft = renderer.flipX;
            visualRestPosition = renderer.transform.localPosition;
            entryStartedAt = float.NegativeInfinity;
            recoilDirection = 0f;
            hitStartedAt = float.NegativeInfinity;
        }

        public void SetFacing(float facing)
        {
            facingLeft = facing < 0f;
            if (spriteRenderer != null) spriteRenderer.flipX = facingLeft;
        }

        private void LateUpdate()
        {
            if (Quaternion.Dot(baseRotation, baseRotation) < 0.5f) baseRotation = transform.localRotation;
            var wave = Mathf.Sin(Time.time * Frequency + phase);
            var animatedScale = style switch
            {
                MotionStyle.EnemyBounce => new Vector3(
                    baseScale.x * (1f - wave * 0.025f),
                    baseScale.y * (1f + wave * 0.055f),
                    baseScale.z),
                MotionStyle.BusSway => new Vector3(
                    baseScale.x * (1f + wave * 0.004f),
                    baseScale.y * (1f - wave * 0.016f),
                    baseScale.z),
                _ => baseScale * (1f + wave * 0.018f)
            };

            var hitAge = Time.time - hitStartedAt;
            var attackAge = Time.time - attackStartedAt;
            if (style == MotionStyle.EnemyBounce && hitAge >= 0f && hitAge < 0.15f)
            {
                var strength = (1f - hitAge / 0.15f) * 0.8f;
                animatedScale.x *= 1f - 0.045f * strength;
                animatedScale.y *= 1f + 0.045f * strength;
                if (spriteRenderer != null)
                    spriteRenderer.color = Color.Lerp(baseColor, new Color(1f, 0.84f, 0.66f, baseColor.a), strength);
            }
            else if (spriteRenderer != null && spriteRenderer.color != baseColor)
            {
                spriteRenderer.color = baseColor;
            }

            if (style == MotionStyle.GentleBreathing && spriteRenderer != null)
                spriteRenderer.flipX = attackAge >= 0f && attackAge < 0.16f ? attackFacing < 0f : facingLeft;

            if (HasSwingSprites)
            {
                spriteRenderer.sprite = attackAge >= 0f && attackAge < 0.16f
                    ? (attackAge < 0.055f ? swingContact : swingFollowThrough)
                    : idleSprite;
                transform.localRotation = baseRotation;
            }
            else if (style == MotionStyle.GentleBreathing && attackAge >= 0f && attackAge < 0.16f)
            {
                var thrust = 1f - attackAge / 0.16f;
                var emphasis = attackComboStep == 3 ? 1f : 0.7f;
                animatedScale.x *= 1f + 0.035f * thrust * emphasis;
                animatedScale.y *= 1f - 0.025f * thrust * emphasis;
                transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, -attackFacing * 5f * thrust * emphasis);
                if (spriteRenderer != null)
                    spriteRenderer.color = Color.Lerp(baseColor, new Color(1f, 0.9f, 0.62f, baseColor.a), thrust * 0.20f);
            }
            else
            {
                transform.localRotation = baseRotation;
            }

            transform.localScale = animatedScale;
            if (style == MotionStyle.EnemyBounce && spriteRenderer != null && spriteRenderer.transform != transform)
                UpdateInteriorEnemyOffset(hitAge, animatedScale);
        }

        private void UpdateInteriorEnemyOffset(float hitAge, Vector3 scale)
        {
            // Apply world-sized offsets in the child renderer's local space.
            var entryRemaining = 1f - Mathf.Clamp01((Time.time - entryStartedAt) / 0.20f);
            var entryEase = entryRemaining * entryRemaining;
            var hitProgress = Mathf.Clamp01(hitAge / 0.15f);
            var recoil = hitAge >= 0f && hitProgress < 1f
                ? Mathf.Min(hitProgress / 0.20f, (1f - hitProgress) / 0.80f)
                : 0f;
            var entryAge = Time.time - entryStartedAt;
            var landing = Mathf.Clamp01(1f - Mathf.Abs(entryAge - 0.10f) / 0.08f);
            var visual = spriteRenderer.transform;
            var squash = new Vector3(1f + 0.11f * landing, 1f - 0.16f * landing, 1f);
            var tilt = Quaternion.Euler(0f, 0f, -recoilDirection * 11f * recoil);
            var offset = new Vector3(
                (-0.29f * entryEase + recoilDirection * 0.19f * recoil) / scale.x,
                (0.18f * entryEase + 0.035f * recoil) / scale.y, 0f);

            // Rotate and squash around the bottom of the visible sprite.
            var feet = new Vector3(0f, spriteRenderer.sprite.bounds.min.y, 0f);
            visual.localScale = squash;
            visual.localRotation = tilt;
            visual.localPosition = visualRestPosition + offset + feet - tilt * Vector3.Scale(feet, squash);
        }

        private float Frequency => style switch
        {
            MotionStyle.EnemyBounce => 4.1f,
            MotionStyle.BusSway => 6.4f,
            _ => 2.0f
        };
    }
}
