using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using static Unity.XR.CoreUtils.XROrigin.TrackingOriginMode;

namespace Script.UI
{
    public class SettingsManager : MonoBehaviour
    {
        private static SettingsManager _instance;

        public static SettingsManager Instance
        {
            get
            {
                if (!_instance)
                {
                    SetupInstance();
                }

                _instance.EnsureInitialized();
                return _instance;
            }
        }

        public static bool TryGetExisting(out SettingsManager settings)
        {
            settings = _instance;
            return settings != null;
        }

        private static void SetupInstance()
        {
            _instance = FindObjectOfType<SettingsManager>();
            if (_instance == null)
            {
                GameObject gameObj = new GameObject("SettingsManager");
                _instance = gameObj.AddComponent<SettingsManager>();
                DontDestroyOnLoad(gameObj);
            }
        }

        private const XROrigin.TrackingOriginMode InitialTrackingMode = Device;

        private XROrigin.TrackingOriginMode _trackingMode = InitialTrackingMode;
        private readonly SettingsValues _values = new SettingsValues();
        private readonly ISettingsStore _store = new PlayerPrefsSettingsStore();
        private readonly List<ExposureState> _exposures = new List<ExposureState>();
        private bool _initialized;

        [SerializeField] [Tooltip("Limit frame-rate")]
        private int maxFrameRate = 60;
        [SerializeField] [Tooltip("Post Process Profile")]
        private PostProcessProfile _postProcessProfile;
        [SerializeField] [Tooltip("Post Process Layer")]
        private PostProcessLayer _postProcessLayer;

        public XROrigin.TrackingOriginMode TrackingMode
        {
            get => _trackingMode;
            set
            {
                _trackingMode = value;
                UpdateXROrigin();
            }
        }

        public float Brightness
        {
            get
            {
                EnsureInitialized();
                return _values.Brightness;
            }
            set
            {
                EnsureInitialized();
                _values.SetBrightness(value);
                _values.Stage(_store);
                AdjustBrightness();
            }
        }

        public float SoundVolume
        {
            get
            {
                EnsureInitialized();
                return _values.SoundVolume;
            }
            set
            {
                EnsureInitialized();
                _values.SetSoundVolume(value);
                _values.Stage(_store);
                AdjustSoundEffect();
            }
        }

        public bool IsFloorInitialized { get; set; }

        public void ClearSettings()
        {
            EnsureInitialized();
            _values.Reset(_store);
            AdjustBrightness();
            AdjustSoundEffect();
            // XR floor calibration belongs to the current device/session, not user preferences.
        }

        public void SaveSettings()
        {
            EnsureInitialized();
            _values.Save(_store);
        }

        public void Reload()
        {
            EnsureInitialized();
            BindPostProcessing();
            AdjustBrightness();
            AdjustSoundEffect();
            UpdateXROrigin();
            UpdateFrameRate();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureInitialized();
        }

        private void OnEnable()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            if (_instance == this)
            {
                Reload();
            }
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _values.Load(_store);
            _initialized = true;
            AdjustSoundEffect();
            UpdateFrameRate();
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            Reload();
        }

        private void BindPostProcessing()
        {
            RestoreProfiles();
            if (!_postProcessLayer)
            {
                _postProcessLayer = FindObjectOfType<PostProcessLayer>();
            }

            foreach (var volume in FindObjectsOfType<PostProcessVolume>())
            {
                if (volume.enabled && volume.sharedProfile != null)
                {
                    // Work on a runtime instance instead of changing a shared project asset.
                    AddExposure(volume.profile);
                }
            }

            if (_exposures.Count == 0)
            {
                AddExposure(_postProcessProfile);
            }
        }

        private void AddExposure(PostProcessProfile profile)
        {
            if (profile == null || !profile.TryGetSettings(out AutoExposure exposure))
            {
                return;
            }

            foreach (var state in _exposures)
            {
                if (state.Exposure == exposure)
                {
                    return;
                }
            }

            _exposures.Add(new ExposureState(exposure, exposure.keyValue.value));
        }

        private void AdjustSoundEffect()
        {
            AudioListener.volume = _values.AudioLevel;
        }

        private void AdjustBrightness()
        {
            foreach (var state in _exposures)
            {
                if (state.Exposure)
                {
                    state.Exposure.keyValue.value = _values.ExposureKeyValue;
                }
            }
        }

        private void RestoreProfiles()
        {
            foreach (var state in _exposures)
            {
                if (state.Exposure)
                {
                    state.Exposure.keyValue.value = state.OriginalValue;
                }
            }

            _exposures.Clear();
        }

        private void UpdateXROrigin()
        {
            var xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null)
            {
                xrOrigin.RequestedTrackingOriginMode = _trackingMode;
            }
        }

        private void UpdateFrameRate()
        {
            int frameRate = maxFrameRate > 0 ? maxFrameRate : 60;
            Application.targetFrameRate = frameRate;
            Time.fixedDeltaTime = 1f / frameRate;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _instance == this)
            {
                SaveSettings();
            }
        }

        private void OnApplicationQuit()
        {
            if (_instance == this)
            {
                SaveSettings();
                RestoreProfiles();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                RestoreProfiles();
                _instance = null;
            }
        }

        private sealed class ExposureState
        {
            public readonly AutoExposure Exposure;
            public readonly float OriginalValue;

            public ExposureState(AutoExposure exposure, float originalValue)
            {
                Exposure = exposure;
                OriginalValue = originalValue;
            }
        }

        private sealed class PlayerPrefsSettingsStore : ISettingsStore
        {
            public float GetFloat(string key, float defaultValue) => PlayerPrefs.GetFloat(key, defaultValue);
            public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
            public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
            public void Save() => PlayerPrefs.Save();
        }
    }
}
