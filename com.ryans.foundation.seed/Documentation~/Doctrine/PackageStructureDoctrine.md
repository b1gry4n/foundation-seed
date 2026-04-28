# Package Structure Doctrine

## Required Layout
- `Runtime/` for runtime-shipped code.
- `Editor/` for editor-only tooling.
- `Documentation~/` for package doctrine/system maps.
- `Tests/` for package tests.
- `Samples~/` for usage examples.

## Rules
- Runtime code must not depend on package docs.
- Editor tooling must remain generic and reusable.
- Project-specific rules/docs stay out of this package.
