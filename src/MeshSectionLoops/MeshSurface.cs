// Copyright 2026 Lofrano Tecnologia
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace MeshSectionLoops;

/// <summary>
/// Immutable indexed triangular surface with topology validation.
/// </summary>
public class MeshSurface
{
    public const int MaxVertices = 16384;
    public const int MaxTriangles = 32768;
    public const int MaxDirectedIncidences = 98304;

    public IReadOnlyList<Point3I> Vertices { get; }
    public IReadOnlyList<TriangleIndices> Triangles { get; }

    public MeshSurface(IReadOnlyList<Point3I> vertices, IReadOnlyList<TriangleIndices> triangles)
    {
        Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        Triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));
    }

    /// <summary>
    /// Validates surface cardinality, geometry bounds, and 2-manifold closed orientability.
    /// Returns true if valid, or false with an explanatory diagnostic.
    /// </summary>
    public bool Validate(out string? diagnostic)
    {
        if (Vertices.Count == 0 || Vertices.Count > MaxVertices)
        {
            diagnostic = $"Vertex count {Vertices.Count} must be in [1, {MaxVertices}].";
            return false;
        }

        for (int i = 0; i < Vertices.Count; i++)
        {
            if (!Vertices[i].IsValid())
            {
                diagnostic = $"Vertex {i} {Vertices[i]} exceeds 20-bit coordinate bounds.";
                return false;
            }
        }

        if (Triangles.Count == 0 || Triangles.Count > MaxTriangles)
        {
            diagnostic = $"Triangle count {Triangles.Count} must be in [1, {MaxTriangles}].";
            return false;
        }

        int totalIncidences = Triangles.Count * 3;
        if (totalIncidences > MaxDirectedIncidences)
        {
            diagnostic = $"Total directed edge incidences {totalIncidences} exceeds maximum {MaxDirectedIncidences}.";
            return false;
        }

        // Half-edge incidence map: (u, v) -> triangle index
        var halfEdges = new Dictionary<(int, int), int>();
        // Vertex incident outgoing edges: v -> list of w where (v, w) is a directed edge
        var outgoing = new Dictionary<int, List<int>>();

        for (int t = 0; t < Triangles.Count; t++)
        {
            var tri = Triangles[t];
            if (tri.HasDuplicates())
            {
                diagnostic = $"Triangle {t} contains duplicate vertex indices: ({tri.V0}, {tri.V1}, {tri.V2}).";
                return false;
            }

            if (tri.V0 < 0 || tri.V0 >= Vertices.Count ||
                tri.V1 < 0 || tri.V1 >= Vertices.Count ||
                tri.V2 < 0 || tri.V2 >= Vertices.Count)
            {
                diagnostic = $"Triangle {t} references out-of-range vertex index.";
                return false;
            }

            // Non-zero area check (cross product of edge vectors != 0)
            Point3I p0 = Vertices[tri.V0];
            Point3I p1 = Vertices[tri.V1];
            Point3I p2 = Vertices[tri.V2];

            long d1x = p1.X - p0.X;
            long d1y = p1.Y - p0.Y;
            long d1z = p1.Z - p0.Z;

            long d2x = p2.X - p0.X;
            long d2y = p2.Y - p0.Y;
            long d2z = p2.Z - p0.Z;

            long cx = d1y * d2z - d1z * d2y;
            long cy = d1z * d2x - d1x * d2z;
            long cz = d1x * d2y - d1y * d2x;

            if (cx == 0 && cy == 0 && cz == 0)
            {
                diagnostic = $"Triangle {t} is degenerate with zero geometric area.";
                return false;
            }

            (int, int)[] edges = [tri.Edge01, tri.Edge12, tri.Edge20];
            foreach (var e in edges)
            {
                if (halfEdges.ContainsKey(e))
                {
                    diagnostic = $"Non-orientable or duplicate directed edge ({e.Item1}, {e.Item2}) in triangle {t}.";
                    return false;
                }
                halfEdges[e] = t;

                if (!outgoing.TryGetValue(e.Item1, out var outs))
                {
                    outs = new List<int>();
                    outgoing[e.Item1] = outs;
                }
                outs.Add(e.Item2);
            }
        }

        // Check exactly two opposite-oriented incidences per undirected edge
        foreach (var (u, v) in halfEdges.Keys)
        {
            if (!halfEdges.ContainsKey((v, u)))
            {
                diagnostic = $"Open boundary detected: directed edge ({u}, {v}) lacks opposite incidence ({v}, {u}).";
                return false;
            }
        }

        // Check single cyclic link around each participating vertex
        foreach (var (v, outs) in outgoing)
        {
            if (outs.Count < 3)
            {
                diagnostic = $"Vertex {v} has degree {outs.Count} < 3, violating closed 2-manifold.";
                return false;
            }

            // Trace the umbrella cycle around vertex v:
            // For half-edge (v, w), find the unique triangle T containing it.
            // In T, the next vertex is z where triangle is (v, w, z).
            // Then the incoming edge to v is (z, v), and its opposite is (v, ...).
            int startW = outs[0];
            int currentW = startW;
            int visitedCount = 0;

            var visitedOuts = new HashSet<int>();

            while (true)
            {
                visitedOuts.Add(currentW);
                visitedCount++;

                int triIdx = halfEdges[(v, currentW)];
                var tri = Triangles[triIdx];
                int nextZ = tri.V0 == v && tri.V1 == currentW ? tri.V2 :
                            tri.V1 == v && tri.V2 == currentW ? tri.V0 :
                            tri.V2 == v && tri.V0 == currentW ? tri.V1 : -1;

                if (nextZ == -1)
                {
                    diagnostic = $"Broken vertex umbrella around vertex {v}.";
                    return false;
                }

                currentW = nextZ;
                if (currentW == startW)
                    break;

                if (visitedCount > outs.Count)
                {
                    diagnostic = $"Non-terminating cycle around vertex {v}.";
                    return false;
                }
            }

            if (visitedCount != outs.Count)
            {
                diagnostic = $"Vertex {v} has multiple disjoint umbrella fans (pinched non-manifold vertex).";
                return false;
            }
        }

        diagnostic = null;
        return true;
    }
}
