using UnityEngine;
using UnityEngine.UIElements;

namespace Script.UI
{
    public class SettingsMenuEvent : MonoBehaviour
    {
        [SerializeField] [Tooltip("Verbose Mode")]
        private bool verbose = false;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private Slider _soundEffectSlider;
        private Slider _brightnessSlider;
        private Button _clearButton;

        private void OnEnable()
        {
            BindControls();
        }

        private void Start()
        {
            // UIDocument can create its visual tree after this component's OnEnable.
            BindControls();
        }

        private void LateUpdate()
        {
            if (_uiDocument == null || _uiDocument.rootVisualElement != _root)
            {
                BindControls();
            }
        }

        private void OnDisable()
        {
            UnbindControls();
            if (SettingsManager.TryGetExisting(out var settings))
            {
                settings.SaveSettings();
            }
        }

        private void BindControls()
        {
            UnbindControls();
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null || _uiDocument.rootVisualElement == null)
            {
                return;
            }

            _root = _uiDocument.rootVisualElement;
            _soundEffectSlider = _root.Q<Slider>("SoundEffectSlider");
            if (_soundEffectSlider == null)
            {
                Debug.LogWarning("SoundEffectSlider is null!");
            }
            else
            {
                _soundEffectSlider.RegisterValueChangedCallback(OnSoundEffectSliderChanged);
            }

            _brightnessSlider = _root.Q<Slider>("BrightnessSlider");
            if (_brightnessSlider == null)
            {
                Debug.LogWarning("BrightnessSlider is null!");
            }
            else
            {
                _brightnessSlider.RegisterValueChangedCallback(OnBrightnessSliderChanged);
            }

            _clearButton = _root.Q<Button>("ClearButton");
            if (_clearButton == null)
            {
                Debug.LogWarning("ClearButton is null!");
            }
            else
            {
                _clearButton.RegisterCallback<ClickEvent>(OnClearButtonClicked);
            }

            SyncControls();
        }

        private void UnbindControls()
        {
            _soundEffectSlider?.UnregisterValueChangedCallback(OnSoundEffectSliderChanged);
            _brightnessSlider?.UnregisterValueChangedCallback(OnBrightnessSliderChanged);
            _clearButton?.UnregisterCallback<ClickEvent>(OnClearButtonClicked);
            _soundEffectSlider = null;
            _brightnessSlider = null;
            _clearButton = null;
            _root = null;
        }

        private void SyncControls()
        {
            var settings = SettingsManager.Instance;
            _soundEffectSlider?.SetValueWithoutNotify(settings.SoundVolume);
            _brightnessSlider?.SetValueWithoutNotify(settings.Brightness);
        }

        private void OnSoundEffectSliderChanged(ChangeEvent<float> evt)
        {
            float volume = evt.newValue;
            if (verbose)
            {
                Debug.Log($"Sound Effect Volume set to: {volume}%");
            }
            SettingsManager.Instance.SoundVolume = volume;
        }

        private void OnBrightnessSliderChanged(ChangeEvent<float> evt)
        {
            float brightness = evt.newValue;
            if (verbose)
            {
                Debug.Log($"Brightness set to: {brightness}%");
            }
            SettingsManager.Instance.Brightness = brightness;
        }

        private void OnClearButtonClicked(ClickEvent _)
        {
            if (verbose)
            {
                Debug.Log("Resetting sound and brightness settings...");
            }

            SettingsManager.Instance.ClearSettings();
            SyncControls();
        }
    }
}
