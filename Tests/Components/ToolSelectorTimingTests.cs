using Script.Equipment;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using static ComponentTestSupport;

public static class ToolSelectorTimingTests
{
    public static void Run()
    {
        Case("A successful selection grants a fresh five seconds on reopen", SelectionResetsNextOpening);
        Case("Repeated successful choices cannot accumulate menu idle time", ChoicesDoNotAccumulateTime);
        Case("Timeout and toggle cancellation still close and reset the menu", CancellationStillWorks);
    }

    private sealed class Menu
    {
        internal readonly ToolSelector Selector = Create<ToolSelector>();
        internal readonly Equipment Equipment = Create<Equipment>();
        internal readonly LocomotionSystem Locomotion = Create<LocomotionSystem>();
        internal readonly Canvas Canvas = Create<Canvas>();
        internal readonly Tool[] Tools = { new Tool(), new Tool(), new Tool(), new Tool() };
        private readonly InputActionReference toggle = new InputActionReference();
        private readonly InputActionReference axis = new InputActionReference();

        internal Menu()
        {
            Equipment.Equip(Tools[0]);
            SetField(Selector, "equipment", Equipment);
            SetField(Selector, "locomotion", Locomotion);
            SetField(Selector, "canvas", Canvas);
            SetField(Selector, "tools", Tools);
            SetField(Selector, "toggleButton", toggle);
            SetField(Selector, "thumbstickAxis", axis);
            SetField(Selector, "buttonImages", new[] { Create<Image>(), Create<Image>(), Create<Image>(), Create<Image>() });
            SetField(Selector, "idleTime", 5f);
            Call(Selector, "Start");
            Tick(0f);
        }

        internal void Tick(float seconds, bool pressToggle = false, float x = 0f, float y = 0f)
        {
            Time.deltaTime = seconds;
            toggle.action.PressedThisFrame = pressToggle;
            axis.action.Axis = new Vector2(x, y);
            Call(Selector, "Update");
        }

        internal void Open()
        {
            Tick(0f, pressToggle: true);
            Assert(Canvas.enabled, "Toggle must open the selection menu.");
            Assert(Locomotion.busy, "An open menu must reserve locomotion.");
        }

        internal void AssertClosed()
        {
            Assert(!Canvas.enabled, "The menu must close.");
            Assert(!Locomotion.busy, "Closing the menu must release locomotion.");
        }
    }

    private static void SelectionResetsNextOpening()
    {
        var menu = new Menu();
        menu.Open();
        menu.Tick(5f, x: 1f);
        menu.Tick(0f);
        menu.AssertClosed();
        Assert(ReferenceEquals(menu.Tools[1], menu.Equipment.GetActiveTool()), "The selected tool must be equipped.");
        Equal(2, menu.Equipment.EquipCount, "Returning the stick to neutral must equip once.");
        menu.Open();
        menu.Tick(0.1f);
        Assert(menu.Canvas.enabled, "A previous successful selection cannot immediately time out the next opening.");
        menu.Tick(4.8f);
        Assert(menu.Canvas.enabled, "The new opening must have its own full five-second idle window.");
        menu.Tick(0.2f);
        menu.AssertClosed();
        Equal(2, menu.Locomotion.FinishCount, "Selection and timeout must each release their operation.");
    }

    private static void ChoicesDoNotAccumulateTime()
    {
        var menu = new Menu();
        for (int choice = 0; choice < 3; choice++)
        {
            menu.Open();
            menu.Tick(2f);
            Assert(menu.Canvas.enabled, "Earlier choices must not shorten the next menu's idle window.");
            menu.Tick(0.25f, x: 1f);
            menu.Tick(0.25f);
            menu.AssertClosed();
            Equal(choice + 2, menu.Equipment.EquipCount, "Each choice should equip exactly one tool.");
        }
        menu.Open();
        menu.Tick(4.9f);
        Assert(menu.Canvas.enabled, "Several earlier choices cannot consume this opening's timeout.");
    }

    private static void CancellationStillWorks()
    {
        var menu = new Menu();
        menu.Open();
        menu.Tick(5.1f);
        menu.AssertClosed();
        Equal(1, menu.Equipment.EquipCount, "An idle timeout must not equip another tool.");
        menu.Open();
        menu.Tick(4f);
        menu.Tick(0.1f, pressToggle: true);
        menu.AssertClosed();
        Equal(1, menu.Equipment.EquipCount, "Toggle cancellation must not equip another tool.");
        menu.Open();
        menu.Tick(4.9f);
        Assert(menu.Canvas.enabled, "A canceled opening must also reset idle time.");
        menu.Tick(0.2f);
        menu.AssertClosed();
        Equal(3, menu.Locomotion.FinishCount, "Every canceled opening must release locomotion once.");
    }
}
