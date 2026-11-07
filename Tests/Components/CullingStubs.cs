using System;
using System.Collections.Generic;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string text) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RangeAttribute : Attribute
    {
        public RangeAttribute(float minimum, float maximum) { }
    }

    public sealed class UnityException : Exception
    {
        public UnityException(string message) : base(message) { }
    }

    public enum PrimitiveType { Sphere }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y + z * z); } }
        public void Set(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 operator +(Vector3 first, Vector3 second)
        {
            return new Vector3(first.x + second.x, first.y + second.y, first.z + second.z);
        }
        public static Vector3 operator -(Vector3 first, Vector3 second)
        {
            return new Vector3(first.x - second.x, first.y - second.y, first.z - second.z);
        }
        public static Vector3 operator *(Vector3 vector, float scale)
        {
            return new Vector3(vector.x * scale, vector.y * scale, vector.z * scale);
        }
    }

    public struct Bounds
    {
        public Vector3 center, extents;
        public Bounds(Vector3 center, Vector3 size) { this.center = center; extents = size * 0.5f; }
    }

    public sealed class Mesh : Object { public Bounds bounds { get; set; } }
    public sealed class MeshFilter : Component { public Mesh mesh { get; set; } }
    public class Renderer : Component
    {
        // Fixtures provide the engine's world-space AABB independently of the
        // mesh-local AABB. Production CullingObject chooses the actual sphere.
        public Bounds bounds { get; set; }
        public bool enabled = true;
        public Material material { get; set; } = new Material();
    }
    public sealed class Material : Object { public Color color { get; set; } }
    public struct Color
    {
        public static Color red { get { return new Color(); } }
        public static Color gray { get { return new Color(); } }
    }

    public struct BoundingSphere
    {
        public Vector3 position;
        public float radius;
        public BoundingSphere(Vector3 position, float radius) { this.position = position; this.radius = radius; }
    }

    public struct CullingGroupEvent
    {
        public int index;
        public bool isVisible;
        public int currentDistance;
    }

    public sealed class CullingGroup : IDisposable
    {
        public static CullingGroup LastCreated { get; internal set; }
        public BoundingSphere[] Spheres { get; private set; }
        public int SphereCount { get; private set; }
        public bool Disposed { get; private set; }
        public Camera targetCamera { get; set; }
        public bool enabled { get; set; }
        public Action<CullingGroupEvent> onStateChanged;

        public CullingGroup() { LastCreated = this; }
        public void SetBoundingDistances(float[] distances) { }
        public void SetBoundingSpheres(BoundingSphere[] spheres) { Spheres = spheres; }
        public void SetBoundingSphereCount(int count) { SphereCount = count; }
        public void SetDistanceReferencePoint(Transform reference) { }
        public void Dispose() { Disposed = true; }
        public void Report(int index, bool visible, int distance)
        {
            onStateChanged?.Invoke(new CullingGroupEvent { index = index, isVisible = visible, currentDistance = distance });
        }
    }
}

namespace Script.Environment.Optimizer
{
    public sealed class CullingTarget : UnityEngine.MonoBehaviour
    {
        // Target discovery is a collaborator boundary; these are actual production
        // CullingObject instances, not replacement sphere-calculation logic.
        public readonly List<CullingObject> PreparedObjects = new List<CullingObject>();
        public void GetCullingObjects(List<CullingObject> destination) { destination.AddRange(PreparedObjects); }
    }
}
