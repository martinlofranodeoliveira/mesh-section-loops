// Copyright 2026 Lofrano Tecnologia
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace MeshSectionLoops;

/// <summary>
/// An exact canonical rational number N/D with D > 0 and gcd(|N|, D) = 1.
/// </summary>
public readonly struct Rational : IEquatable<Rational>, IComparable<Rational>
{
    public BigInteger Numerator { get; }
    public BigInteger Denominator { get; }

    public static Rational Zero { get; } = new(BigInteger.Zero, BigInteger.One);
    public static Rational One { get; } = new(BigInteger.One, BigInteger.One);

    public Rational(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.IsZero)
            throw new DivideByZeroException("Rational denominator cannot be zero.");

        if (numerator.IsZero)
        {
            Numerator = BigInteger.Zero;
            Denominator = BigInteger.One;
            return;
        }

        if (denominator < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        BigInteger gcd = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
        Numerator = numerator / gcd;
        Denominator = denominator / gcd;
    }

    public Rational(long numerator, long denominator) : this(new BigInteger(numerator), new BigInteger(denominator)) { }

    public Rational(long value) : this(new BigInteger(value), BigInteger.One) { }

    public bool IsZero => Numerator.IsZero;
    public int Sign => Numerator.Sign;

    public static Rational operator +(Rational a, Rational b) =>
        new(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator);

    public static Rational operator -(Rational a, Rational b) =>
        new(a.Numerator * b.Denominator - b.Numerator * a.Denominator, a.Denominator * b.Denominator);

    public static Rational operator -(Rational a) =>
        new(-a.Numerator, a.Denominator);

    public static Rational operator *(Rational a, Rational b) =>
        new(a.Numerator * b.Numerator, a.Denominator * b.Denominator);

    public static Rational operator /(Rational a, Rational b)
    {
        if (b.IsZero)
            throw new DivideByZeroException("Division by zero in Rational.");
        return new(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
    }

    public static bool operator ==(Rational a, Rational b) =>
        a.Numerator == b.Numerator && a.Denominator == b.Denominator;

    public static bool operator !=(Rational a, Rational b) => !(a == b);

    public static bool operator <(Rational a, Rational b) =>
        a.Numerator * b.Denominator < b.Numerator * a.Denominator;

    public static bool operator <=(Rational a, Rational b) =>
        a.Numerator * b.Denominator <= b.Numerator * a.Denominator;

    public static bool operator >(Rational a, Rational b) =>
        a.Numerator * b.Denominator > b.Numerator * a.Denominator;

    public static bool operator >=(Rational a, Rational b) =>
        a.Numerator * b.Denominator >= b.Numerator * a.Denominator;

    public bool Equals(Rational other) => this == other;

    public override bool Equals(object? obj) => obj is Rational r && Equals(r);

    public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);

    public int CompareTo(Rational other) =>
        (Numerator * other.Denominator).CompareTo(other.Numerator * Denominator);

    public override string ToString() =>
        Denominator.IsOne ? Numerator.ToString() : $"{Numerator}/{Denominator}";

    public double ToDouble() => (double)Numerator / (double)Denominator;
}
