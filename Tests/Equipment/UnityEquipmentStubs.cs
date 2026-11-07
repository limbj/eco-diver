using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// The standalone compiler uses .NET Framework; Unity's Tool also references HashCode.
// Hash values are not part of the equipment/lighting behavior under test.
namespace System
{
    public static class HashCode
    {
        public static int Combine<TFirst, TSecond>(TFirst first, TSecond second)
        {
            unchecked
            {
                return EqualityComparer<TFirst>.Default.GetHashCode(first) * 397
                    ^ EqualityComparer<TSecond>.Default.GetHashCode(second);
            }
        }
    }
}

// Engine boundaries only. Headlight, Equipment and Tool are the production files.
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RequireComponentAttribute : Attribute
    {
        public RequireComponentAttribute(Type type) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string fileName;
        public string menuName;
        public int order;
    }

    public class Object
    {
        private static readonly List<Object> Registry = new List<Object>();
        private bool destroyed;
        public Object() { Registry.Add(this); }

        public static implicit operator bool(Object value)
        {
            return !ReferenceEquals(value, null) && !value.destroyed;
        }

        public static bool operator ==(Object first, Object second)
        {
            bool firstMissing = ReferenceEquals(first, null) || first.destroyed;
            bool secondMissing = ReferenceEquals(second, null) || second.destroyed;
            return firstMissing || secondMissing ? firstMissing && secondMissing : ReferenceEquals(first, second);
        }

        public static bool operator !=(Object first, Object second) { return !(first == second); }
        public override bool Equals(object value) { return this == value as Object; }
        public override int GetHashCode() { return base.GetHashCode(); }

        public static T FindObjectOfType<T>() where T : Object
        {
            return Registry.OfType<T>().FirstOrDefault(value => value);
        }

        public static GameObject Instantiate(GameObject prefab, Transform parent)
        {
            var clone = new GameObject();
            clone.transform.parent = parent;
            return clone;
        }

        public static void Destroy(Object target)
        {
            if (!target) return;
            MethodInfo callback = target.GetType().GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic);
            if (callback != null) callback.Invoke(target, null);
            target.destroyed = true;
        }

        internal static void ResetObjects()
        {
            foreach (Object value in Registry.ToArray()) Destroy(value);
            Registry.Clear();
        }
    }

    public class MonoBehaviour : Object
    {
        public Object AttachedComponent { get; set; }
        public bool enabled = true;
        public T GetComponent<T>() where T : Object { return AttachedComponent as T; }
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() { return new T(); }
    }

    public sealed class GameObject : Object
    {
        public Transform transform { get; private set; }
        public GameObject() { transform = new Transform { gameObject = this }; }
        public void SetActive(bool active) { }
    }

    public sealed class Transform : Object
    {
        public GameObject gameObject { get; set; }
        public Transform parent { get; set; }
    }

    public sealed class Light : Object { public float intensity { get; set; } }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogError(object message) { }
    }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction<in T>(T value);
}

namespace UnityEngine.XR.Interaction.Toolkit
{
    public sealed class ActionBasedController : UnityEngine.Object
    {
        public UnityEngine.GameObject modelPrefab;
        public UnityEngine.Transform model;
        public UnityEngine.Transform modelParent = new UnityEngine.GameObject().transform;
    }
}
