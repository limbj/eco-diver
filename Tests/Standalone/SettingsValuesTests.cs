using System;
using System.Collections.Generic;
using Script.UI;

public static class SettingsValuesTests
{
    public static void Run()
    {
        DefaultsAndRoundTrip();
        InvalidValuesAreNormalized();
        ResetPreservesOtherPreferences();
        OutputLevelsAreSafe();
        SliderChangesDoNotFlushOnEveryEvent();
    }

    private static void DefaultsAndRoundTrip()
    {
        var store = new MemoryStore();
        var values = new SettingsValues();
        values.Load(store);
        Equal(100f, values.Brightness, "default brightness");
        Equal(100f, values.SoundVolume, "default sound");
        values.SetBrightness(152.5f);
        values.SetSoundVolume(24f);
        values.Save(store);

        var restored = new SettingsValues();
        restored.Load(store);
        Equal(152.5f, restored.Brightness, "restored brightness");
        Equal(24f, restored.SoundVolume, "restored sound");
        Equal(1, store.SaveCount, "one explicit save");
    }

    private static void InvalidValuesAreNormalized()
    {
        var store = new MemoryStore();
        store.SetFloat(SettingsValues.BrightnessKey, float.NaN);
        store.SetFloat(SettingsValues.SoundVolumeKey, float.PositiveInfinity);
        var values = new SettingsValues();
        values.Load(store);
        Equal(100f, values.Brightness, "invalid stored brightness falls back");
        Equal(100f, values.SoundVolume, "invalid stored sound falls back");

        values.SetBrightness(-2f);
        values.SetSoundVolume(400f);
        Equal(0f, values.Brightness, "negative brightness clamped");
        Equal(200f, values.SoundVolume, "large sound clamped");
        values.SetBrightness(float.NegativeInfinity);
        values.SetSoundVolume(float.NaN);
        Equal(100f, values.Brightness, "invalid input brightness falls back");
        Equal(100f, values.SoundVolume, "invalid input sound falls back");

        store.SetFloat(SettingsValues.BrightnessKey, 300f);
        store.SetFloat(SettingsValues.SoundVolumeKey, -100f);
        values.Load(store);
        Equal(200f, values.Brightness, "stored brightness clamped");
        Equal(0f, values.SoundVolume, "stored sound clamped");
    }

    private static void ResetPreservesOtherPreferences()
    {
        var store = new MemoryStore();
        store.SetFloat("OtherFeature.SaveSlot", 7f);
        var values = new SettingsValues();
        values.SetBrightness(0f);
        values.SetSoundVolume(10f);
        values.Save(store);
        values.Reset(store);
        Equal(100f, values.Brightness, "reset brightness");
        Equal(100f, values.SoundVolume, "reset sound");
        Equal(7f, store.GetFloat("OtherFeature.SaveSlot", -1f), "unrelated preference survives");
        Equal(-1f, store.GetFloat(SettingsValues.BrightnessKey, -1f), "brightness override removed");
        Equal(-1f, store.GetFloat(SettingsValues.SoundVolumeKey, -1f), "sound override removed");

        var restored = new SettingsValues();
        restored.Load(store);
        Equal(100f, restored.Brightness, "reset persists after relaunch");
        Equal(100f, restored.SoundVolume, "reset sound persists after relaunch");
        Equal(2, store.SaveCount, "reset flushed immediately");
    }

    private static void OutputLevelsAreSafe()
    {
        var values = new SettingsValues();
        values.SetSoundVolume(0f);
        Equal(0f, values.AudioLevel, "zero sound mutes");
        values.SetSoundVolume(25f);
        Equal(0.25f, values.AudioLevel, "sound percent conversion");
        values.SetSoundVolume(200f);
        Equal(1f, values.AudioLevel, "listener volume capped");
        values.SetBrightness(0f);
        Equal(0.03f, values.ExposureKeyValue, "minimum exposure remains usable");
        values.SetBrightness(200f);
        Equal(2f, values.ExposureKeyValue, "brightness percent conversion");
    }

    private static void SliderChangesDoNotFlushOnEveryEvent()
    {
        var store = new MemoryStore();
        var values = new SettingsValues();
        values.SetSoundVolume(63f);
        values.Stage(store);
        Equal(63f, store.GetFloat(SettingsValues.SoundVolumeKey, -1f), "slider change staged");
        Equal(0, store.SaveCount, "slider staging avoids disk flush");
        values.Save(store);
        Equal(1, store.SaveCount, "checkpoint flushes staged values");
    }

    private static void Equal(float expected, float actual, string message)
    {
        if (float.IsNaN(actual) || float.IsInfinity(actual) || Math.Abs(expected - actual) > 0.0001f)
        {
            throw new Exception(message + ": expected " + expected + ", got " + actual);
        }
    }

    private sealed class MemoryStore : ISettingsStore
    {
        private readonly Dictionary<string, float> _values = new Dictionary<string, float>();
        public int SaveCount { get; private set; }
        public float GetFloat(string key, float defaultValue) => _values.TryGetValue(key, out var value) ? value : defaultValue;
        public void SetFloat(string key, float value) => _values[key] = value;
        public void DeleteKey(string key) => _values.Remove(key);
        public void Save() => SaveCount++;
    }
}
