using System;
using UnityEngine;

namespace LastStop
{
    /// <summary>
    /// Stable gameplay-to-audio contract for the MVP. It is deliberately safe while
    /// no clips are assigned: callers can publish cues without an AudioSource.
    /// </summary>
    public enum LastStopAudioEvent
    {
        PlayerDamaged,
        GameOver,
        BreachWarningOn,
        ViewChanged,
        CoreDamaged
    }

    public static class LastStopAudio
    {
        public static event Action<LastStopAudioEvent> CueRequested;

        private static AudioSource source;
        private static readonly AudioClip[] Clips = new AudioClip[5];
        private static readonly float[] LastPlayed = new float[5];
        private static bool gameOverPlayed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            source = null;
            gameOverPlayed = false;
            for (var index = 0; index < LastPlayed.Length; index++) LastPlayed[index] = -100f;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            gameOverPlayed = false;
            for (var index = 0; index < LastPlayed.Length; index++) LastPlayed[index] = -100f;
        }

        public static void TryPlay(LastStopAudioEvent cue)
        {
            CueRequested?.Invoke(cue);
            if (!Application.isPlaying) return;

            var index = (int)cue;
            if (index < 0 || index >= Clips.Length) return;
            if (cue == LastStopAudioEvent.GameOver && gameOverPlayed) return;
            var now = Time.unscaledTime;
            var cooldown = cue == LastStopAudioEvent.ViewChanged ? 0.15f : 0.35f;
            if (now - LastPlayed[index] < cooldown) return;
            LastPlayed[index] = now;
            if (cue == LastStopAudioEvent.GameOver) gameOverPlayed = true;

            if (source == null)
            {
                var host = new GameObject("Last Stop Audio Cues");
                UnityEngine.Object.DontDestroyOnLoad(host);
                source = host.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = 0.28f;
            }

            if (Clips[index] == null) Clips[index] = CreateCue(cue);
            source.PlayOneShot(Clips[index]);
        }

        private static AudioClip CreateCue(LastStopAudioEvent cue)
        {
            const int sampleRate = 22050;
            const float duration = 0.22f;
            var samples = new float[(int)(sampleRate * duration)];
            var startFrequency = cue switch
            {
                LastStopAudioEvent.PlayerDamaged => 410f,
                LastStopAudioEvent.CoreDamaged => 260f,
                LastStopAudioEvent.GameOver => 330f,
                LastStopAudioEvent.BreachWarningOn => 520f,
                LastStopAudioEvent.ViewChanged => 620f,
                _ => 740f
            };
            var endFrequency = cue switch
            {
                LastStopAudioEvent.PlayerDamaged => 280f,
                LastStopAudioEvent.CoreDamaged => 145f,
                LastStopAudioEvent.GameOver => 165f,
                LastStopAudioEvent.BreachWarningOn => 590f,
                LastStopAudioEvent.ViewChanged => 780f,
                _ => 980f
            };
            var phase = 0f;
            for (var index = 0; index < samples.Length; index++)
            {
                var progress = (float)index / samples.Length;
                phase += 2f * Mathf.PI * Mathf.Lerp(startFrequency, endFrequency, progress) / sampleRate;
                var envelope = Mathf.Sin(Mathf.PI * progress) * (1f - progress * 0.35f);
                samples[index] = Mathf.Sin(phase) * envelope * 0.45f;
            }

            var clip = AudioClip.Create("LastStop_" + cue, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
