# Core Doctrine

## Runtime Rules
- Gameplay logic must be pause-aware.
- Simulation-critical updates must not depend on render frame rate.
- Shared runtime services must have explicit ownership.
- Runtime behavior must be observable from structured logs.

## Boundary Rules
- This package owns reusable infrastructure only.
- Game-specific mechanics and product design doctrine belong in project-owned code/docs.
- Projects should extend via adapters/composition rather than editing package internals.
