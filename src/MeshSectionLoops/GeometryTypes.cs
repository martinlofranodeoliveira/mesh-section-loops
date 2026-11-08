// Copyright 2026 Lofrano Tecnologia
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace MeshSectionLoops;

/// <summary>
/// A 3D integer vertex coordinate bounded by signed 20-bit integers [-524288, 524287].
/// </summary>
public readonly record struct Point3I(int X, int Y, int Z)
{
    public const int MinCoordinate = -524288;
    public const int MaxCoordinate = 524287;

    public bool IsValid() =>
        X >= MinCoordinate && X <= MaxCoordinate &&
        Y >= MinCoordinate && Y <= MaxCoordinate &&
        Z >= MinCoordinate && Z <= MaxCoordinate;

    public override string ToString() => $"({X}, {Y}, {Z})";
}

/// <summary>
/// An oriented triangle specified by three vertex indices (v0, v1, v2).
/// </summary>
public readonly record struct TriangleIndices(int V0, int V1, int V2)
{
    public bool HasDuplicates() => V0 == V1 || V1 == V2 || V0 == V2;

    public (int, int) Edge01 => (V0, V1);
    public (int, int) Edge12 => (V1, V2);
    public (int, int) Edge20 => (V2, V0);
}

/// <summary>
/// An exact plane defined by integer normal (Nx, Ny, Nz) != 0 and integer offset D:
/// Nx * X + Ny * Y + Nz * Z = D.
/// </summary>
public readonly record struct Plane3I(int Nx, int Ny, int Nz, long D)
{
    public const int MinNormal = -32768;
    public const int MaxNormal = 32767;
    public const long MinOffset = -(1L << 36);
    public const long MaxOffset = (1L << 36) - 1;

    public bool IsValid()
    {
        if (Nx == 0 && Ny == 0 && Nz == 0) return false;
        if (Nx < MinNormal || Nx > MaxNormal) return false;
        if (Ny < MinNormal || Ny > MaxNormal) return false;
        if (Nz < MinNormal || Nz > MaxNormal) return false;
        if (D < MinOffset || D > MaxOffset) return false;
        return true;
    }

    /// <summary>
    /// Evaluates the signed distance predicate: h(v) = n . v - D.
    /// Strictly fits within a signed 38-bit integer envelope.
    /// </summary>
    public BigInteger EvaluateDistance(Point3I v)
    {
        BigInteger dot = new BigInteger(Nx) * v.X + new BigInteger(Ny) * v.Y + new BigInteger(Nz) * v.Z;
        return dot - D;
    }

    /// <summary>
    /// Returns the coordinate axis (0 for X, 1 for Y, 2 for Z) with the largest normal magnitude,
    /// used for canonical 2D projection.
    /// </summary>
    public int DominantAxis
    {
        get
        {
            int ax = Math.Abs(Nx);
            int ay = Math.Abs(Ny);
            int az = Math.Abs(Nz);
            if (ax >= ay && ax >= az) return 0;
            if (ay >= ax && ay >= az) return 1;
            return 2;
        }
    }
}

/// <summary>
/// An exact 3D point with canonical Rational coordinates.
/// </summary>
public readonly record struct RationalPoint3D(Rational X, Rational Y, Rational Z)
{
    public override string ToString() => $"({X}, {Y}, {Z})";

    public static bool AreCollinear(RationalPoint3D a, RationalPoint3D b, RationalPoint3D c)
    {
        // Vectors v1 = b - a, v2 = c - b
        Rational dx1 = b.X - a.X;
        Rational dy1 = b.Y - a.Y;
        Rational dz1 = b.Z - a.Z;

        Rational dx2 = c.X - b.X;
        Rational dy2 = c.Y - b.Y;
        Rational dz2 = c.Z - b.Z;

        // Cross product v1 x v2 = (dy1*dz2 - dz1*dy2, dz1*dx2 - dx1*dz2, dx1*dy2 - dy1*dx2)
        Rational cx = dy1 * dz2 - dz1 * dy2;
        Rational cy = dz1 * dx2 - dx1 * dz2;
        Rational cz = dx1 * dy2 - dy1 * dx2;

        return cx.IsZero && cy.IsZero && cz.IsZero;
    }

    public RationalPoint2D ProjectDropAxis(int axis) => axis switch
    {
        0 => new RationalPoint2D(Y, Z), // Drop X -> (Y, Z)
        1 => new RationalPoint2D(X, Z), // Drop Y -> (X, Z)
        _ => new RationalPoint2D(X, Y), // Drop Z -> (X, Y)
    };
}

/// <summary>
/// An exact 2D point with canonical Rational coordinates.
/// </summary>
public readonly record struct RationalPoint2D(Rational U, Rational V)
{
    public override string ToString() => $"({U}, {V})";
}
