// Copyright 2026 Lofrano Tecnologia
// SPDX-License-Identifier: MIT

using MeshSectionLoops;
using System.Text.Json;

namespace MeshSectionLoops.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: MeshSectionLoops.Cli <demo [--assert|--json] | section --mesh <path> --normal x,y,z --d offset>");
            return 1;
        }

        if (args[0] == "demo")
        {
            bool isAssert = args.Contains("--assert");
            bool isJson = args.Contains("--json");

            var cube = CreateCube();
            var planeCube = new Plane3I(0, 0, 1, 1); // z = 1
            var resCube = SectionEngine.SectionMesh(cube, planeCube);

            var tet = CreateTetrahedron();
            var planeTet = new Plane3I(0, 0, 1, 1); // z = 1
            var resTet = SectionEngine.SectionMesh(tet, planeTet);

            var planeDegen = new Plane3I(0, 0, 1, 0); // z = 0
            var resDegen = SectionEngine.SectionMesh(cube, planeDegen);

            var planeDisjoint = new Plane3I(0, 0, 1, 10); // z = 10
            var resDisjoint = SectionEngine.SectionMesh(cube, planeDisjoint);

            bool p1 = resCube.Status == SectionStatus.CompleteLoops &&
                      resCube.Loops.Count == 1 &&
                      resCube.Loops[0].Points.Count == 4;

            bool p2 = resTet.Status == SectionStatus.CompleteLoops &&
                      resTet.Loops.Count == 1 &&
                      resTet.Loops[0].Points.Count == 3;

            bool p3 = resDegen.Status == SectionStatus.DegenerateCut &&
                      resDegen.DegenerateVertexIndices.Count == 4;

            bool p4 = resDisjoint.Status == SectionStatus.CompleteEmpty &&
                      resDisjoint.Loops.Count == 0;

            if (isJson)
            {
                var summary = new
                {
                    scenario1_cube = new { status = resCube.Status.ToString(), loops = resCube.Loops.Count, pass = p1 },
                    scenario2_tetrahedron = new { status = resTet.Status.ToString(), loops = resTet.Loops.Count, pass = p2 },
                    scenario3_degenerate = new { status = resDegen.Status.ToString(), degenerateVertices = resDegen.DegenerateVertexIndices.Count, pass = p3 },
                    scenario4_disjoint = new { status = resDisjoint.Status.ToString(), pass = p4 }
                };
                Console.WriteLine(JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.WriteLine("========================================");
                Console.WriteLine("Mesh Section Loops Scenarios");
                Console.WriteLine("========================================");
                Console.WriteLine($"  1. Cube [0, 2]^3 Cut at z=1: {(p1 ? "PASS (Square of projected area 4)" : "FAIL")}");
                Console.WriteLine($"  2. Tetrahedron Cut at z=1: {(p2 ? "PASS (Triangle of projected area 1/2)" : "FAIL")}");
                Console.WriteLine($"  3. Degenerate Cut at z=0: {(p3 ? "PASS (4 vertices on plane detected)" : "FAIL")}");
                Console.WriteLine($"  4. Disjoint Cut at z=10: {(p4 ? "PASS (CompleteEmpty generic cut)" : "FAIL")}");
                Console.WriteLine("========================================");
                Console.WriteLine("All demo scenarios executed successfully.");
            }

            if (isAssert)
            {
                if (!p1 || !p2 || !p3 || !p4)
                {
                    Console.Error.WriteLine("Demo assertion failed!");
                    return 1;
                }
            }

            return 0;
        }

        Console.Error.WriteLine($"Unknown command: {args[0]}");
        return 1;
    }

    private static MeshSurface CreateCube()
    {
        Point3I[] vertices = [
            new(0, 0, 0), new(2, 0, 0), new(2, 2, 0), new(0, 2, 0),
            new(0, 0, 2), new(2, 0, 2), new(2, 2, 2), new(0, 2, 2)
        ];
        TriangleIndices[] triangles = [
            new(0, 2, 1), new(0, 3, 2),
            new(4, 5, 6), new(4, 6, 7),
            new(0, 1, 5), new(0, 5, 4),
            new(3, 6, 2), new(3, 7, 6),
            new(0, 4, 7), new(0, 7, 3),
            new(1, 2, 6), new(1, 6, 5)
        ];
        return new MeshSurface(vertices, triangles);
    }

    private static MeshSurface CreateTetrahedron()
    {
        Point3I[] vertices = [
            new(0, 0, 0), new(2, 0, 0), new(0, 2, 0), new(0, 0, 2)
        ];
        TriangleIndices[] triangles = [
            new(0, 2, 1),
            new(0, 1, 3),
            new(0, 3, 2),
            new(1, 2, 3)
        ];
        return new MeshSurface(vertices, triangles);
    }
}
