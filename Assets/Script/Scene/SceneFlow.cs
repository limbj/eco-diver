using System;

namespace Script.Scene
{
    public static class SceneFlow
    {
        private static readonly SceneDetail[] ProductionScenes =
        {
            SceneDetail.TitleScene, SceneDetail.PrologueScene, SceneDetail.Quest1Scene,
            SceneDetail.Quest2Scene, SceneDetail.Quest3Scene, SceneDetail.EndingScene
        };

        private static readonly SceneDetail[] TestScenes =
        {
            SceneDetail.TestStartScene, SceneDetail.TestMainScene, SceneDetail.TestEndScene
        };

        public static bool IsEndingScene(string scenePath)
        {
            return Matches(SceneDetail.EndingScene, scenePath);
        }

        public static bool TryGetNextScene(string scenePath, bool loopAtEnd, out string nextScenePath)
        {
            // The ending always returns to the title, including when opened directly.
            if (IsEndingScene(scenePath))
            {
                nextScenePath = SceneDetail.TitleScene.AssetPath;
                return true;
            }

            return TryGetNext(ProductionScenes, scenePath, loopAtEnd, out nextScenePath)
                   || TryGetNext(TestScenes, scenePath, loopAtEnd, out nextScenePath);
        }

        private static bool TryGetNext(SceneDetail[] scenes, string scenePath, bool loopAtEnd,
            out string nextScenePath)
        {
            for (int i = 0; i < scenes.Length; i++)
            {
                if (!Matches(scenes[i], scenePath)) continue;

                if (i + 1 < scenes.Length)
                {
                    nextScenePath = scenes[i + 1].AssetPath;
                    return true;
                }

                if (loopAtEnd)
                {
                    nextScenePath = scenes[0].AssetPath;
                    return true;
                }

                break;
            }

            nextScenePath = null;
            return false;
        }

        private static bool Matches(SceneDetail scene, string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath)) return false;
            string normalized = scenePath.Replace('\\', '/');
            if (normalized.StartsWith("Assets/", StringComparison.Ordinal))
                normalized = normalized.Substring("Assets/".Length);
            if (normalized.EndsWith(".unity", StringComparison.Ordinal))
                normalized = normalized.Substring(0, normalized.Length - ".unity".Length);

            return string.Equals(normalized, scene.Name, StringComparison.Ordinal)
                   || (normalized.IndexOf('/') < 0 &&
                       string.Equals(normalized, scene.Name.Substring(scene.Name.LastIndexOf('/') + 1),
                           StringComparison.Ordinal));
        }
    }

    // Loading is reserved before the first coroutine yield. A committed Unity async load
    // cannot be cancelled, so callers can distinguish the cancellable delay/fade phase.
    public sealed class SceneTransitionState
    {
        public bool IsLoading { get; private set; }
        public bool IsCommitted { get; private set; }
        public bool IsNextSceneBlocked { get; private set; }

        public bool TryBegin(bool allowBlockedTransition = false)
        {
            if (IsLoading || (IsNextSceneBlocked && !allowBlockedTransition)) return false;
            IsLoading = true;
            IsCommitted = false;
            return true;
        }

        public void MarkLoadStarted()
        {
            if (IsLoading) IsCommitted = true;
        }

        public bool CancelPending()
        {
            IsNextSceneBlocked = true;
            if (IsCommitted) return false;
            IsLoading = false;
            return true;
        }

        public void Complete()
        {
            IsLoading = false;
            IsCommitted = false;
            IsNextSceneBlocked = false;
        }

        public void ReleaseFailedLoad()
        {
            IsLoading = false;
            IsCommitted = false;
        }
    }
}
