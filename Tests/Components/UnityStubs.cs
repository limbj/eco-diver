using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// This harness supplies engine and collaborator boundaries only. Component behavior
// comes from the production sources compiled by run-checks.ps1. Lifecycle calls are explicit.
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string text) { }
    }

    public class Object
    {
        internal static readonly List<Object> Objects = new List<Object>();
        internal bool Destroyed;
        internal bool PendingDestroy;
        internal bool Persistent;
        public HideFlags hideFlags { get; set; }

        public Object() { Objects.Add(this); }

        public static implicit operator bool(Object value)
        {
            return !ReferenceEquals(value, null) && !value.Destroyed;
        }

        public static bool operator ==(Object left, Object right)
        {
            bool leftMissing = ReferenceEquals(left, null) || left.Destroyed;
            bool rightMissing = ReferenceEquals(right, null) || right.Destroyed;
            return leftMissing || rightMissing
                ? leftMissing && rightMissing
                : ReferenceEquals(left, right);
        }

        public static bool operator !=(Object left, Object right) { return !(left == right); }
        public override bool Equals(object other) { return this == other as Object; }
        public override int GetHashCode() { return base.GetHashCode(); }

        public static T FindObjectOfType<T>() where T : Object
        {
            return FindObjectsOfType<T>().FirstOrDefault();
        }

        public static T[] FindObjectsOfType<T>() where T : Object
        {
            return Objects.OfType<T>().Where(value => value
                && (!(value is Component component) || component.gameObject.activeSelf)).ToArray();
        }

        public static void Destroy(Object value)
        {
            if (value) value.PendingDestroy = true;
        }

        public static void DontDestroyOnLoad(Object value)
        {
            StubRuntime.DontDestroyCalls++;
            if (value) value.Persistent = true;
        }
    }

    public class GameObject : Object
    {
        private readonly List<Component> components = new List<Component>();
        public bool activeSelf = true;
        public string tag = "Untagged";
        public Transform transform { get; private set; }

        public GameObject()
        {
            transform = AddComponent<Transform>();
        }

        public T AddComponent<T>() where T : Component, new()
        {
            var component = new T();
            component.Owner = this;
            components.Add(component);
            return component;
        }

        public T GetComponent<T>() where T : Component
        {
            return components.OfType<T>().FirstOrDefault(component => component);
        }

        public void SetActive(bool active) { activeSelf = active; }

        internal void DestroyComponents()
        {
            foreach (Component component in components.ToArray()) StubRuntime.DestroyNow(component);
        }
    }

    public class Component : Object
    {
        internal GameObject Owner;
        public GameObject gameObject { get { return Owner; } }
        public Transform transform { get { return Owner.transform; } }
        public T GetComponent<T>() where T : Component { return Owner.GetComponent<T>(); }
        public bool TryGetComponent<T>(out T component) where T : Component
        {
            component = GetComponent<T>();
            return component;
        }
        public bool CompareTag(string tag) { return Owner.tag == tag; }
    }

    public class MonoBehaviour : Component
    {
        public bool enabled = true;
        private readonly List<IEnumerator> coroutines = new List<IEnumerator>();
        public Coroutine StartCoroutine(IEnumerator routine)
        {
            coroutines.Add(routine);
            routine.MoveNext();
            return new Coroutine();
        }
        public void StopAllCoroutines() { coroutines.Clear(); }
    }

    public sealed class Coroutine { }
    public enum HideFlags { HideAndDontSave }
    public sealed class WaitForSeconds { public WaitForSeconds(float seconds) { } }
    public class Transform : Component { }
    public class Collider : Component { }
    public sealed class BoxCollider : Collider { public bool isTrigger; }
    public sealed class AudioClip : Object { }
    public sealed class AudioSource : Component { public void PlayOneShot(AudioClip clip) { } }
    public sealed class Camera : Component { public static Camera main { get { return null; } } }
    public sealed class Canvas : MonoBehaviour { }
    public static class Time { public static float deltaTime; }
    public struct Vector2
    {
        public readonly float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 up { get { return new Vector2(0f, 1f); } }
        public float sqrMagnitude { get { return x * x + y * y; } }
        public static float SignedAngle(Vector2 from, Vector2 to)
        {
            return (float)(Math.Atan2(from.x * to.y - from.y * to.x, from.x * to.x + from.y * to.y)
                * 180d / Math.PI);
        }
    }
    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
    }
    public static class Mathf
    {
        public static int Min(int first, int second) { return Math.Min(first, second); }
        public static float Abs(float value) { return Math.Abs(value); }
    }
    public static class Random
    {
        private static readonly System.Random Generator = new System.Random(11);
        public static int Range(int minimum, int maximum) { return Generator.Next(minimum, maximum); }
    }

    internal static class StubRuntime
    {
        internal static int DontDestroyCalls;

        internal static void FlushDestroyed()
        {
            foreach (Object value in Object.Objects.Where(value => value.PendingDestroy && value).ToArray())
                DestroyNow(value);
        }

        internal static void DestroyNow(Object value)
        {
            if (!value) return;
            if (value is GameObject gameObject) gameObject.DestroyComponents();
            MethodInfo destroy = ComponentTestSupport.FindMethod(value.GetType(), "OnDestroy");
            if (destroy != null) destroy.Invoke(value, null);
            value.Destroyed = true;
        }

        internal static void UnloadScene()
        {
            foreach (GameObject value in Object.Objects.OfType<GameObject>()
                .Where(value => value && !value.Persistent).ToArray()) Object.Destroy(value);
            FlushDestroyed();
        }

        internal static void ClearAll()
        {
            foreach (GameObject value in Object.Objects.OfType<GameObject>().Where(value => value).ToArray())
                DestroyNow(value);
            Object.Objects.Clear();
            DontDestroyCalls = 0;
            Time.deltaTime = 0f;
        }
    }
}

namespace Script.Quest.Entity
{
    public sealed class Robot : UnityEngine.MonoBehaviour
    {
        public Action<ChainedRobot> OnChainTied;
        public ChainedRobot Tie()
        {
            var chained = new UnityEngine.GameObject().AddComponent<ChainedRobot>();
            OnChainTied?.Invoke(chained);
            UnityEngine.Object.Destroy(gameObject);
            return chained;
        }
    }

    public sealed class ChainedRobot : UnityEngine.MonoBehaviour
    {
        public Action<Robot> OnChainUntied;
        public Robot Untie()
        {
            var robot = new UnityEngine.GameObject().AddComponent<Robot>();
            OnChainUntied?.Invoke(robot);
            UnityEngine.Object.Destroy(gameObject);
            return robot;
        }
    }
}

namespace Script.Quest
{
    public sealed class QuestArrowIndicator : UnityEngine.MonoBehaviour
    {
        public UnityEngine.Transform questTransform;
    }
}

namespace Script.Display
{
    public sealed class DisplayManager : UnityEngine.MonoBehaviour
    {
        public static DisplayManager Instance { get { return FindObjectOfType<DisplayManager>(); } }
        public Script.Quest.QuestLevel QuestLevel { get; set; }
        public string QuestName { get; set; }
        public string QuestDescription { get; set; }
        public float WaterDepth { get; set; }
        public float OxygenRate { get; set; }
    }
}

namespace Script.Scene
{
    public sealed class SceneLoader : UnityEngine.MonoBehaviour
    {
        public static SceneLoader Instance { get { return FindObjectOfType<SceneLoader>(); } }
        public bool IsLoadingScene { get; set; }
        public void LoadNextScene() { }
    }
}

namespace Script.Environment.Collect
{
    public sealed class CollectableItem : UnityEngine.MonoBehaviour { }
}

namespace Crest
{
    public sealed class OceanRenderer : UnityEngine.MonoBehaviour
    {
        public static OceanRenderer Instance { get { return FindObjectOfType<OceanRenderer>(); } }
        public float ViewerHeightAboveWater { get; set; }
    }
}

namespace Script.UI
{
    public sealed class DiveFailureController : UnityEngine.MonoBehaviour
    {
        public bool IsFailed { get; private set; }
        public void Show() { IsFailed = true; }
    }
}

namespace UnityEngine.InputSystem
{
    public sealed class InputActionReference
    {
        public readonly InputAction action = new InputAction();
    }

    public sealed class InputAction
    {
        public bool PressedThisFrame { get; set; }
        public UnityEngine.Vector2 Axis { get; set; }
        public bool WasPressedThisFrame() { return PressedThisFrame; }
        public T ReadValue<T>() { return (T)(object)Axis; }
    }
}

namespace UnityEngine.UI
{
    public sealed class Image : UnityEngine.MonoBehaviour { }
}

namespace UnityEngine.XR
{
    public struct InputDevice { }
}

namespace UnityEngine.XR.Interaction.Toolkit
{
    public enum RequestResult { Success, Busy }
    public class LocomotionProvider : UnityEngine.MonoBehaviour { }

    public sealed class LocomotionSystem : UnityEngine.MonoBehaviour
    {
        private LocomotionProvider owner;
        public bool busy { get { return owner; } }
        public int FinishCount { get; private set; }
        public RequestResult RequestExclusiveOperation(LocomotionProvider requester)
        {
            if (owner) return RequestResult.Busy;
            owner = requester;
            return RequestResult.Success;
        }
        public void FinishExclusiveOperation(LocomotionProvider requester)
        {
            if (owner == requester)
            {
                owner = null;
                FinishCount++;
            }
        }
    }
}

namespace Script.Equipment
{
    public sealed class Tool : UnityEngine.Object { }

    public sealed class Equipment : UnityEngine.MonoBehaviour
    {
        private Tool active;
        public int EquipCount { get; private set; }
        public Tool GetActiveTool() { return active; }
        public void Equip(Tool tool) { active = tool; EquipCount++; }
    }
}
