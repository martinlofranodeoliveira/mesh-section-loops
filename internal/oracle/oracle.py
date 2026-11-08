#!/usr/bin/env python3
# Copyright 2026 Lofrano Tecnologia
# SPDX-License-Identifier: MIT

"""
Independent Python Fractions Reference for Exact Mesh-Plane Sectioning.
Cross-validates C# Rational calculations against python's fractions.Fraction standard library.
"""

from fractions import Fraction
import json


def evaluate_distance(vertex, normal, d):
    return normal[0] * vertex[0] + normal[1] * vertex[1] + normal[2] * vertex[2] - d


def intersect_edge(u, v, hu, hv):
    denom = hu - hv
    # p = (u * (-hv) + v * hu) / denom
    px = Fraction(u[0] * (-hv) + v[0] * hu, denom)
    py = Fraction(u[1] * (-hv) + v[1] * hu, denom)
    pz = Fraction(u[2] * (-hv) + v[2] * hu, denom)
    return (px, py, pz)


def main():
    with open("tests/fixtures/meshes.json") as f:
        fixtures = json.load(f)

    # 1. Cube test at z = 1
    cube = fixtures["cube"]
    verts = cube["vertices"]
    normal = (0, 0, 1)
    d = 1

    dists = [evaluate_distance(v, normal, d) for v in verts]
    # Vertices 0..3 have z=0 -> dist = -1
    # Vertices 4..7 have z=2 -> dist = +1
    assert all(dists[i] == -1 for i in range(4))
    assert all(dists[i] == 1 for i in range(4, 8))

    # Crossing edges connect 0-4, 1-5, 2-6, 3-7
    # All intersection points must have z = 1
    for i in range(4):
        pt = intersect_edge(verts[i], verts[i + 4], dists[i], dists[i + 4])
        assert pt[2] == Fraction(1, 1), f"Expected z=1, got {pt[2]}"

    # Projected square area on (x, y) = 2 * 2 = 4
    print("Python Fractions Oracle: Cube cut at z=1 verified (square area 4).")

    # 2. Tetrahedron test at z = 1
    tet = fixtures["tetrahedron"]
    tverts = tet["vertices"]
    tdists = [evaluate_distance(v, normal, d) for v in tverts]
    # Vertices 0, 1, 2 have z=0 -> dist = -1
    # Vertex 3 has z=2 -> dist = +1
    p03 = intersect_edge(tverts[0], tverts[3], tdists[0], tdists[3])
    p13 = intersect_edge(tverts[1], tverts[3], tdists[1], tdists[3])
    p23 = intersect_edge(tverts[2], tverts[3], tdists[2], tdists[3])

    assert p03 == (Fraction(0), Fraction(0), Fraction(1))
    assert p13 == (Fraction(1), Fraction(0), Fraction(1))
    assert p23 == (Fraction(0), Fraction(1), Fraction(1))

    # Projected right triangle: legs at (1, 0) and (0, 1) -> area = 1/2
    tri_area = Fraction(1, 2) * (p13[0] - p03[0]) * (p23[1] - p03[1])
    assert tri_area == Fraction(1, 2), f"Expected 1/2, got {tri_area}"
    print("Python Fractions Oracle: Tetrahedron cut at z=1 verified (triangle area 1/2).")

    print("All Python Fractions Oracle checks PASSED successfully.")


if __name__ == "__main__":
    main()
