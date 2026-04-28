# Input System Map

## Entry Point
- `Runtime/Input/FoundationSeedInputRouter.cs`

## Chain
1. Router receives raised actions from project-owned binding adapters.
2. Subscribers consume standardized action payloads.
3. Package does not own concrete control schemes.
