using UnityEngine;

namespace TheLastArk.UI
{
    /// <summary>A short original bass impact and dry crack; no external sound asset required.</summary>
    public sealed class PanicImpactAudio : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip impact;

        public void Play()
        {
            float volume = Mathf.Clamp01(PlayerPrefs.GetFloat("Setting.SFXVolume", 1f))
                * Mathf.Clamp01(PlayerPrefs.GetFloat("Setting.MasterVolume", 1f));
            if (volume <= 0f) return;
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0;
                source.loop = false;
                // Read the game's saved master setting directly, without applying listener volume twice.
                source.ignoreListenerVolume = true;
            }
            if (impact == null) impact = CreateImpact();
            source.PlayOneShot(impact, volume * .65f);
        }

        private static AudioClip CreateImpact()
        {
            const int sampleRate = 44100;
            const float duration = .8f;
            var samples = new float[(int)(sampleRate * duration)];
            var random = new System.Random(1701);
            float lowNoise = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)sampleRate;
                float noise = (float)random.NextDouble() * 2 - 1;
                lowNoise = Mathf.Lerp(lowNoise, noise, .14f);
                float bass = Mathf.Sin(2 * Mathf.PI * (68 * t - 22 * t * t)) * Mathf.Exp(-8 * t);
                float crack = (noise - lowNoise) * Mathf.Exp(-45 * t);
                float rumble = lowNoise * Mathf.Exp(-6 * t);
                float envelope = Mathf.Clamp01(t / .004f) * Mathf.Clamp01((duration - t) / .06f);
                samples[i] = (bass * .55f + crack * .21f + rumble * .2f) * envelope;
            }
            var clip = AudioClip.Create("Panic Impact", samples.Length, 1, sampleRate, false);
            clip.hideFlags = HideFlags.HideAndDontSave;
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDisable() { if (source != null) source.Stop(); }
        private void OnDestroy() { if (impact != null) Destroy(impact); }
    }
}
