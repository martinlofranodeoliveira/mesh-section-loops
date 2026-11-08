# Mesh Section Loops: Architecture & Implementation Design

## 1. Architectural Pipeline

`mesh-section-loops` avoids floating-point approximations and tolerance parameters by decoupling topological connectivity from coordinate precision.

```
            MeshSurface & Plane3I Input
                         │
                         ▼
             Closed 2-Manifold Validator
           (Half-Edge Incidence & Umbrella)
                         │
                         ▼
             Signed Distance Predicates
              h(v) = n . v - d (BigInt)
                         │
        ┌────────────────┼────────────────┐
        ▼                ▼                ▼
   Any h(v) == 0    All Same Sign    Mixed Signs
  (DegenerateCut)  (CompleteEmpty)        │
                                          ▼
                                Crossing Triangles
                               (Entering & Exiting)
                                          │
                                          ▼
                                Exact Rational Points
                              (BigInteger GCD Reductions)
                                          │
                                          ▼
                                Topo-Directed Loop Walk
                                          │
                                          ▼
                                Collinear Simplification
                                ((B-A) x (C-B) == 0)
                                          │
                                          ▼
                                Canonical 2D Projection
                                & Exact Rational Shoelace
```

---

## 2. Rational Arithmetic Engine (`Rational.cs`)

Standard IEEE 754 64-bit floating point numbers cannot represent arbitrary rational fractions (e.g., $1/3$, $1/7$, or divisions by large prime denominators) without accumulating rounding errors. In planar mesh sectioning, rounding errors cause adjacent intersection segments to mismatch by $10^{-15}$, forcing typical CAD systems to rely on arbitrary "welding tolerance" heuristics ($\epsilon \approx 10^{-6}$).

`mesh-section-loops` implements an exact `Rational` type backed by `System.Numerics.BigInteger`:
- Invariant: Denominator $> 0$, $\gcd(|\text{Numerator}|, \text{Denominator}) = 1$.
- Guaranteed exact equality: $A / B == C / D \iff A \cdot D == B \cdot C$.
- All point coordinates and loop areas are calculated without precision loss.

---

## 3. Topology-Driven Cycle Assembly

Instead of searching $O(N^2)$ space for geometrically proximate points, loop connectivity is derived strictly from triangle half-edges:
1. Each crossing edge is indexed canonically as $(\min(u, v), \max(u, v))$.
2. In each crossing triangle $T$, the directed section enters across edge $E_{in}$ and exits across edge $E_{out}$.
3. We store this as a directed map `edgeTransitions[E_in] = E_out`.
4. Tracing cycles in `edgeTransitions` executes in $O(|E_{\text{crossing}}|)$ linear time, naturally decomposing multiple nested or disjoint intersection contours into individual loops.
