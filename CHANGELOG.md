# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-11-08

### Added
- Initial release of `mesh-section-loops`.
- Exact 2-manifold triangular surface validation (orientation pairing and vertex umbrella cycle).
- Integer distance predicates and degeneration detection ($h(v) = 0$).
- Exact `BigInteger` normalized rational coordinates (`Rational`, `RationalPoint3D`).
- Topology-driven loop assembly without floating-point coordinate welding.
- Collinear degree-2 point simplification with exact provenance retention.
- Zero-dependency console test suite and CLI (`demo --assert`, `demo --json`).
- Bilingual documentation in English and Portuguese.
