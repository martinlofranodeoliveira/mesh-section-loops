// Copyright 2026 Lofrano Tecnologia
// SPDX-License-Identifier: MIT

using MeshSectionLoops;
using System.Numerics;

namespace MeshSectionLoops.Tests;

public static class Program
{
    private static int _passedTests = 0;
    private static int _failedTests = 0;

    private static void Assert(bool condition, string message)
    {
        if (condition)
        {
            _passedTests++;
        }
        else
        {
            _failedTests++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {message}");
            Console.ResetColor();
        }
    }

    public static int Main()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("Mesh Section Loops Test Suite");
        Console.WriteLine("========================================");

        TestRational();
        TestCubeCut();
        TestTetrahedronCut();
        TestDegenerateCut();
        TestDisjointCut();
        TestInvalidMesh();

        Console.WriteLine("========================================");
        Console.WriteLine($"Passed: {_passedTests}, Failed: {_failedTests}");
        Console.WriteLine("========================================");

        return _failedTests == 0 ? 0 : 1;
    }

    private static void TestRational()
    {
        var r1 = new Rational(6, 4);
        Assert(r1.Numerator == 3 && r1.Denominator == 2, "Rational reduction 6/4 == 3/2");

        var r2 = new Rational(1, -2);
        Assert(r2.Numerator == -1 && r2.Denominator == 2, "Rational negative denominator normalization");

        var sum = new Rational(1, 3) + new Rational(1, 6);
        Assert(sum == new Rational(1, 2), "Rational 1/3 + 1/6 == 1/2");

        var diff = new Rational(1, 2) - new Rational(1, 3);
        Assert(diff == new Rational(1, 6), "Rational 1/2 - 1/3 == 1/6");

        var prod = new Rational(2, 3) * new Rational(3, 4);
        Assert(prod == new Rational(1, 2), "Rational 2/3 * 3/4 == 1/2");

        var div = new Rational(1, 2) / new Rational(3, 4);
        Assert(div == new Rational(2, 3), "Rational (1/2) / (3/4) == 2/3");

        Assert(new Rational(1, 3) < new Rational(1, 2), "Rational comparison 1/3 < 1/2");
    }

    public static MeshSurface CreateCubeMesh()
    {
        // Cube [0, 2]^3
        Point3I[] vertices = [
            new(0, 0, 0), // 0
            new(2, 0, 0), // 1
            new(2, 2, 0), // 2
            new(0, 2, 0), // 3
            new(0, 0, 2), // 4
            new(2, 0, 2), // 5
            new(2, 2, 2), // 6
            new(0, 2, 2)  // 7
        ];

        // 12 triangles with consistent outward winding
        TriangleIndices[] triangles = [
            // Bottom face (z = 0, normal (0, 0, -1))
            new(0, 2, 1), new(0, 3, 2),
            // Top face (z = 2, normal (0, 0, 1))
            new(4, 5, 6), new(4, 6, 7),
            // Front face (y = 0, normal (0, -1, 0))
            new(0, 1, 5), new(0, 5, 4),
            // Back face (y = 2, normal (0, 1, 0))
            new(3, 6, 2), new(3, 7, 6),
            // Left face (x = 0, normal (-1, 0, 0))
            new(0, 4, 7), new(0, 7, 3),
            // Right face (x = 2, normal (1, 0, 0))
            new(1, 2, 6), new(1, 6, 5)
        ];

        return new MeshSurface(vertices, triangles);
    }

    private static void TestCubeCut()
    {
        var cube = CreateCubeMesh();
        Assert(cube.Validate(out string? diag), $"Cube mesh validation: {diag}");

        // Cut at z = 1 (normal (0, 0, 1), offset 1)
        var plane = new Plane3I(0, 0, 1, 1);
        var result = SectionEngine.SectionMesh(cube, plane);

        Assert(result.Status == SectionStatus.CompleteLoops, "Cube cut status == CompleteLoops");
        Assert(result.Loops.Count == 1, "Cube cut produces exactly 1 loop");

        var loop = result.Loops[0];
        Assert(loop.Points.Count == 4, $"Cube cut simplified loop has 4 vertices (got {loop.Points.Count})");

        Rational absArea = Rational.One * loop.ProjectedSignedArea;
        if (absArea < Rational.Zero) absArea = -absArea;
        Assert(absArea == new Rational(4), $"Cube cut projected area is 4 (got {absArea})");
    }

    public static MeshSurface CreateTetrahedronMesh()
    {
        Point3I[] vertices = [
            new(0, 0, 0), // 0
            new(2, 0, 0), // 1
            new(0, 2, 0), // 2
            new(0, 0, 2)  // 3
        ];

        // 4 triangles with outward normal
        TriangleIndices[] triangles = [
            new(0, 2, 1), // bottom z = 0
            new(0, 1, 3), // front y = 0
            new(0, 3, 2), // left x = 0
            new(1, 2, 3)  // inclined face
        ];

        return new MeshSurface(vertices, triangles);
    }

    private static void TestTetrahedronCut()
    {
        var tet = CreateTetrahedronMesh();
        Assert(tet.Validate(out string? diag), $"Tetrahedron validation: {diag}");

        // Cut at z = 1
        var plane = new Plane3I(0, 0, 1, 1);
        var result = SectionEngine.SectionMesh(tet, plane);

        Assert(result.Status == SectionStatus.CompleteLoops, "Tetrahedron cut status == CompleteLoops");
        Assert(result.Loops.Count == 1, "Tetrahedron cut produces exactly 1 loop");

        var loop = result.Loops[0];
        Assert(loop.Points.Count == 3, $"Tetrahedron cut loop has 3 vertices (got {loop.Points.Count})");

        Rational absArea = Rational.One * loop.ProjectedSignedArea;
        if (absArea < Rational.Zero) absArea = -absArea;
        Assert(absArea == new Rational(1, 2), $"Tetrahedron cut projected area is 1/2 (got {absArea})");
    }

    private static void TestDegenerateCut()
    {
        var cube = CreateCubeMesh();
        // Cut at z = 0 passes through vertices 0, 1, 2, 3!
        var plane = new Plane3I(0, 0, 1, 0);
        var result = SectionEngine.SectionMesh(cube, plane);

        Assert(result.Status == SectionStatus.DegenerateCut, "Cut through vertices == DegenerateCut");
        Assert(result.DegenerateVertexIndices.Count == 4, "Detects exactly 4 degenerate vertices");
    }

    private static void TestDisjointCut()
    {
        var cube = CreateCubeMesh();
        // Cut at z = 10 is completely outside [0, 2]^3
        var plane = new Plane3I(0, 0, 1, 10);
        var result = SectionEngine.SectionMesh(cube, plane);

        Assert(result.Status == SectionStatus.CompleteEmpty, "Disjoint cut == CompleteEmpty");
        Assert(result.Loops.Count == 0, "Disjoint cut has 0 loops");
    }

    private static void TestInvalidMesh()
    {
        // Open mesh (missing triangles)
        Point3I[] vertices = [new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)];
        TriangleIndices[] triangles = [new(0, 1, 2)];
        var openMesh = new MeshSurface(vertices, triangles);

        var plane = new Plane3I(0, 0, 1, 1);
        var result = SectionEngine.SectionMesh(openMesh, plane);

        Assert(result.Status == SectionStatus.InvalidMesh, "Open mesh returns InvalidMesh");
    }
}
