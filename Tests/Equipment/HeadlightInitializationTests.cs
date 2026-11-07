using System;
using System.Linq;
using System.Reflection;
using Script.Equipment;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Object = UnityEngine.Object;

public static class HeadlightInitializationTests
{
    public static void Run()
    {
        Case("Equipment before headlight starts with Hand and light off", () => DefaultHand(equipmentFirst: true));
        Case("Headlight before equipment starts with Hand and light off", () => DefaultHand(equipmentFirst: false));
        Case("Equipment before headlight preserves a selected headlight", () => DefaultHeadlight(equipmentFirst: true));
        Case("Headlight before equipment preserves a selected headlight", () => DefaultHeadlight(equipmentFirst: false));
        Case("An empty active tool is dark and later equipping restores intensity", NoDefaultTool);
        Case("A missing configured headlight tool stays dark", MissingConfiguredTool);
        Case("A missing equipment system safely disables the headlight", MissingEquipment);
        Case("Destroying the headlight removes its equipment listener", DestroyUnsubscribes);
    }

    private sealed class Setup
    {
        internal readonly Tool Hand = ScriptableObject.CreateInstance<Tool>();
        internal readonly Tool Lamp = ScriptableObject.CreateInstance<Tool>();
        internal readonly Equipment Equipment = new Equipment();
        internal readonly Light Light = new Light { intensity = 10f };
        internal readonly Headlight Headlight = new Headlight();

        internal Setup()
        {
            SetField(Hand, "id", "Hand");
            SetField(Lamp, "id", "Headlight");
            SetField(Equipment, "rightController", new ActionBasedController());
            Headlight.AttachedComponent = Light;
            Headlight.tool = Lamp;
        }

        internal void Start(Tool defaultTool, bool equipmentFirst)
        {
            SetField(Equipment, "defaultTool", defaultTool);
            if (equipmentFirst) { Call(Equipment, "Start"); Call(Headlight, "Start"); }
            else { Call(Headlight, "Start"); Call(Equipment, "Start"); }
        }

        internal void AssertTransitions(float intensity)
        {
            Equipment.Equip(Lamp);
            Equal(intensity, Light.intensity, "Equipping the headlight must restore its original intensity.");
            Equipment.Equip(Hand);
            Equal(0f, Light.intensity, "Equipping Hand must switch the light off.");
            Equipment.Equip(Lamp);
            Equal(intensity, Light.intensity, "Repeated switches cannot overwrite the original intensity with zero.");
        }
    }

    private static void DefaultHand(bool equipmentFirst)
    {
        var setup = new Setup();
        setup.Start(setup.Hand, equipmentFirst);
        Assert(ReferenceEquals(setup.Hand, setup.Equipment.GetActiveTool()), "The real Equipment must equip its default Hand.");
        Equal(0f, setup.Light.intensity, "Startup order cannot leave the headlight on with Hand selected.");
        setup.AssertTransitions(10f);
    }

    private static void DefaultHeadlight(bool equipmentFirst)
    {
        var setup = new Setup();
        setup.Light.intensity = 3.25f;
        setup.Start(setup.Lamp, equipmentFirst);
        Equal(3.25f, setup.Light.intensity, "An initially selected headlight must retain the configured intensity.");
        setup.AssertTransitions(3.25f);
    }

    private static void NoDefaultTool()
    {
        var setup = new Setup();
        setup.Start(null, equipmentFirst: false);
        Assert(setup.Equipment.GetActiveTool() == null, "This fixture must begin without any equipped tool.");
        Equal(0f, setup.Light.intensity, "No equipped tool must leave the headlight off without throwing.");
        setup.AssertTransitions(10f);
        setup.Equipment.OnEquip?.Invoke(null);
        Equal(0f, setup.Light.intensity, "A null equipment notification must also switch the light off.");
        setup.AssertTransitions(10f);
    }

    private static void MissingConfiguredTool()
    {
        var setup = new Setup();
        setup.Headlight.tool = null;
        setup.Start(setup.Hand, equipmentFirst: true);
        Equal(0f, setup.Light.intensity, "An unassigned headlight tool cannot illuminate the scene.");
        setup.Equipment.Equip(setup.Lamp);
        Equal(0f, setup.Light.intensity, "Other tool selections cannot match a missing headlight tool.");
    }

    private static void MissingEquipment()
    {
        var light = new Light { intensity = 10f };
        var headlight = new Headlight { tool = ScriptableObject.CreateInstance<Tool>(), AttachedComponent = light };
        Call(headlight, "Start");
        Equal(0f, light.intensity, "A missing equipment system must fail with the light off.");
        Assert(!headlight.enabled, "The component cannot keep running without equipment.");
        Object.Destroy(headlight);
    }

    private static void DestroyUnsubscribes()
    {
        var setup = new Setup();
        setup.Start(setup.Hand, equipmentFirst: true);
        Equal(1, ListenerCount(setup), "Startup must subscribe to the real equipment exactly once.");
        Object.Destroy(setup.Headlight);
        Equal(0, ListenerCount(setup), "A destroyed headlight cannot remain in equipment callbacks.");
        setup.Equipment.Equip(setup.Lamp);
        Equal(0f, setup.Light.intensity, "An unsubscribed headlight cannot react to later tool changes.");
    }

    private static int ListenerCount(Setup setup)
    {
        return setup.Equipment.OnEquip == null ? 0 : setup.Equipment.OnEquip.GetInvocationList()
            .Count(listener => ReferenceEquals(listener.Target, setup.Headlight));
    }

    private static void Case(string name, Action body)
    {
        Object.ResetObjects();
        try { body(); Console.WriteLine("  PASS " + name); }
        finally { Object.ResetObjects(); }
    }

    private static void Call(object target, string name)
    {
        MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null) throw new InvalidOperationException("Missing lifecycle method: " + name);
        method.Invoke(target, null);
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) throw new InvalidOperationException("Missing serialized field: " + name);
        field.SetValue(target, value);
    }

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual)) throw new InvalidOperationException(message + " Expected " + expected + ", got " + actual + ".");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
