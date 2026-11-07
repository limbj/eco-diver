using System;
using Script.Scene;

public static class SceneFlowTests
{
    public static void Run()
    {
        Next(SceneDetail.TitleScene.AssetPath, false, SceneDetail.PrologueScene.AssetPath);
        Next(SceneDetail.PrologueScene.AssetPath, false, SceneDetail.Quest1Scene.AssetPath);
        Next(SceneDetail.Quest1Scene.AssetPath, false, SceneDetail.Quest2Scene.AssetPath);
        Next(SceneDetail.Quest2Scene.AssetPath, false, SceneDetail.Quest3Scene.AssetPath);
        Next(SceneDetail.Quest3Scene.AssetPath, false, SceneDetail.EndingScene.AssetPath);
        Next(SceneDetail.EndingScene.AssetPath, false, SceneDetail.TitleScene.AssetPath);
        Next(SceneDetail.EndingScene.AssetPath, true, SceneDetail.TitleScene.AssetPath);
        Next("EndingScene", false, SceneDetail.TitleScene.AssetPath);
        Next(@"Assets\Level\Production\Quest3Scene.unity", false, SceneDetail.EndingScene.AssetPath);
        Next(SceneDetail.TestEndScene.AssetPath, true, SceneDetail.TestStartScene.AssetPath);
        Assert(!SceneFlow.TryGetNextScene(SceneDetail.TestEndScene.AssetPath, false, out _),
            "Non-looping test sequence must stop at its end.");
        Assert(!SceneFlow.TryGetNextScene("Assets/Other/Quest3Scene.unity", true, out _),
            "A similarly named scene outside the scenario must not advance it.");
        Assert(!SceneFlow.TryGetNextScene("Assets/Level/Production/Quest3SceneCopy.unity", true, out _),
            "Scene matching must not use substrings.");
        Assert(!SceneFlow.TryGetNextScene(null, true, out _), "Missing scene must be rejected.");

        var transition = new SceneTransitionState();
        Assert(transition.TryBegin(), "First request must reserve the transition immediately.");
        Assert(transition.IsLoading, "Reservation must last through delay and fade.");
        Assert(!transition.TryBegin(), "Duplicate next request must be ignored.");
        Assert(!transition.TryBegin(true), "A restart cannot race another reserved load.");
        Assert(transition.CancelPending(), "An uncommitted transition must be cancellable.");
        Assert(!transition.IsLoading, "Cancellation must release the load reservation.");
        Assert(!transition.TryBegin(), "Failure must block next-scene input and quest callbacks.");
        Assert(transition.TryBegin(true), "Failure UI must still be able to restart or return to title.");
        transition.MarkLoadStarted();
        Assert(!transition.CancelPending(), "Unity async loads cannot be cancelled after commitment.");
        Assert(transition.IsLoading, "An irreversible async load must retain its reservation.");
        Assert(!transition.TryBegin(true), "An in-flight async load must not be overlapped.");
        transition.Complete();
        Assert(!transition.IsLoading && !transition.IsCommitted && !transition.IsNextSceneBlocked,
            "A completed scene must reset all transition state.");
        Assert(transition.TryBegin(), "The new scene must accept the next normal transition.");

        transition.CancelPending();
        Assert(transition.TryBegin(true), "Failure recovery must be allowed before a failed load.");
        transition.ReleaseFailedLoad();
        Assert(!transition.IsLoading && transition.IsNextSceneBlocked,
            "A failed recovery load must release busy state while preserving the failure input block.");
        Assert(transition.TryBegin(true), "A failed recovery load must be retryable.");
    }

    private static void Next(string current, bool loop, string expected)
    {
        Assert(SceneFlow.TryGetNextScene(current, loop, out string actual) && actual == expected,
            "Incorrect scene transition from " + current + ". Expected " + expected + ", got " + actual);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
