using System;
using System.Collections.Generic;
using Script.Environment.Optimizer;
using UnityEngine;
using static ComponentTestSupport;

public static class CullingBoundsTests
{
    public static void Run()
    {
        Case("Scaled offset mesh uses the world center and encloses every corner", ScaledOffsetMesh);
        Case("Nonuniform rotated mesh keeps its world bounds", NonuniformMesh);
        Case("A small configured radius cannot clip real geometry", RadiusThresholdCannotShrink);
        Case("Quest2 combined coral geometry fits inside its sphere", Quest2CoralGeometry);
        Case("Culling helper reads fresh renderer bounds when queried", HelperReadsCurrentBounds);
        Case("Optimizer publishes valid spheres and preserves visibility switching", OptimizerUsesWorldSphere);
    }

    private sealed class Geometry
    {
        internal readonly Renderer Renderer = Create<Renderer>();
        internal readonly MeshFilter MeshFilter;
        internal readonly CullingObject Object;

        internal Geometry(Vector3 origin, Vector3 localCenter, Vector3 localExtents,
            Vector3 scale, Vector3 worldCenter, Vector3 worldExtents, float threshold = 100f)
        {
            Renderer.transform.position = origin;
            Renderer.transform.localScale = scale;
            Renderer.bounds = new Bounds(worldCenter, worldExtents * 2f);
            MeshFilter = Renderer.gameObject.AddComponent<MeshFilter>();
            MeshFilter.mesh = new Mesh { bounds = new Bounds(localCenter, localExtents * 2f) };
            Object = new CullingObject(Renderer, MeshFilter, Renderer.transform, threshold);
        }

        internal BoundingSphere Sphere() { return new BoundingSphere(Object.Position, Object.GetRadius()); }
    }

    private static Geometry ScaledGeometry(float threshold = 100f)
    {
        return new Geometry(new Vector3(100f, 0f, 0f), new Vector3(2f, 0f, 0f), new Vector3(1f, 1f, 1f),
            new Vector3(10f, 10f, 10f), new Vector3(120f, 0f, 0f), new Vector3(10f, 10f, 10f), threshold);
    }

    private static void ScaledOffsetMesh()
    {
        Geometry geometry = ScaledGeometry();
        Encloses(geometry.Renderer.bounds, geometry.Sphere());
        Near(new Vector3(120f, 0f, 0f), geometry.Object.Position);
    }

    private static void NonuniformMesh()
    {
        // A 90-degree Z rotation after scale (2,3,4) swaps the X/Y AABB extents.
        var geometry = new Geometry(new Vector3(10f, -2f, 3f), new Vector3(1f, 2f, 3f), new Vector3(1f, 2f, 3f),
            new Vector3(2f, 3f, 4f), new Vector3(4f, 0f, 15f), new Vector3(6f, 2f, 12f));
        Encloses(geometry.Renderer.bounds, geometry.Sphere());
        Near(geometry.Renderer.bounds.center, geometry.Object.Position);
    }

    private static void RadiusThresholdCannotShrink()
    {
        Geometry geometry = ScaledGeometry(threshold: 1f);
        Encloses(geometry.Renderer.bounds, geometry.Sphere());
        Assert(geometry.Object.GetRadius() > 1f, "A warning threshold cannot shrink the sphere below its renderer.");
    }

    private static void Quest2CoralGeometry()
    {
        // Quest2's 23_combined_static mesh AABB and its Floor ancestor scale,
        // with the Floor's -90-degree X rotation applied to the world AABB.
        var geometry = new Geometry(new Vector3(0f, -24f, 0f),
            new Vector3(-0.005624373f, 0.009671821f, 0.006137886f),
            new Vector3(0.0019032125f, 0.002290427f, 0.0010994561f),
            new Vector3(5000f, 5000f, 2500f),
            new Vector3(-28.121865f, -8.655285f, -48.359105f),
            new Vector3(9.5160625f, 2.74864025f, 11.452135f), threshold: 1f);
        Encloses(geometry.Renderer.bounds, geometry.Sphere());
        Near(15.141396f, geometry.Object.GetRadius());
        Near(58.00778f, (geometry.Object.Position - geometry.Renderer.transform.position).magnitude);
    }

    private static void HelperReadsCurrentBounds()
    {
        Geometry geometry = ScaledGeometry();
        geometry.Renderer.bounds = new Bounds(new Vector3(-30f, 12f, 5f), new Vector3(80f, 8f, 4f));
        Encloses(geometry.Renderer.bounds, geometry.Sphere());
        Near(geometry.Renderer.bounds.center, geometry.Object.Position);
    }

    private static void OptimizerUsesWorldSphere()
    {
        Geometry geometry = ScaledGeometry(threshold: 1f);
        CullingTarget target = Create<CullingTarget>();
        target.PreparedObjects.Add(geometry.Object);
        CullingOptimizer optimizer = Create<CullingOptimizer>();
        SetField(optimizer, "camera", Create<Camera>());
        SetField(optimizer, "targets", new List<CullingTarget> { target });
        Call(optimizer, "OnEnable");
        CullingGroup group = CullingGroup.LastCreated;
        try
        {
            Equal(1, group.SphereCount, "Optimizer must publish its actual target count.");
            Encloses(geometry.Renderer.bounds, group.Spheres[0]);
            Near(geometry.Renderer.bounds.center, group.Spheres[0].position);
            group.Report(0, visible: false, distance: 0);
            Assert(!geometry.Renderer.enabled, "A target outside the view must still be hidden.");
            group.Report(0, visible: true, distance: 1);
            Assert(!geometry.Renderer.enabled, "A target outside the rendering distance must still be hidden.");
            group.Report(0, visible: true, distance: 0);
            Assert(geometry.Renderer.enabled, "A visible nearby target must be restored.");
            Assert(geometry.Renderer.gameObject.activeSelf, "Renderer culling cannot deactivate the whole game object.");
        }
        finally { Call(optimizer, "OnDisable"); }
        Assert(group.Disposed, "Disabling the optimizer must dispose its culling group.");
    }

    private static void Encloses(Bounds bounds, BoundingSphere sphere)
    {
        Assert(!float.IsNaN(sphere.radius) && !float.IsInfinity(sphere.radius) && sphere.radius >= 0f,
            "Culling radius must be finite and non-negative.");
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = bounds.center + new Vector3(x * bounds.extents.x, y * bounds.extents.y, z * bounds.extents.z);
                    float distance = (corner - sphere.position).magnitude;
                    Assert(distance <= sphere.radius + 0.0001f,
                        "World-space renderer corner lies outside its culling sphere: distance " + distance
                        + ", radius " + sphere.radius + ".");
                }
    }

    private static void Near(Vector3 expected, Vector3 actual)
    {
        Near(expected.x, actual.x); Near(expected.y, actual.y); Near(expected.z, actual.z);
    }

    private static void Near(float expected, float actual)
    {
        Assert(!float.IsNaN(actual) && !float.IsInfinity(actual) && Math.Abs(expected - actual) <= 0.0001f,
            "Expected " + expected + ", got " + actual + ".");
    }
}
