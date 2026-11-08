# Mesh Section Loops: Mathematical Contract & Invariants Specification

## 1. Abstract & Scope

`mesh-section-loops` provides an exact geometric and topological mesh-plane intersection engine in C# (.NET 10). It evaluates closed oriented triangular 2-manifold surfaces against integer hyperplanes, computing exact rational contour coordinates and assembling closed topological loops without floating-point coordinate welding.

---

## 2. Geometry & Surface Admissibility

### 2.1 Coordinate Domains
1. **Vertex Coordinates**: $v = (v_x, v_y, v_z) \in [-2^{19}, 2^{19}-1]^3 = [-524288, 524287]^3$ (signed 20-bit integers).
2. **Plane Equation**:
   $$\mathbf{n} \cdot \mathbf{x} = d \iff n_x x + n_y y + n_z z = d$$
   where normal $\mathbf{n} = (n_x, n_y, n_z) \in [-32768, 32767]^3 \setminus \{(0,0,0)\}$ (signed 16-bit integers) and $d \in [-2^{36}, 2^{36}-1]$ (signed 37-bit integer).
3. **Signed Distance Envelope**:
   The signed distance predicate for vertex $v$ is:
   $$h(v) = \mathbf{n} \cdot \mathbf{v} - d$$
   Maximum magnitude:
   $$|\mathbf{n} \cdot \mathbf{v}| \le 3 \times 32768 \times 524288 \approx 5.15 \times 10^{10} < 2^{36}$$
   Thus $h(v)$ strictly fits within a signed 38-bit integer envelope.

### 2.2 Manifold Topology Invariants
A surface $\mathcal{M} = (\mathcal{V}, \mathcal{F})$ is an admitted closed oriented 2-manifold iff:
1. **Cardinality**: $1 \le |\mathcal{V}| \le 16384$ vertices and $1 \le |\mathcal{F}| \le 32768$ triangles.
2. **Area Non-Degeneracy**: For every triangle $(v_0, v_1, v_2)$, $(v_1 - v_0) \times (v_2 - v_0) \ne \mathbf{0}$.
3. **Edge Incidence Pairing**: Every directed half-edge $(u, v)$ has exactly one corresponding reverse half-edge $(v, u)$ in the entire mesh (no boundaries, no non-orientable Möbius strips).
4. **Vertex Umbrella Cycle**: For every vertex $v$, the set of incident triangles forms a single connected topological cycle (no pinched vertices).

---

## 3. Exact Rational Intersection & Loop Assembly

### 3.1 Edge-Plane Intersection
An edge $(u, v)$ intersects plane $\mathbf{n} \cdot \mathbf{x} = d$ iff $h(u) \cdot h(v) < 0$.
The intersection parameter is:
$$t = \frac{h(u)}{h(u) - h(v)} \in (0, 1)$$
The exact 3D point $\mathbf{p}$ is given by:
$$\mathbf{p} = \frac{u \cdot (-h(v)) + v \cdot h(u)}{h(u) - h(v)}$$
Each coordinate component $(p_x, p_y, p_z)$ is stored as an exact normalized `Rational` $N / D$ where $D > 0$ and $\gcd(|N|, D) = 1$ using `System.Numerics.BigInteger`.

### 3.2 Degeneracy Classification
If for any vertex $v$, $h(v) = 0$ (the plane passes directly through one or more vertices), the engine returns `SectionStatus.DegenerateCut` reporting all degenerate vertex IDs. Coplanar facets and vertex cuts are not silently converted into approximate lines.

### 3.3 Topological Loop Walk
In every crossing triangle $T$:
- Exactly one edge $E_{in}$ transitions from negative to positive half-space ($h(u) < 0, h(v) > 0$).
- Exactly one edge $E_{out}$ transitions from positive to negative half-space ($h(u) > 0, h(v) < 0$).
The oriented segment runs from $E_{in}$ to $E_{out}$. Because $T$ shares $E_{out}$ with an adjacent triangle where the half-edge is traversed in reverse, the segments connect topologically to form closed cycles without floating-point proximity searches.

---

## 4. Collinear Simplification & 2D Projection

### 4.1 Collinear Point Removal
Three consecutive points $A, B, C$ along a loop are collinear iff:
$$(B - A) \times (C - B) = \mathbf{0}$$
Redundant degree-2 points are removed exactly, preserving loop orientation.

### 4.2 Canonical 2D Projection
Loops are projected onto the plane orthogonal to the dominant normal axis $k = \arg\max_{\alpha} |n_\alpha|$:
- If $|n_z| \ge |n_x|$ and $|n_z| \ge |n_y|$, drop $z \to (x, y)$.
- If $|n_y| \ge |n_x|$ and $|n_y| \ge |n_z|$, drop $y \to (x, z)$.
- Otherwise drop $x \to (y, z)$.
Projected signed area is evaluated via the shoelace formula using exact rational arithmetic.
