using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Script.Scene
{
    [RequireComponent(typeof(VideoPlayer))]
    public class VideoEndDetector : MonoBehaviour
    {
        private VideoPlayer _videoPlayer;
        private Coroutine _transitionCoroutine;
        private int _sourceSceneHandle;

        private void OnEnable()
        {
            _videoPlayer = GetComponent<VideoPlayer>();
            _sourceSceneHandle = gameObject.scene.handle;
            if (!_videoPlayer) return;
            _videoPlayer.loopPointReached += OnVideoEnd;
            _videoPlayer.errorReceived += OnVideoError;
        }

        private void Start()
        {
            if (!_videoPlayer) return;
            bool hasSource = _videoPlayer.source == VideoSource.VideoClip
                ? _videoPlayer.clip != null : !string.IsNullOrEmpty(_videoPlayer.url);
            if (!hasSource)
            {
                Debug.LogWarning("Video source is missing; continuing to the next scene.");
                ScheduleTransition();
            }
        }

        private void OnDisable()
        {
            if (_videoPlayer)
            {
                _videoPlayer.loopPointReached -= OnVideoEnd;
                _videoPlayer.errorReceived -= OnVideoError;
            }
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }
        }

        private void OnVideoEnd(VideoPlayer player)
        {
            ScheduleTransition();
        }

        private void OnVideoError(VideoPlayer player, string message)
        {
            Debug.LogWarning("Video could not be played: " + message);
            ScheduleTransition();
        }

        private void ScheduleTransition()
        {
            if (_transitionCoroutine == null)
                _transitionCoroutine = StartCoroutine(WaitAndLoadScene());
        }

        private IEnumerator WaitAndLoadScene()
        {
            yield return new WaitForSecondsRealtime(5f);
            if (!isActiveAndEnabled || SceneManager.GetActiveScene().handle != _sourceSceneHandle)
                yield break;

            var loader = SceneLoader.Instance;
            if (!loader.IsLoadingScene) loader.LoadNextScene();
        }
    }
}
