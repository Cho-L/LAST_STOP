using System;
using UnityEngine;

namespace LastStop
{
    /// <summary>
    /// Presentation projectile used by bus weapons. Damage resolves only on arrival,
    /// so the visual shot and the combat result remain in sync.
    /// </summary>
    public sealed class LastStopProjectileVisual : MonoBehaviour
    {
        private Transform target;
        private float speed;
        private Action onArrive;
        private Func<bool> targetIsValid;
        private bool faceTarget;

        public void Configure(Transform destination, float travelSpeed, Action arrival, Func<bool> validityCheck, bool orientToTarget = false)
        {
            target = destination;
            speed = travelSpeed;
            onArrive = arrival;
            targetIsValid = validityCheck;
            faceTarget = orientToTarget;
            if (faceTarget) UpdateFacing();
        }

        private void Update()
        {
            if (target == null || (targetIsValid != null && !targetIsValid()))
            {
                Destroy(gameObject);
                return;
            }

            if (faceTarget) UpdateFacing();
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
            if (Vector2.Distance(transform.position, target.position) > 0.08f) return;
            onArrive?.Invoke();
            Destroy(gameObject);
        }

        private void UpdateFacing()
        {
            if (target == null) return;
            var direction = target.position - transform.position;
            if (direction.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

    }
}
