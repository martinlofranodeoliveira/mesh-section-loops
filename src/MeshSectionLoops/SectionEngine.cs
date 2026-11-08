// Copyright 2026 Lofrano Tecnologia
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace MeshSectionLoops;

public enum SectionStatus
{
    CompleteLoops,
    CompleteEmpty,
    DegenerateCut,
    InvalidMesh,
    InvalidPlane,
    Incomplete
}

public class ContourLoop
{
    public IReadOnlyList<RationalPoint3D> Points { get; }
    public IReadOnlyList<RationalPoint2D> ProjectedPoints { get; }
    public Rational ProjectedSignedArea { get; }

    public ContourLoop(IReadOnlyList<RationalPoint3D> points, int dominantAxis)
    {
        Points = points ?? throw new ArgumentNullException(nameof(points));
        ProjectedPoints = points.Select(p => p.ProjectDropAxis(dominantAxis)).ToList();
        ProjectedSignedArea = ComputeProjectedArea(ProjectedPoints);
    }

    private static Rational ComputeProjectedArea(IReadOnlyList<RationalPoint2D> pts)
    {
        if (pts.Count < 3) return Rational.Zero;
        Rational sum = Rational.Zero;
        for (int i = 0; i < pts.Count; i++)
        {
            var p1 = pts[i];
            var p2 = pts[(i + 1) % pts.Count];
            sum += (p1.U * p2.V - p2.U * p1.V);
        }
        return sum / new Rational(2);
    }
}

public class SectionResult
{
    public SectionStatus Status { get; }
    public string? Diagnostic { get; }
    public IReadOnlyList<ContourLoop> Loops { get; }
    public IReadOnlyList<int> DegenerateVertexIndices { get; }
    public int StepsExecuted { get; }
    public int MaxBudget { get; }

    public SectionResult(
        SectionStatus status,
        string? diagnostic,
        IReadOnlyList<ContourLoop> loops,
        IReadOnlyList<int> degenerateVertexIndices,
        int stepsExecuted,
        int maxBudget)
    {
        Status = status;
        Diagnostic = diagnostic;
        Loops = loops;
        DegenerateVertexIndices = degenerateVertexIndices;
        StepsExecuted = stepsExecuted;
        MaxBudget = maxBudget;
    }
}

public static class SectionEngine
{
    public const int DefaultBudget = 100000;

    /// <summary>
    /// Exact plane-mesh sectioning algorithm.
    /// </summary>
    public static SectionResult SectionMesh(MeshSurface mesh, Plane3I plane, int maxBudget = DefaultBudget)
    {
        int steps = 0;

        if (!plane.IsValid())
        {
            return new SectionResult(
                SectionStatus.InvalidPlane,
                "Plane specification is invalid or normal is zero vector.",
                [], [], steps, maxBudget);
        }

        if (!mesh.Validate(out string? meshDiagnostic))
        {
            return new SectionResult(
                SectionStatus.InvalidMesh,
                meshDiagnostic,
                [], [], steps, maxBudget);
        }

        // 1. Evaluate signed vertex distances: h(v) = n . v - D
        int vCount = mesh.Vertices.Count;
        BigInteger[] distances = new BigInteger[vCount];
        var degenerateVertices = new List<int>();
        int posCount = 0;
        int negCount = 0;

        for (int i = 0; i < vCount; i++)
        {
            steps++;
            if (steps > maxBudget)
                return new SectionResult(SectionStatus.Incomplete, "Budget exhausted during distance evaluation.", [], [], steps, maxBudget);

            BigInteger dist = plane.EvaluateDistance(mesh.Vertices[i]);
            distances[i] = dist;

            if (dist.IsZero)
                degenerateVertices.Add(i);
            else if (dist > 0)
                posCount++;
            else
                negCount++;
        }

        // If plane passes through any mesh vertex, return DegenerateCut with affected identities
        if (degenerateVertices.Count > 0)
        {
            return new SectionResult(
                SectionStatus.DegenerateCut,
                $"Plane intersects {degenerateVertices.Count} mesh vertex/vertices directly.",
                [], degenerateVertices, steps, maxBudget);
        }

        // Generic plane completely outside mesh
        if (posCount == 0 || negCount == 0)
        {
            return new SectionResult(
                SectionStatus.CompleteEmpty,
                "Plane is disjoint from mesh surface.",
                [], [], steps, maxBudget);
        }

        // Canonical edge intersection cache: (min(u, v), max(u, v)) -> RationalPoint3D
        var edgeIntersections = new Dictionary<(int, int), RationalPoint3D>();

        RationalPoint3D GetEdgeIntersection(int u, int v)
        {
            var key = (Math.Min(u, v), Math.Max(u, v));
            if (edgeIntersections.TryGetValue(key, out var pt))
                return pt;

            BigInteger hu = distances[u];
            BigInteger hv = distances[v];
            BigInteger denom = hu - hv; // hu and hv have opposite signs, so denom is non-zero

            Point3I pu = mesh.Vertices[u];
            Point3I pv = mesh.Vertices[v];

            // p = (pu * (-hv) + pv * hu) / denom
            BigInteger nx = new BigInteger(pu.X) * (-hv) + new BigInteger(pv.X) * hu;
            BigInteger ny = new BigInteger(pu.Y) * (-hv) + new BigInteger(pv.Y) * hu;
            BigInteger nz = new BigInteger(pu.Z) * (-hv) + new BigInteger(pv.Z) * hu;

            Rational rx = new(nx, denom);
            Rational ry = new(ny, denom);
            Rational rz = new(nz, denom);

            var inter = new RationalPoint3D(rx, ry, rz);
            edgeIntersections[key] = inter;
            return inter;
        }

        // 2. Identify crossing triangles and build directed segments
        // Each directed segment connects canonical edge E_in to E_out
        var edgeTransitions = new Dictionary<(int, int), (int, int)>();

        for (int t = 0; t < mesh.Triangles.Count; t++)
        {
            steps++;
            if (steps > maxBudget)
                return new SectionResult(SectionStatus.Incomplete, "Budget exhausted during triangle intersection.", [], [], steps, maxBudget);

            var tri = mesh.Triangles[t];
            (int, int)[] edges = [tri.Edge01, tri.Edge12, tri.Edge20];

            (int, int)? enteringEdge = null;
            (int, int)? exitingEdge = null;

            foreach (var (u, v) in edges)
            {
                BigInteger hu = distances[u];
                BigInteger hv = distances[v];

                if (hu < 0 && hv > 0)
                {
                    // Transition from negative to positive halfspace
                    enteringEdge = (Math.Min(u, v), Math.Max(u, v));
                }
                else if (hu > 0 && hv < 0)
                {
                    // Transition from positive to negative halfspace
                    exitingEdge = (Math.Min(u, v), Math.Max(u, v));
                }
            }

            if (enteringEdge.HasValue && exitingEdge.HasValue)
            {
                // In triangle t, section directed line goes from enteringEdge to exitingEdge
                edgeTransitions[enteringEdge.Value] = exitingEdge.Value;
            }
        }

        // 3. Assemble directed cycles
        var visitedEdges = new HashSet<(int, int)>();
        var loops = new List<ContourLoop>();
        int dominantAxis = plane.DominantAxis;

        foreach (var startEdge in edgeTransitions.Keys)
        {
            if (visitedEdges.Contains(startEdge))
                continue;

            var rawPoints = new List<RationalPoint3D>();
            var currentEdge = startEdge;

            while (true)
            {
                steps++;
                if (steps > maxBudget)
                    return new SectionResult(SectionStatus.Incomplete, "Budget exhausted during loop assembly.", [], [], steps, maxBudget);

                visitedEdges.Add(currentEdge);
                rawPoints.Add(GetEdgeIntersection(currentEdge.Item1, currentEdge.Item2));

                if (!edgeTransitions.TryGetValue(currentEdge, out var nextEdge))
                {
                    return new SectionResult(SectionStatus.InvalidMesh, "Broken loop transition in section.", [], [], steps, maxBudget);
                }

                currentEdge = nextEdge;
                if (currentEdge == startEdge)
                    break;

                if (visitedEdges.Contains(currentEdge))
                {
                    return new SectionResult(SectionStatus.InvalidMesh, "Unexpected loop branching detected.", [], [], steps, maxBudget);
                }
            }

            if (rawPoints.Count >= 3)
            {
                // 4. Exact Collinear Simplification
                var simplified = SimplifyCollinearPoints(rawPoints);
                if (simplified.Count >= 3)
                {
                    loops.Add(new ContourLoop(simplified, dominantAxis));
                }
            }
        }

        return new SectionResult(
            loops.Count > 0 ? SectionStatus.CompleteLoops : SectionStatus.CompleteEmpty,
            null,
            loops,
            [],
            steps,
            maxBudget);
    }

    /// <summary>
    /// Removes redundant collinear degree-2 points from a closed polygonal loop.
    /// </summary>
    private static List<RationalPoint3D> SimplifyCollinearPoints(List<RationalPoint3D> pts)
    {
        if (pts.Count <= 3) return [.. pts];

        var result = new List<RationalPoint3D>(pts);
        bool changed = true;

        while (changed && result.Count > 3)
        {
            changed = false;
            for (int i = 0; i < result.Count; i++)
            {
                int prevIdx = (i - 1 + result.Count) % result.Count;
                int nextIdx = (i + 1) % result.Count;

                if (RationalPoint3D.AreCollinear(result[prevIdx], result[i], result[nextIdx]))
                {
                    result.RemoveAt(i);
                    changed = true;
                    break;
                }
            }
        }

        return result;
    }
}
