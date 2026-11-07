using System;
using Crest;
using Script.Display;
using Script.Scene;
using UnityEngine;
using static ComponentTestSupport;

public static class WaterDepthConsumptionTests
{
    public static void Run()
    {
        Case("Surfacing through shallow water stops actual oxygen consumption", SurfacingStopsConsumption);
        Case("Initially shallow water still consumes oxygen", ShallowInitialDepthIsPublished);
        Case("Every subthreshold and unchanged depth is published", SubthresholdChangesRemainCurrent);
        Case("Missing ocean leaves the last display value untouched", MissingOceanIsIgnored);
    }

    private sealed class Dive
    {
        internal readonly DisplayManager Display = Create<DisplayManager>();
        internal readonly WaterDepthManager Depth = Create<WaterDepthManager>();
        internal readonly OxygenManager Oxygen = Create<OxygenManager>();
        internal readonly OceanRenderer Ocean = Create<OceanRenderer>();

        internal Dive()
        {
            Create<SceneLoader>();
            Call(Depth, "Start");
            Call(Oxygen, "Start");
        }

        internal void SetDepth(float depth)
        {
            Ocean.ViewerHeightAboveWater = -depth;
            Call(Depth, "Update");
        }

        internal void Consume(float seconds)
        {
            Time.deltaTime = seconds;
            Call(Oxygen, "Update");
        }
    }

    private static void SurfacingStopsConsumption()
    {
        var dive = new Dive();
        dive.SetDepth(0.16f);
        Near(0.16f, dive.Display.WaterDepth);
        dive.Consume(10f);
        Near(99.9f, dive.Display.OxygenRate);
        dive.SetDepth(0.05f);
        Near(0.05f, dive.Display.WaterDepth);
        dive.Consume(10f);
        Near(99.8f, dive.Display.OxygenRate);
        dive.SetDepth(0f);
        Near(0f, dive.Display.WaterDepth);
        dive.Consume(1000f);
        Near(99.8f, dive.Display.OxygenRate);
        dive.SetDepth(-0.3f);
        Near(0f, dive.Display.WaterDepth);
        dive.Consume(1000f);
        Near(99.8f, dive.Display.OxygenRate);
    }

    private static void ShallowInitialDepthIsPublished()
    {
        var dive = new Dive();
        dive.SetDepth(0.05f);
        Near(0.05f, dive.Display.WaterDepth);
        dive.Consume(10f);
        Near(99.9f, dive.Display.OxygenRate);
    }

    private static void SubthresholdChangesRemainCurrent()
    {
        var dive = new Dive();
        foreach (float depth in new[] { 0.16f, 0.15f, 0.12f, 0.09f, 0.05f, 0.01f, 0.01f, 0f })
        {
            dive.SetDepth(depth);
            Near(depth, dive.Display.WaterDepth);
        }
    }

    private static void MissingOceanIsIgnored()
    {
        DisplayManager display = Create<DisplayManager>();
        display.WaterDepth = 1.25f;
        WaterDepthManager depth = Create<WaterDepthManager>();
        Call(depth, "Start");
        Call(depth, "Update");
        Near(1.25f, display.WaterDepth);
    }

    private static void Near(float expected, float actual)
    {
        if (float.IsNaN(actual) || float.IsInfinity(actual) || Math.Abs(expected - actual) > 0.00001f)
            throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
    }
}
