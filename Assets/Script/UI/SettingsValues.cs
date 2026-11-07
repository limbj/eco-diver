using System;

namespace Script.UI
{
    public interface ISettingsStore
    {
        float GetFloat(string key, float defaultValue);
        void SetFloat(string key, float value);
        void DeleteKey(string key);
        void Save();
    }

    // Kept independent of Unity so persisted values and reset behavior can be tested directly.
    public sealed class SettingsValues
    {
        public const float DefaultValue = 100f;
        public const float MaximumValue = 200f;
        public const string BrightnessKey = "EcoDiver.Settings.Brightness.v1";
        public const string SoundVolumeKey = "EcoDiver.Settings.SoundVolume.v1";

        public float Brightness { get; private set; } = DefaultValue;
        public float SoundVolume { get; private set; } = DefaultValue;
        public float AudioLevel => Math.Min(1f, SoundVolume / DefaultValue);
        public float ExposureKeyValue => Math.Max(0.03f, Brightness / DefaultValue);

        public void Load(ISettingsStore store)
        {
            Brightness = Normalize(store.GetFloat(BrightnessKey, DefaultValue));
            SoundVolume = Normalize(store.GetFloat(SoundVolumeKey, DefaultValue));
        }

        public void SetBrightness(float value) => Brightness = Normalize(value);
        public void SetSoundVolume(float value) => SoundVolume = Normalize(value);

        public void Stage(ISettingsStore store)
        {
            store.SetFloat(BrightnessKey, Brightness);
            store.SetFloat(SoundVolumeKey, SoundVolume);
        }

        public void Save(ISettingsStore store)
        {
            Stage(store);
            store.Save();
        }

        public void Reset(ISettingsStore store)
        {
            Brightness = DefaultValue;
            SoundVolume = DefaultValue;
            store.DeleteKey(BrightnessKey);
            store.DeleteKey(SoundVolumeKey);
            store.Save();
        }

        private static float Normalize(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return DefaultValue;
            }

            return Math.Max(0f, Math.Min(MaximumValue, value));
        }
    }
}
