using System.Collections.Generic;
using Script.Display;
using Script.Quest;
using Script.Quest.Entity;
using UnityEngine;
using static ComponentTestSupport;

public static class QuestLifecycleTests
{
    public static void Run()
    {
        Case("Observer starts before B.Start", () => CompleteRobotQuest(false, true));
        Case("B.Start runs before observer", () => CompleteRobotQuest(true, true));
        Case("Initially inactive B receives the earlier tie", () => CompleteRobotQuest(false, false));
        Case("Repeated preparation keeps a single listener", PreparationIsIdempotent);
        Case("Outside drop can be chained and delivered again", OutsideDropAndRechain);
        Case("Destroying B detaches its initial robot", DestroyBeforeTie);
        Case("Destroying B detaches its chained robot", DestroyWhileChained);
        Case("Destroying B detaches its replacement robot", DestroyAfterOutsideDrop);
    }

    private sealed class RobotQuest
    {
        internal readonly Robot Original = Create<Robot>();
        internal readonly Quest3A A = Create<Quest3A>();
        internal readonly Quest3B B = Create<Quest3B>();
        internal readonly QuestObserver Observer = Create<QuestObserver>();
        internal readonly DisplayManager Display = Create<DisplayManager>();
        internal readonly Collider Player = Create<Collider>();

        internal RobotQuest(bool startBFirst, bool initiallyActive)
        {
            A.robot = Original;
            B.robot = Original;
            B.gameObject.SetActive(initiallyActive);
            Player.gameObject.tag = "Player";
            SetField(Observer, "quests", new List<Quest> { A, B });
            SetField(Observer, "arrowIndicator", Create<QuestArrowIndicator>());
            if (startBFirst) Call(B, "Start");
            Call(Observer, "Start");
            Call(A, "Start");
            Assert(A.gameObject.activeSelf && !B.gameObject.activeSelf,
                "Observer must keep only the first quest active.");
        }

        internal ChainedRobot TieAndAdvance(bool bAlreadyStarted = false)
        {
            Equal(1, Listeners(Original.OnChainTied, B),
                "Observer must prepare B before its deferred Start and before robot destruction.");
            ChainedRobot chained = Original.Tie();
            Equal(0, Listeners(Original.OnChainTied, B), "The old robot must be detached during its tie callback.");
            StubRuntime.FlushDestroyed();
            Assert(!Original, "Original robot must be gone before B.Start is allowed to run.");
            Equal(1, Listeners(chained.OnChainUntied, B), "Inactive B must retain the new chained robot.");
            Call(A, "Update");
            Assert(!A.gameObject.activeSelf && B.gameObject.activeSelf, "Completing A must activate B.");
            if (!bAlreadyStarted) Call(B, "Start");
            Equal(1, Listeners(chained.OnChainUntied, B), "Delayed Start must preserve the earlier tie.");
            Equal(1, Display.QuestLevel.CurrentLevel, "Only A should be completed so far.");
            return chained;
        }
    }

    private static void CompleteRobotQuest(bool startBFirst, bool initiallyActive)
    {
        var fixture = new RobotQuest(startBFirst, initiallyActive);
        ChainedRobot chained = fixture.TieAndAdvance(startBFirst);
        BoxCollider trigger = fixture.B.GetComponent<BoxCollider>();
        Assert(trigger != null && trigger.isTrigger, "B.Start must still configure its trigger collider.");
        Call(fixture.B, "OnTriggerEnter", fixture.Player);
        chained.Untie();
        StubRuntime.FlushDestroyed();
        Equal(0, Listeners(chained.OnChainUntied, fixture.B), "Untie must detach the retired chain.");
        Call(fixture.B, "Update");
        Assert(!fixture.B.gameObject.activeSelf, "Delivery inside the zone must complete B.");
        Equal(2, fixture.Display.QuestLevel.CurrentLevel, "Both actual quests must advance the observer.");
    }

    private static void PreparationIsIdempotent()
    {
        Robot original = Create<Robot>();
        Quest3B quest = Create<Quest3B>();
        quest.robot = original;
        Call(quest, "Prepare");
        Call(quest, "Prepare");
        Equal(1, Listeners(original.OnChainTied, quest), "Preparing twice must subscribe once.");
        ChainedRobot chained = original.Tie();
        StubRuntime.FlushDestroyed();
        Call(quest, "Prepare");
        Call(quest, "Start");
        Call(quest, "Prepare");
        Equal(0, Listeners(original.OnChainTied, quest), "Start must not subscribe to the destroyed original.");
        Equal(1, Listeners(chained.OnChainUntied, quest), "Preparation must preserve one current chain listener.");
    }

    private static void OutsideDropAndRechain()
    {
        var fixture = new RobotQuest(false, false);
        ChainedRobot firstChain = fixture.TieAndAdvance();
        Call(fixture.B, "OnTriggerEnter", fixture.Player);
        Call(fixture.B, "OnTriggerExit", fixture.Player);
        Robot replacement = firstChain.Untie();
        StubRuntime.FlushDestroyed();
        Call(fixture.B, "Update");
        Equal(1, fixture.Display.QuestLevel.CurrentLevel, "Dropping after leaving the zone cannot complete B.");
        Assert(fixture.B.gameObject.activeSelf, "B must remain available for another delivery attempt.");
        Equal(0, Listeners(firstChain.OnChainUntied, fixture.B), "Outside drop must detach its old chain.");
        Equal(1, Listeners(replacement.OnChainTied, fixture.B), "The replacement robot must receive one listener.");
        Call(fixture.B, "Prepare");
        Call(fixture.B, "Prepare");
        Equal(1, Listeners(replacement.OnChainTied, fixture.B), "Preparing again must keep the replacement subscription.");
        ChainedRobot secondChain = replacement.Tie();
        StubRuntime.FlushDestroyed();
        Equal(0, Listeners(replacement.OnChainTied, fixture.B), "Rechain must detach the replaced robot.");
        Equal(1, Listeners(secondChain.OnChainUntied, fixture.B), "Rechain must observe the new chain once.");
        Call(fixture.B, "OnTriggerEnter", fixture.Player);
        secondChain.Untie();
        StubRuntime.FlushDestroyed();
        Call(fixture.B, "Update");
        Equal(2, fixture.Display.QuestLevel.CurrentLevel, "A later delivery must complete normally.");
    }

    private static Quest3B PreparedQuest(out Robot robot)
    {
        robot = Create<Robot>();
        Quest3B quest = Create<Quest3B>();
        quest.robot = robot;
        Call(quest, "Prepare");
        return quest;
    }

    private static void DestroyBeforeTie()
    {
        Quest3B quest = PreparedQuest(out Robot robot);
        Object.Destroy(quest.gameObject);
        StubRuntime.FlushDestroyed();
        Equal(0, Listeners(robot.OnChainTied, quest), "Destroy must detach the initial robot listener.");
    }

    private static void DestroyWhileChained()
    {
        Quest3B quest = PreparedQuest(out Robot robot);
        ChainedRobot chained = robot.Tie();
        StubRuntime.FlushDestroyed();
        Object.Destroy(quest.gameObject);
        StubRuntime.FlushDestroyed();
        Equal(0, Listeners(chained.OnChainUntied, quest), "Destroy must detach the current chained robot listener.");
    }

    private static void DestroyAfterOutsideDrop()
    {
        Quest3B quest = PreparedQuest(out Robot robot);
        ChainedRobot chained = robot.Tie();
        StubRuntime.FlushDestroyed();
        Robot replacement = chained.Untie();
        StubRuntime.FlushDestroyed();
        Object.Destroy(quest.gameObject);
        StubRuntime.FlushDestroyed();
        Equal(0, Listeners(replacement.OnChainTied, quest), "Destroy must detach the replacement robot listener.");
    }
}
