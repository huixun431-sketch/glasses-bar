using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GlassesBar;

/// <summary>
/// Builds a visual prism and non-overlapping triangular-prism collision from one
/// authoritative convex XZ footprint. Vertices are emitted in world-local coordinates,
/// so presentation and collision consume exactly the same layout record.
/// </summary>
public static class BarPolygonGeometry
{
    public static ArrayMesh CreateMesh(BarPolygonPrismLayout layout)
    {
        var footprint = NormalizeFootprint(layout);
        var triangles = TriangulateFan(footprint);
        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);

        foreach (var (a, b, c) in triangles)
        {
            AddTriangle(surface,
                ToVector3(footprint[a], layout.TopY),
                ToVector3(footprint[c], layout.TopY),
                ToVector3(footprint[b], layout.TopY));
            AddTriangle(surface,
                ToVector3(footprint[a], layout.BottomY),
                ToVector3(footprint[b], layout.BottomY),
                ToVector3(footprint[c], layout.BottomY));
        }

        for (var index = 0; index < footprint.Count; index++)
        {
            var next = (index + 1) % footprint.Count;
            var lowerA = ToVector3(footprint[index], layout.BottomY);
            var lowerB = ToVector3(footprint[next], layout.BottomY);
            var upperA = ToVector3(footprint[index], layout.TopY);
            var upperB = ToVector3(footprint[next], layout.TopY);
            AddTriangle(surface, lowerA, lowerB, upperB);
            AddTriangle(surface, lowerA, upperB, upperA);
        }

        return surface.Commit() ??
               throw new InvalidOperationException($"Could not create polygon mesh '{layout.Name}'.");
    }

    public static MeshInstance3D CreateVisual(
        Node3D parent,
        BarPolygonPrismLayout layout,
        Material material)
    {
        var visual = new MeshInstance3D
        {
            Name = layout.Name,
            Mesh = CreateMesh(layout),
            MaterialOverride = material
        };
        parent.AddChild(visual);
        return visual;
    }

    public static StaticBody3D CreateCollisionBody(
        Node3D parent,
        string name,
        BarPolygonPrismLayout layout,
        uint collisionLayer)
    {
        var footprint = NormalizeFootprint(layout);
        var body = new StaticBody3D
        {
            Name = name,
            CollisionLayer = collisionLayer
        };
        parent.AddChild(body);

        var triangleIndex = 0;
        foreach (var (a, b, c) in TriangulateFan(footprint))
        {
            var shape = new ConvexPolygonShape3D
            {
                Points = new[]
                {
                    ToVector3(footprint[a], layout.BottomY),
                    ToVector3(footprint[b], layout.BottomY),
                    ToVector3(footprint[c], layout.BottomY),
                    ToVector3(footprint[a], layout.TopY),
                    ToVector3(footprint[b], layout.TopY),
                    ToVector3(footprint[c], layout.TopY)
                }
            };
            body.AddChild(new CollisionShape3D
            {
                Name = $"Prism{++triangleIndex}",
                Shape = shape
            });
        }

        return body;
    }

    private static IReadOnlyList<Vector2> NormalizeFootprint(BarPolygonPrismLayout layout)
    {
        if (layout.TopY <= layout.BottomY)
            throw new InvalidOperationException(
                $"Polygon prism '{layout.Name}' must have positive height.");
        if (layout.Footprint is null || layout.Footprint.Count < 3)
            throw new InvalidOperationException(
                $"Polygon prism '{layout.Name}' must contain at least three points.");

        var points = layout.Footprint.ToList();
        for (var index = 0; index < points.Count; index++)
        {
            var next = points[(index + 1) % points.Count];
            if (points[index].DistanceSquaredTo(next) < 0.000001f)
                throw new InvalidOperationException(
                    $"Polygon prism '{layout.Name}' contains duplicate adjacent points at {index}.");
        }

        var area = SignedArea(points);
        if (Math.Abs(area) < 0.000001f)
            throw new InvalidOperationException($"Polygon prism '{layout.Name}' has zero area.");
        if (area < 0f)
            points.Reverse();

        var orientation = 0f;
        for (var index = 0; index < points.Count; index++)
        {
            var a = points[index];
            var b = points[(index + 1) % points.Count];
            var c = points[(index + 2) % points.Count];
            var cross = Cross(b - a, c - b);
            if (Math.Abs(cross) < 0.000001f)
                continue;
            if (orientation == 0f)
                orientation = Math.Sign(cross);
            else if (Math.Sign(cross) != Math.Sign(orientation))
                throw new InvalidOperationException(
                    $"Polygon prism '{layout.Name}' must remain convex for deterministic collision decomposition.");
        }
        return points.AsReadOnly();
    }

    private static IReadOnlyList<(int A, int B, int C)> TriangulateFan(
        IReadOnlyList<Vector2> footprint)
    {
        var triangles = new List<(int A, int B, int C)>();
        for (var index = 1; index < footprint.Count - 1; index++)
        {
            if (Math.Abs(Cross(footprint[index] - footprint[0],
                    footprint[index + 1] - footprint[0])) < 0.000001f)
                continue;
            triangles.Add((0, index, index + 1));
        }
        if (triangles.Count == 0)
            throw new InvalidOperationException("Polygon fan triangulation produced no triangles.");
        return triangles;
    }

    private static void AddTriangle(SurfaceTool surface, Vector3 a, Vector3 b, Vector3 c)
    {
        var normal = (b - a).Cross(c - a).Normalized();
        surface.SetNormal(normal);
        surface.AddVertex(a);
        surface.SetNormal(normal);
        surface.AddVertex(b);
        surface.SetNormal(normal);
        surface.AddVertex(c);
    }

    private static Vector3 ToVector3(Vector2 point, float y) => new(point.X, y, point.Y);

    private static float SignedArea(IReadOnlyList<Vector2> points)
    {
        var area = 0f;
        for (var index = 0; index < points.Count; index++)
        {
            var next = (index + 1) % points.Count;
            area += points[index].X * points[next].Y - points[next].X * points[index].Y;
        }
        return area * 0.5f;
    }

    private static float Cross(Vector2 first, Vector2 second) =>
        first.X * second.Y - first.Y * second.X;
}
