using System.Collections.Generic;
using Script.Equipment;
using Script.Display;
using Script.Interaction;
using Script.Quest;
using Script.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using InputDevice = UnityEngine.XR.InputDevice;
using CommonUsages = UnityEngine.XR.CommonUsages;
using DiveQuest = Script.Quest.Quest;

namespace Script.UI
{
    /// <summary>Created by the existing oxygen component, without scene-specific wiring.</summary>
    public sealed class DiveFailureController : MonoBehaviour
    {
        private readonly List<Behaviour> _pausedBehaviours = new List<Behaviour>();
        private readonly FailureMenuInput _input = new FailureMenuInput();
        private GameObject _panel;
        private TMP_Text _status;
        private Button _restartButton;
        private Button _titleButton;
        private bool _transitionPending;
        private bool _ownsPause;
        private float _previousTimeScale;
        private bool _previousCursorVisible;
        private CursorLockMode _previousCursorLock;
        private int _failedSceneHandle;

        public bool IsFailed { get; private set; }

        public void Show()
        {
            if (IsFailed || SceneLoader.Instance.IsLoadingScene) return;
            IsFailed = true;
            _failedSceneHandle = SceneManager.GetActiveScene().handle;
            SceneLoader.Instance.CancelPendingTransition();
            if (QuestObserver.Instance != null) QuestObserver.Instance.SuspendForFailure();

            Pause<DiverController>();
            Pause<ToolSelector>();
            Pause<Slicer>();
            Pause<LocomotionProvider>();
            Pause<XRDirectInteractor>();
            Pause<DiveQuest>();

            _previousTimeScale = Time.timeScale;
            _previousCursorVisible = Cursor.visible;
            _previousCursorLock = Cursor.lockState;
            _ownsPause = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            DisplayManager.Instance.QuestName = "Dive failed";
            DisplayManager.Instance.QuestDescription = "Oxygen depleted. Restart the mission or return to the title.";
            BuildPanel();
        }

        private void Pause<T>() where T : Behaviour
        {
            foreach (var behaviour in FindObjectsOfType<T>())
            {
                if (behaviour.enabled && !_pausedBehaviours.Contains(behaviour))
                {
                    _pausedBehaviours.Add(behaviour);
                    behaviour.enabled = false;
                }
            }
        }

        private void Update()
        {
            if (!IsFailed) return;
            if (_transitionPending)
            {
                if (!SceneLoader.Instance.IsLoadingScene && SceneManager.GetActiveScene().handle == _failedSceneHandle)
                    RecoverSelection();
                return;
            }
            bool restart = IsControllerButton(CommonUsages.primaryButton)
                           || (Keyboard.current != null && Keyboard.current.rKey.isPressed);
            bool title = IsControllerButton(CommonUsages.secondaryButton)
                         || (Keyboard.current != null && Keyboard.current.escapeKey.isPressed);
            FailureMenuChoice choice = _input.Advance(Time.unscaledDeltaTime, restart, title);
            _restartButton.interactable = _input.CanChoose;
            _titleButton.interactable = _input.CanChoose;
            if (choice != FailureMenuChoice.None) Transition(choice);
        }

        private static bool IsControllerButton(InputFeatureUsage<bool> usage)
        {
            InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            return (left.TryGetFeatureValue(usage, out bool leftPressed) && leftPressed)
                   || (right.TryGetFeatureValue(usage, out bool rightPressed) && rightPressed);
        }

        private void RestartMission()
        {
            FailureMenuChoice choice = _input.Choose(FailureMenuChoice.Restart);
            if (choice != FailureMenuChoice.None) Transition(choice);
        }

        private void ReturnToTitle()
        {
            FailureMenuChoice choice = _input.Choose(FailureMenuChoice.Title);
            if (choice != FailureMenuChoice.None) Transition(choice);
        }

        private void Transition(FailureMenuChoice choice)
        {
            if (_transitionPending || SceneLoader.Instance.IsLoadingScene) return;
            _transitionPending = true;
            RestorePause();
            if (choice == FailureMenuChoice.Restart) SceneLoader.Instance.RestartCurrentScene();
            else SceneLoader.Instance.ReturnToTitle();

            if (SceneLoader.Instance.IsLoadingScene)
            {
                _panel.SetActive(false);
                // Gameplay components stay suspended until the old scene is unloaded.
                return;
            }

            RecoverSelection();
        }

        private void RecoverSelection()
        {
            _transitionPending = false;
            _input.RetrySelection();
            SceneLoader.Instance.CancelPendingTransition();
            _ownsPause = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _panel.SetActive(true);
            _status.text = "The scene could not be loaded. Please choose again.";
        }

        private void RestorePause()
        {
            if (!_ownsPause) return;
            Time.timeScale = _previousTimeScale;
            Cursor.lockState = _previousCursorLock;
            Cursor.visible = _previousCursorVisible;
            _ownsPause = false;
        }

        private void OnDisable()
        {
            RestorePause();
            if (!_transitionPending)
                foreach (var behaviour in _pausedBehaviours)
                    if (behaviour != null) behaviour.enabled = true;
            if (_panel != null) Destroy(_panel);
        }

        private void BuildPanel()
        {
            Camera viewer = Camera.main;
            if (viewer == null) viewer = FindObjectOfType<Camera>();
            _panel = new GameObject("Dive failure", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = _panel.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue;
            var rect = _panel.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(680f, 390f);
            if (viewer != null)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = viewer;
                rect.SetParent(viewer.transform, false);
                rect.localPosition = new Vector3(0f, 0f, 1.6f);
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one * 0.002f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
            }

            Image background = _panel.AddComponent<Image>();
            background.color = new Color(0.025f, 0.09f, 0.15f, 0.97f);
            AddText("DIVE INTERRUPTED", 28f, 17f, new Color(0.43f, 0.85f, 0.95f), 0f, 143f, 600f, 34f);
            AddText("Oxygen depleted", 39f, 0f, Color.white, 0f, 87f, 600f, 62f);
            _status = AddText("Your current mission has stopped.\nRestart with a full oxygen tank, or return to the title.",
                21f, 0f, new Color(0.8f, 0.86f, 0.91f), 0f, 16f, 600f, 76f);
            _restartButton = AddButton("Restart mission", -155f, -71f, RestartMission);
            _titleButton = AddButton("Return to title", 155f, -71f, ReturnToTitle);
            AddText("A / X or R: restart     B / Y or Esc: title", 18f, 0f,
                new Color(0.64f, 0.75f, 0.83f), 0f, -147f, 620f, 35f);

            if (FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("Failure menu event system", typeof(EventSystem),
                    typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }
        }

        private TMP_Text AddText(string value, float size, float characterSpacing, Color color,
            float x, float y, float width, float height)
        {
            var obj = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = obj.GetComponent<RectTransform>();
            rect.SetParent(_panel.transform, false);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
            var text = obj.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.characterSpacing = characterSpacing;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private Button AddButton(string label, float x, float y, UnityEngine.Events.UnityAction click)
        {
            var obj = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = obj.GetComponent<RectTransform>();
            rect.SetParent(_panel.transform, false);
            rect.sizeDelta = new Vector2(285f, 58f);
            rect.anchoredPosition = new Vector2(x, y);
            var image = obj.GetComponent<Image>();
            image.color = new Color(0.08f, 0.4f, 0.53f);
            var button = obj.GetComponent<Button>();
            button.targetGraphic = image;
            button.interactable = false;
            button.onClick.AddListener(click);
            TMP_Text text = AddText(label, 22f, 0f, Color.white, x, y, 280f, 54f);
            text.transform.SetParent(obj.transform, true);
            return button;
        }
    }
}
