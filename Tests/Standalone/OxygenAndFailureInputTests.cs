using System;
using Script.Display;
using Script.UI;

public static class OxygenAndFailureInputTests
{
    public static void Run()
    {
        FreshSupplyAndPartialInterval();
        LongFramesPreserveElapsedConsumption();
        SurfaceTimeDoesNotConsume();
        DepletionIsExactAndReportedOnce();
        RefillResetsSupplyAndTimer();
        InvalidTimeCannotPoisonTheSupply();
        HeldButtonCannotDismissFailure();
        FreshRestartPressSelectsOnlyOnce();
        TitleAndMouseSelectionWork();
        AmbiguousInputAndRetryAreSafe();
    }

    private static void FreshSupplyAndPartialInterval()
    {
        var oxygen = new OxygenSupply();
        Equal(100f, oxygen.Percent);
        False(oxygen.Advance(2f, true, 3f));
        Equal(100f, oxygen.Percent);
        False(oxygen.Advance(1f, true, 3f));
        Equal(99.9f, oxygen.Percent);
    }

    private static void LongFramesPreserveElapsedConsumption()
    {
        var oxygen = new OxygenSupply();
        oxygen.Advance(10f, true, 3f);
        Equal(99.7f, oxygen.Percent);
        oxygen.Advance(2f, true, 3f);
        Equal(99.6f, oxygen.Percent);
    }

    private static void SurfaceTimeDoesNotConsume()
    {
        var oxygen = new OxygenSupply();
        oxygen.Advance(2f, true, 3f);
        oxygen.Advance(5000f, false, 3f);
        Equal(100f, oxygen.Percent);
        oxygen.Advance(1f, true, 3f);
        Equal(99.9f, oxygen.Percent);
    }

    private static void DepletionIsExactAndReportedOnce()
    {
        var oxygen = new OxygenSupply();
        False(oxygen.Advance(2999f, true, 3f));
        Equal(0.1f, oxygen.Percent);
        True(oxygen.Advance(1f, true, 3f));
        True(oxygen.IsDepleted);
        Equal(0f, oxygen.Percent);
        False(oxygen.Advance(1000f, true, 3f));
        Equal(0f, oxygen.Percent);
    }

    private static void RefillResetsSupplyAndTimer()
    {
        var oxygen = new OxygenSupply();
        oxygen.Advance(3002f, true, 3f);
        oxygen.Refill();
        False(oxygen.IsDepleted);
        Equal(100f, oxygen.Percent);
        oxygen.Advance(1f, true, 3f);
        Equal(100f, oxygen.Percent);
    }

    private static void InvalidTimeCannotPoisonTheSupply()
    {
        var oxygen = new OxygenSupply();
        oxygen.Advance(float.NaN, true, 3f);
        oxygen.Advance(float.PositiveInfinity, true, 3f);
        oxygen.Advance(-1f, true, 3f);
        Equal(100f, oxygen.Percent);
        oxygen.Advance(1f, true, 0f);
        Equal(99.9f, oxygen.Percent);
        True(oxygen.Advance(float.MaxValue, true, 1f));
        Equal(0f, oxygen.Percent);
    }

    private static void HeldButtonCannotDismissFailure()
    {
        var input = new FailureMenuInput();
        Equal(FailureMenuChoice.None, input.Advance(0.8f, true, false));
        False(input.CanChoose);
        Equal(FailureMenuChoice.None, input.Advance(0.1f, false, false));
        True(input.CanChoose);
    }

    private static void FreshRestartPressSelectsOnlyOnce()
    {
        var input = new FailureMenuInput();
        Equal(FailureMenuChoice.None, input.Advance(0.2f, false, false));
        False(input.CanChoose);
        Equal(FailureMenuChoice.None, input.Advance(0.3f, false, false));
        Equal(FailureMenuChoice.Restart, input.Advance(0f, true, false));
        Equal(FailureMenuChoice.None, input.Advance(1f, false, true));
        Equal(FailureMenuChoice.None, input.Choose(FailureMenuChoice.Title));
    }

    private static void TitleAndMouseSelectionWork()
    {
        var input = new FailureMenuInput();
        input.Advance(0.5f, false, false);
        Equal(FailureMenuChoice.Title, input.Advance(0f, false, true));
        var mouse = new FailureMenuInput();
        Equal(FailureMenuChoice.None, mouse.Choose(FailureMenuChoice.Restart));
        mouse.Advance(0.5f, false, false);
        Equal(FailureMenuChoice.Restart, mouse.Choose(FailureMenuChoice.Restart));
    }

    private static void AmbiguousInputAndRetryAreSafe()
    {
        var input = new FailureMenuInput();
        input.Advance(0.5f, false, false);
        Equal(FailureMenuChoice.None, input.Advance(0f, true, true));
        Equal(FailureMenuChoice.None, input.Advance(0f, false, true));
        input.Advance(0f, false, false);
        input.Choose(FailureMenuChoice.Title);
        input.RetrySelection();
        False(input.CanChoose);
        Equal(FailureMenuChoice.None, input.Advance(float.NaN, false, false));
        input.Advance(0.5f, false, false);
        Equal(FailureMenuChoice.Restart, input.Choose(FailureMenuChoice.Restart));
    }

    private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
    private static void False(bool value) { if (value) throw new Exception("Expected false."); }
    private static void Equal<T>(T expected, T actual)
    {
        if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual);
    }
}
