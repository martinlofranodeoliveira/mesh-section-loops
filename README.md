# mesh-section-loops

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-14.0-blue.svg)](https://learn.microsoft.com/dotnet/csharp/)
[![Exact Arithmetic](https://img.shields.io/badge/Math-Exact%20Rational-brightgreen.svg)](#exact-rational-arithmetic)
[![Zero NuGet Runtime Deps](https://img.shields.io/badge/Dependencies-Zero%20NuGet-success.svg)](#zero-dependencies)

> **An exact mesh-plane section SDK in C# (.NET 10) for oriented triangular surfaces. Computes topology-preserving contour loops with exact BigInteger rational coordinates, integer predicates, and explicit degeneration classification.**

---

## Overview

Planar slicing is fundamental to computational geometry, finite element pre-processing, and 3D printing toolpath generation (slicers). Traditional mesh slicers rely on IEEE 754 floating-point arithmetic and heuristic coordinate welding tolerances (e.g. $\epsilon = 10^{-5}$) to stitch segments into closed loops. In boundary cases or near-coincident cuts, rounding errors inevitably create micro-gaps, self-intersections, or spurious topologies.

`mesh-section-loops` eliminates floating-point tolerance heuristics by solving mesh-plane intersection **exactly**:
- **Integer Predicates**: Evaluates signed vertex-plane distances $h(v) = \mathbf{n} \cdot \mathbf{v} - d$ within a signed 38-bit integer envelope.
- **Exact Rational Coordinates**: Computes intersection points $\mathbf{p} = \frac{u \cdot (-h(v)) + v \cdot h(u)}{h(u) - h(v)}$ as normalized fractions using `System.Numerics.BigInteger`.
- **Topology-Driven Assembly**: Assembles closed loops directly from triangle half-edge incidences, guaranteeing closed contours without spatial proximity searches.
- **Explicit Degeneration**: Classifies cuts passing directly through mesh vertices ($h(v) = 0$) as `DegenerateCut` rather than silently approximating them.
- **Collinear Simplification**: Removes degree-2 redundant points along planar edges using exact rational 3D cross products.

---

## Key Features

1. **Closed 2-Manifold Validator**:
   - Enforces valid orientation pairing: every directed half-edge $(u, v)$ must have exactly one opposite directed edge $(v, u)$.
   - Verifies vertex umbrella cycles, rejecting non-manifold pinched cones.
   - Enforces non-zero geometric triangle area.
2. **Exact Rational Geometry (`Rational`)**:
   - Zero precision loss across all operations.
   - Exact 3D coordinates `RationalPoint3D` and canonical projected 2D coordinates `RationalPoint2D`.
   - Exact polygon area via rational shoelace formula.
3. **Structured Outcomes**:
   - `CompleteLoops`: Non-empty closed topological loops produced.
   - `CompleteEmpty`: Plane is disjoint from the mesh.
   - `DegenerateCut`: Plane passes through one or more vertices ($h(v) = 0$).
   - `InvalidMesh`: Non-manifold topology or degenerate triangles detected.
   - `InvalidPlane`: Normal is zero or parameters out of bounds.
   - `Incomplete`: Traversal step budget exhausted.

---

## Invariant Limits

| Metric | Bound | Description |
|:---:|:---:|:---|
| Max Vertices | 16,384 | Vertex cardinality ($V \le 2^{14}$) |
| Max Triangles | 32,768 | Triangle cardinality ($F \le 2^{15}$) |
| Max Incidences | 98,304 | Directed half-edge incidences ($3 \times 32768$) |
| Vertex Coordinate Envelope | $[-524288, 524287]$ | Signed 20-bit integer coordinates |
| Plane Normal Components | $[-32768, 32767]$ | Signed 16-bit integer normal $\mathbf{n} \ne \mathbf{0}$ |
| Plane Offset $D$ | $[-2^{36}, 2^{36}-1]$ | Signed 37-bit integer plane offset |
| Signed Distance Envelope | $[-2^{37}, 2^{37}-1]$ | Fits within signed 38-bit envelope |

---

## Quickstart & CLI Usage

### Build and Run Test Suite
```bash
dotnet build MeshSectionLoops.sln
dotnet run --project tests/MeshSectionLoops.Tests/MeshSectionLoops.Tests.csproj
```

### Run Demonstration Scenarios
```bash
dotnet run --project src/MeshSectionLoops.Cli/MeshSectionLoops.Cli.csproj -- demo --assert
```

### Emit JSON Diagnostics
```bash
dotnet run --project src/MeshSectionLoops.Cli/MeshSectionLoops.Cli.csproj -- demo --json
```

---

## C# API Example

```csharp
using MeshSectionLoops;

// 1. Define a triangular mesh (e.g. Tetrahedron)
Point3I[] vertices = [
    new(0, 0, 0), new(2, 0, 0), new(0, 2, 0), new(0, 0, 2)
];
TriangleIndices[] triangles = [
    new(0, 2, 1), new(0, 1, 3), new(0, 3, 2), new(1, 2, 3)
];
var mesh = new MeshSurface(vertices, triangles);

// 2. Define cutting plane: z = 1 (normal (0, 0, 1), offset 1)
var plane = new Plane3I(0, 0, 1, 1);

// 3. Compute exact section loops
var result = SectionEngine.SectionMesh(mesh, plane);

if (result.Status == SectionStatus.CompleteLoops)
{
    Console.WriteLine($"Produced {result.Loops.Count} loop(s).");
    var loop = result.Loops[0];
    Console.WriteLine($"Loop vertices: {loop.Points.Count}");
    Console.WriteLine($"Exact projected area: {loop.ProjectedSignedArea}"); // 1/2
}
```

---

## Technical Specifications & Architecture

For comprehensive mathematical proofs and topological guarantees:
- [Contract Specification](docs/contract.md): Signed distance envelope, 2-manifold invariants, and exact rational algebra.
- [Architecture & Design](docs/design.md): Half-edge incidence mappings, cycle walking, and collinear simplification.

---

## Security

Please report vulnerabilities via GitHub Private Vulnerability Reporting or refer to [SECURITY.md](SECURITY.md).

---

## License

This project is licensed under the [MIT License](LICENSE).
Copyright © 2026 Lofrano Tecnologia.
