using UnityEngine;

namespace LastStop
{
    /// <summary>Visual-only wheel frames. The bus transform and collision logic remain untouched.</summary>
    public sealed class LastStopBusWheelAnimation : MonoBehaviour
    {
        private const float FramesPerSecond = 12f;

        private Sprite[] frames;
        private SpriteRenderer wheelRenderer;

        public void Configure(Sprite[] wheelFrames, SpriteRenderer bodyRenderer)
        {
            if (wheelFrames == null || wheelFrames.Length != 4 || bodyRenderer == null) return;

            frames = wheelFrames;
            var wheelObject = new GameObject("Bus Wheels");
            wheelObject.transform.SetParent(transform, false);
            wheelRenderer = wheelObject.AddComponent<SpriteRenderer>();
            wheelRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
            wheelRenderer.sortingOrder = bodyRenderer.sortingOrder - 1;
            wheelRenderer.color = bodyRenderer.color;
            wheelRenderer.sprite = frames[0];
        }

        private void Update()
        {
            if (wheelRenderer == null || frames == null) return;
            var frame = Mathf.FloorToInt(Time.time * FramesPerSecond) % frames.Length;
            wheelRenderer.sprite = frames[frame];
        }
    }
}
