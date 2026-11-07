using System;
using System.Collections;
using Scene;
using Script.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Script.Scene
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] [Tooltip("Temporary button for loading the next scene")]
        private InputActionReference loadButton;

        [SerializeField] [Tooltip("Loop the test scenario back to its first scene")]
        private bool returnToTitle = true;

        [SerializeField] private bool verbose;
        [SerializeField] [Min(0)] private float secondsToWaitLoad = 1f;

        private readonly SceneTransitionState _transitionState = new SceneTransitionState();
        private FadeScreen _fadeScreen;
        private Coroutine _transitionCoroutine;
        private Coroutine _holdButtonCoroutine;
        private Coroutine _endingFallbackCoroutine;
        private AsyncOperation _asyncLoad;
        private static SceneLoader _instance;

        public bool IsLoadingScene => _transitionState.IsLoading;

        public static SceneLoader Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = FindObjectOfType<SceneLoader>();
                    if (!_instance)
                        _instance = new GameObject("SceneLoader").AddComponent<SceneLoader>();
                }
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureSceneLoader()
        {
            // Directly opening a mission or ending in the editor must work as well.
            _ = Instance;
            SettingsManager.Instance.Reload();
        }

        private void Awake()
        {
            if (_instance && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            ConfigureCurrentScene(SceneManager.GetActiveScene());
        }

        private void OnDestroy()
        {
            if (_instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _instance = null;
        }

        private static string CurrentScenePath()
        {
            var currentScene = SceneManager.GetActiveScene();
            return string.IsNullOrEmpty(currentScene.path) ? currentScene.name : currentScene.path;
        }

        public void LoadNextScene()
        {
            if (_transitionState.IsNextSceneBlocked || IsLoadingScene) return;
            if (!SceneFlow.TryGetNextScene(CurrentScenePath(), returnToTitle, out string nextScene))
            {
                Debug.LogWarning("No next scene configured for " + CurrentScenePath());
                return;
            }
            BeginTransition(nextScene, secondsToWaitLoad, false);
        }

        public void RestartCurrentScene()
        {
            BeginTransition(CurrentScenePath(), 0f, true);
        }

        public void ReturnToTitle()
        {
            BeginTransition(SceneDetail.TitleScene.AssetPath, 0f, true);
        }

        public void CancelPendingTransition()
        {
            if (_holdButtonCoroutine != null)
            {
                StopCoroutine(_holdButtonCoroutine);
                _holdButtonCoroutine = null;
            }

            bool hadPendingTransition = IsLoadingScene;
            if (!_transitionState.CancelPending()) return;
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }
            if (hadPendingTransition && _fadeScreen) _fadeScreen.FadeIn();
        }

        private void BeginTransition(string scenePath, float delay, bool allowBlockedTransition)
        {
            if (IsLoadingScene) return;
            if (!Application.CanStreamedLevelBeLoaded(scenePath))
            {
                Debug.LogError("Scene is not available in Build Settings: " + scenePath);
                return;
            }
            if (!_transitionState.TryBegin(allowBlockedTransition)) return;

            if (verbose) Debug.Log("Loading scene: " + scenePath);
            _transitionCoroutine = StartCoroutine(TransitionScene(scenePath, delay));
        }

        private IEnumerator TransitionScene(string scenePath, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            var fadeScreen = GetFadeScreen();
            if (fadeScreen)
            {
                fadeScreen.gameObject.SetActive(true);
                fadeScreen.FadeOut();
                if (fadeScreen.FadeDuration > 0f)
                    yield return new WaitForSecondsRealtime(fadeScreen.FadeDuration);
            }

            Exception loadError = null;
            try
            {
                _asyncLoad = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                loadError = exception;
            }

            if (_asyncLoad == null)
            {
                _transitionState.ReleaseFailedLoad();
                _transitionCoroutine = null;
                if (fadeScreen) fadeScreen.FadeIn();
                Debug.LogError("Could not load scene " + scenePath + ": " + loadError?.Message);
                yield break;
            }

            _transitionState.MarkLoadStarted();
            // Keep the reservation until sceneLoaded, not just until activation is allowed.
            yield return _asyncLoad;
        }

        private FadeScreen GetFadeScreen()
        {
            if (!_fadeScreen) _fadeScreen = FindObjectOfType<FadeScreen>(true);
            return _fadeScreen;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
            if (_endingFallbackCoroutine != null) StopCoroutine(_endingFallbackCoroutine);
            _transitionState.Complete();
            _transitionCoroutine = null;
            _endingFallbackCoroutine = null;
            _asyncLoad = null;
            if (_holdButtonCoroutine != null)
            {
                StopCoroutine(_holdButtonCoroutine);
                _holdButtonCoroutine = null;
            }
            _fadeScreen = null;
            ConfigureCurrentScene(scene);
            SettingsManager.Instance.Reload();
        }

        private void ConfigureCurrentScene(UnityEngine.SceneManagement.Scene scene)
        {
            var fadeScreen = GetFadeScreen();
            if (fadeScreen) fadeScreen.gameObject.SetActive(true);
            if (!SceneFlow.IsEndingScene(scene.path)) return;

            int playerCount = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var player in root.GetComponentsInChildren<VideoPlayer>(true))
                {
                    playerCount++;
                    player.isLooping = false;
                    if (!player.GetComponent<VideoEndDetector>())
                        player.gameObject.AddComponent<VideoEndDetector>();
                }
            }
            if (playerCount == 0)
            {
                Debug.LogWarning("Ending video player is missing; returning to the title.");
                _endingFallbackCoroutine = StartCoroutine(ReturnFromMissingEndingVideo(scene.handle));
            }
        }

        private IEnumerator ReturnFromMissingEndingVideo(int sourceSceneHandle)
        {
            yield return new WaitForSecondsRealtime(5f);
            _endingFallbackCoroutine = null;
            if (SceneManager.GetActiveScene().handle == sourceSceneHandle && !IsLoadingScene)
                ReturnToTitle();
        }

        private void Update()
        {
            if (_transitionState.IsNextSceneBlocked || IsLoadingScene) return;
            if (loadButton && loadButton.action != null && loadButton.action.WasPressedThisFrame()
                && _holdButtonCoroutine == null)
                _holdButtonCoroutine = StartCoroutine(LoadButtonCoroutine());
        }

        private IEnumerator LoadButtonCoroutine()
        {
            yield return new WaitForSecondsRealtime(1f);
            _holdButtonCoroutine = null;
            if (loadButton && loadButton.action != null && loadButton.action.IsPressed())
                LoadNextScene();
        }
    }
}
