# Save System Map

## Entry Point
- `Runtime/Save/FoundationSeedSaveService.cs`

## Chain
1. Save service registers project providers by provider id.
2. Save writes provider payloads to one envelope file.
3. Load restores registered providers from stored payloads.
4. Projects own payload schema semantics.
