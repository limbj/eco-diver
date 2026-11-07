using System.Linq;
using Script.Environment.Collect;
using UnityEngine;
using static ComponentTestSupport;

public static class CollectionLifetimeTests
{
    public static void Run()
    {
        Case("Scene reload clears partial collection progress", FreshSceneStartsAtZero);
        Case("Destroying a duplicate keeps the live singleton", DuplicateCannotClearSingleton);
        Case("New scene initializes its own collectable selection", NewSceneSelectsOwnItems);
    }

    private static CollectManager Manager()
    {
        CollectManager manager = Create<CollectManager>();
        Call(manager, "Awake");
        return manager;
    }

    private static void FreshSceneStartsAtZero()
    {
        CollectManager first = Manager();
        Call(first, "Start");
        first.CollectItem();
        first.CollectItem();
        Equal(2, first.GetTotalCollectedItems(), "Partial mission progress must first exist.");
        Equal(0, StubRuntime.DontDestroyCalls, "The collection manager must belong to its scene.");
        StubRuntime.UnloadScene();
        Assert(!first, "Scene unload must destroy the previous collection manager.");
        Assert(ReferenceEquals(null, CollectManager.Instance), "Destroying the live manager must clear its static reference.");
        CollectManager next = Manager();
        Call(next, "Start");
        Assert(ReferenceEquals(next, CollectManager.Instance), "A reloaded scene must install its own manager.");
        Equal(0, next.GetTotalCollectedItems(), "Fresh missions and retries must start at zero.");
    }

    private static void DuplicateCannotClearSingleton()
    {
        CollectManager live = Manager();
        live.CollectItem();
        CollectManager duplicate = Manager();
        Assert(ReferenceEquals(live, CollectManager.Instance), "Awake must retain the first live manager.");
        StubRuntime.FlushDestroyed();
        Assert(!duplicate, "A duplicate manager must be destroyed.");
        Assert(ReferenceEquals(live, CollectManager.Instance), "Duplicate OnDestroy must not clear the live manager.");
        Equal(1, live.GetTotalCollectedItems(), "Duplicate cleanup must preserve current-scene progress.");
    }

    private static void NewSceneSelectsOwnItems()
    {
        CollectManager first = Manager();
        SetField(first, "numberOfItemsToActivate", 1);
        CollectableItem[] oldItems = Enumerable.Range(0, 3).Select(index => Create<CollectableItem>()).ToArray();
        Call(first, "Start");
        Equal(1, oldItems.Count(item => item.gameObject.activeSelf), "Initial scene selection must still work.");
        first.CollectItem();
        StubRuntime.UnloadScene();
        CollectManager next = Manager();
        SetField(next, "numberOfItemsToActivate", 2);
        CollectableItem[] nextItems = Enumerable.Range(0, 4).Select(index => Create<CollectableItem>()).ToArray();
        Call(next, "Start");
        Equal(2, nextItems.Count(item => item.gameObject.activeSelf), "New scene must run its own item selection.");
        Equal(0, next.GetTotalCollectedItems(), "New scene item selection must accompany a fresh counter.");
    }
}
