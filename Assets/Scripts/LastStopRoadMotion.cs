using UnityEngine;

namespace LastStop
{
    /// <summary>
    /// Presentation-only looping motion for exterior roadside props. It does not
    /// interact with enemies, damage, collision, or simulation state.
    /// </summary>
    public sealed class LastStopRoadMotion : MonoBehaviour
    {
        private float speed;
        private float leftEdge;
        private float rightEdge;

        public void Configure(float scrollSpeed, float wrapLeft, float wrapRight)
        {
            speed = scrollSpeed;
            leftEdge = wrapLeft;
            rightEdge = wrapRight;
        }

        private void Update()
        {
            var position = transform.localPosition;
            position.x -= speed * Time.deltaTime;
            if (position.x < leftEdge)
            {
                position.x += rightEdge - leftEdge;
            }

            transform.localPosition = position;
        }
    }
}
