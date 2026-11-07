using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

internal static class ComponentTestSupport
{
    internal static T Create<T>() where T : Component, new()
    {
        return new GameObject().AddComponent<T>();
    }

    internal static void Case(string name, Action body)
    {
        StubRuntime.ClearAll();
        try
        {
            body();
            Console.WriteLine("  PASS " + name);
        }
        finally { StubRuntime.ClearAll(); }
    }

    internal static void Call(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = FindMethod(target.GetType(), methodName);
        if (method == null) throw new InvalidOperationException("Missing lifecycle method: " + methodName);
        method.Invoke(target, arguments);
    }

    internal static MethodInfo FindMethod(Type type, string name)
    {
        while (type != null)
        {
            MethodInfo method = type.GetMethod(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (method != null) return method;
            type = type.BaseType;
        }
        return null;
    }

    internal static void SetField(object target, string name, object value)
    {
        Type type = target.GetType();
        while (type != null)
        {
            FieldInfo field = type.GetField(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field != null) { field.SetValue(target, value); return; }
            type = type.BaseType;
        }
        throw new InvalidOperationException("Missing serialized field: " + name);
    }

    internal static int Listeners(Delegate signal, object subscriber)
    {
        return signal == null ? 0 : signal.GetInvocationList()
            .Count(callback => ReferenceEquals(callback.Target, subscriber));
    }

    internal static void Equal<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual))
            throw new InvalidOperationException(message + " Expected " + expected + ", got " + actual + ".");
    }

    internal static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
